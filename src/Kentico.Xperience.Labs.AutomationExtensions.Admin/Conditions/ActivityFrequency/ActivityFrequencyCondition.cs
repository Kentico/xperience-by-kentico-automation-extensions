using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationCondition<ActivityFrequencyCondition>(
    ActivityFrequencyCondition.IDENTIFIER,
    "labs.automationextensions.activityfrequency.displayname",
    Description = "labs.automationextensions.activityfrequency.description",
    IconName = Icons.EkgLine)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Compares how many activities of one type the contact performed in the last N days with a number.
/// Read-only – never changes the contact. Plain "has performed at least once" checks belong to the built-in Rule-based condition.
/// </summary>
public sealed class ActivityFrequencyCondition(
    IAutomationComponentStatus componentStatus,
    ActivityTypeResolver activityTypeResolver,
    ActivityLanguageResolver languageResolver,
    IActivityCounter activityCounter,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : AutomationCondition<ActivityFrequencyProperties>
{
    public const string NAME = "ActivityFrequency";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;

    /// <summary>
    /// Maximum number of distinct URLs per contact and window that URL rules evaluate; more → No (fails closed).
    /// </summary>
    public const int MAX_DISTINCT_URLS = 10_000;


    private static readonly TimeSpan warningLogInterval = TimeSpan.FromHours(1);


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ActivityTypeResolver activityTypeResolver = activityTypeResolver;
    private readonly ActivityLanguageResolver languageResolver = languageResolver;
    private readonly IActivityCounter activityCounter = activityCounter;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.ActivityFrequency));


    /// <inheritdoc />
    public override async Task<bool> Evaluate(
        ActivityFrequencyProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.ActivityFrequency))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.ActivityFrequency, context.Process.DisplayName, "sends all contacts through the No branch");
            return false;
        }

        if (SkippableStep.TryGetSkippedResult(properties, out bool skippedResult))
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return skippedResult;
        }

        string? activityType = await activityTypeResolver.ResolveTypeName(properties.ActivityType, cancellationToken);
        // Resolved only when the language filter applies, so stale hidden languages never invalidate the step.
        int? languageId = ActivityDetailFilters.IsLanguageApplied(activityType, properties)
            ? await languageResolver.ResolveLanguageId(properties.Language, cancellationToken)
            : null;
        IReadOnlyList<ActivityCriterion> criteria = [];
        string? error = ActivityFrequencyConfiguration.GetConfigurationError(properties, activityType is not null, out var comparisonOperator)
            ?? ActivityDetailFilters.GetCriteria(activityType!, properties, languageId, out criteria);
        if (error is not null)
        {
            LogInvalidConfiguration(context.Process.DisplayName, properties.ActivityType, error);
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var contact = await context.GetProcessedObject(cancellationToken);

        return await CountAndCompare(contact.ContactID, activityType!, criteria, comparisonOperator, properties, context.Process.DisplayName, cancellationToken);
    }


    /// <summary>
    /// Counts the contact's activities in the window and compares the count. Separated from <see cref="Evaluate"/> because
    /// reading <c>ContactInfo.ContactID</c> needs the Xperience container, which unit tests don't have.
    /// </summary>
    internal async Task<bool> CountAndCompare(
        int contactId,
        string activityType,
        IReadOnlyList<ActivityCriterion> criteria,
        NumberComparisonOperator comparisonOperator,
        ActivityFrequencyProperties properties,
        string processName,
        CancellationToken cancellationToken)
    {
        var since = ActivityWindow.GetSince(timeProvider.GetLocalNow().DateTime, properties.Days);
        int? count = criteria.OfType<ActivityUrlMatchCriterion>().FirstOrDefault() is { } urlMatch
            ? await CountUrlMatches(contactId, activityType, since, criteria, urlMatch, processName, cancellationToken)
            : await activityCounter.Count(contactId, activityType, since, criteria, cancellationToken);
        if (count is null)
        {
            return false;
        }

        bool result = NumberComparison.Compare(count.Value, comparisonOperator, properties.Count);

        logger.LogDebug(
            LogEvents.ActivityFrequencyEvaluated,
            "Automation '{ProcessName}', contact {ContactId}: {ActivityCount} '{ActivityType}' activities in the last {Days} days {Operator} {Count} is {Result}.",
            processName,
            contactId,
            count,
            activityType,
            properties.Days,
            comparisonOperator,
            properties.Count,
            result);

        return result;
    }


    /// <summary>
    /// Counts activities whose URL matches the rules: the database groups the filtered (and prefiltered) activities by URL,
    /// the distinct URLs are matched here and their counts summed. Returns <c>null</c> (→ No) when the contact has more than
    /// <see cref="MAX_DISTINCT_URLS"/> distinct URLs in the window.
    /// </summary>
    private async Task<int?> CountUrlMatches(
        int contactId,
        string activityType,
        DateTime since,
        IReadOnlyList<ActivityCriterion> criteria,
        ActivityUrlMatchCriterion urlMatch,
        string processName,
        CancellationToken cancellationToken)
    {
        var groups = await activityCounter.CountByValue(contactId, activityType, since, criteria, urlMatch.Column, MAX_DISTINCT_URLS + 1, cancellationToken);
        if (groups.Count > MAX_DISTINCT_URLS)
        {
            logger.LogWithIntervalPolicy(
                LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|UrlLimit|{processName}|{activityType}", warningLogInterval),
                l => l.LogWarning(
                    LogEvents.ActivityUrlLimitExceeded,
                    "Step '{ComponentIdentifier}' in process '{ProcessName}': contact {ContactId} has more than {MaxUrls} distinct URLs of '{ActivityType}' activities in the window, so URL rules weren't evaluated and the contact follows the No branch.",
                    IDENTIFIER,
                    processName,
                    contactId,
                    MAX_DISTINCT_URLS,
                    activityType));

            return null;
        }

        var (count, timedOut) = ActivityUrlMatchCounting.Sum(groups, urlMatch.RuleSet);
        if (timedOut)
        {
            logger.LogWithIntervalPolicy(
                LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|UrlMatchTimeout|{processName}", warningLogInterval),
                l => l.LogWarning(
                    LogEvents.UrlMatchTimeout,
                    "Step '{ComponentIdentifier}' in process '{ProcessName}': a wildcard URL rule took too long for some URLs of contact {ContactId}; those URLs were counted as not matching.",
                    IDENTIFIER,
                    processName,
                    contactId));
        }

        return count;
    }


    private void LogInvalidConfiguration(string processName, string? activityType, string reason) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|InvalidConfiguration|{processName}|{activityType}", warningLogInterval),
            l => l.LogWarning(
                LogEvents.InvalidActivityFrequencyConfiguration,
                "Step '{ComponentIdentifier}' in process '{ProcessName}' is not configured correctly and sends all contacts through the No branch: {Reason}.",
                IDENTIFIER,
                processName,
                reason));
}
