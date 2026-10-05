using CMS.Automation;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationCondition<WithinTimeWindowCondition>(
    WithinTimeWindowCondition.IDENTIFIER,
    "labs.automationextensions.withintimewindow.displayname",
    Description = "labs.automationextensions.withintimewindow.description",
    IconName = Icons.Clock)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;

/// <summary>
/// Evaluates whether the current time (when the condition runs) falls within a configured time window.
/// </summary>
public sealed class WithinTimeWindowCondition(
    IAutomationComponentStatus componentStatus,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : AutomationCondition<WithinTimeWindowProperties>
{
    public const string NAME = "WithinTimeWindow";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;

    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.WithinTimeWindow));


    /// <inheritdoc />
    public override async Task<bool> Evaluate(
        WithinTimeWindowProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.WithinTimeWindow))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.WithinTimeWindow, context.Process.DisplayName, "sends all contacts through the No branch");
            return false;
        }

        if (SkippableStep.TryGetSkippedResult(properties, out bool skippedResult))
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return skippedResult;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Read the current time exactly once
        var utcNow = timeProvider.GetUtcNow().DateTime;

        try
        {
            var selectedWeekdays = properties.GetSelectedWeekdays();

            // Validate settings
            bool isWithin = TimeWindowCalculator.IsWithinWindow(
                utcNow,
                selectedWeekdays,
                properties.StartTime,
                properties.EndTime,
                properties.TimeZoneId,
                FormatDate(properties.StartDate),
                FormatDate(properties.EndDate));

            if (isWithin)
            {
                logger.LogInformation(
                    "Contact is within the configured time window ({StartTime}-{EndTime} {TimeZoneId}).",
                    properties.StartTime,
                    properties.EndTime,
                    properties.TimeZoneId);
            }
            else
            {
                logger.LogInformation(
                    "Contact is outside the configured time window ({StartTime}-{EndTime} {TimeZoneId}).",
                    properties.StartTime,
                    properties.EndTime,
                    properties.TimeZoneId);
            }

            return isWithin;
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid configuration in WithinTimeWindow condition: {ErrorMessage}", ex.Message);
            return false;
        }
    }


    private static string FormatDate(DateTime? date) =>
        date?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
}
