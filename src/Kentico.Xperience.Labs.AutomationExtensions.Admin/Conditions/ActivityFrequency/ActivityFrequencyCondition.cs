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
    IActivityCounter activityCounter,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : AutomationCondition<ActivityFrequencyProperties>
{
    public const string NAME = "ActivityFrequency";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private static readonly TimeSpan invalidConfigurationLogInterval = TimeSpan.FromHours(1);


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ActivityTypeResolver activityTypeResolver = activityTypeResolver;
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
        IReadOnlyList<ActivityDetailCriterion> criteria = [];
        string? error = ActivityFrequencyConfiguration.GetConfigurationError(properties, activityType is not null, out var comparisonOperator)
            ?? ActivityDetailFilters.GetCriteria(activityType!, properties, out criteria);
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
        IReadOnlyList<ActivityDetailCriterion> criteria,
        NumberComparisonOperator comparisonOperator,
        ActivityFrequencyProperties properties,
        string processName,
        CancellationToken cancellationToken)
    {
        var since = ActivityWindow.GetSince(timeProvider.GetLocalNow().DateTime, properties.Days);
        int count = await activityCounter.Count(
            contactId,
            activityType,
            since,
            criteria,
            cancellationToken);
        bool result = NumberComparison.Compare(count, comparisonOperator, properties.Count);

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


    private void LogInvalidConfiguration(string processName, string? activityType, string reason) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|InvalidConfiguration|{processName}|{activityType}", invalidConfigurationLogInterval),
            l => l.LogWarning(
                LogEvents.InvalidActivityFrequencyConfiguration,
                "Step '{ComponentIdentifier}' in process '{ProcessName}' is not configured correctly and sends all contacts through the No branch: {Reason}.",
                IDENTIFIER,
                processName,
                reason));
}
