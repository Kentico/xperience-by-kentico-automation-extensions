using CMS.Automation;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.PreviousStepSucceeded;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationCondition<PreviousStepSucceededCondition>(
    PreviousStepSucceededCondition.IDENTIFIER,
    "labs.automationextensions.previousstepsucceeded.displayname",
    Description = "labs.automationextensions.previousstepsucceeded.description",
    IconName = Icons.CheckCircle)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.PreviousStepSucceeded;

/// <summary>
/// Branches on the outcome a preceding library action (e.g. "Send webhook") recorded under the selected result key.
/// Follows the false branch when enabled and no result was recorded; a disabled component follows the No branch.
/// </summary>
public sealed class PreviousStepSucceededCondition(
    IAutomationComponentStatus componentStatus,
    ILoggerFactory loggerFactory) : AutomationCondition<PreviousStepSucceededConditionProperties>
{
    public const string NAME = "PreviousStepSucceeded";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private static readonly TimeSpan missingResultLogInterval = TimeSpan.FromHours(1);


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.PreviousStepSucceeded));


    /// <inheritdoc />
    public override async Task<bool> Evaluate(PreviousStepSucceededConditionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.PreviousStepSucceeded))
        {
            DisabledComponentLog.Write(
                logger,
                AutomationComponentCatalog.PreviousStepSucceeded,
                context.Process.DisplayName,
                "sends all contacts through the No branch");
            return false;
        }

        if (SkippableStep.TryGetSkippedResult(properties, out bool skippedResult))
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return skippedResult;
        }

        string? key = StepResultKeys.Create(properties.SourceName, properties.ResultNumber);
        var data = await context.GetProcessData<StepResultData>(cancellationToken);
        var result = StepResultRecorder.Find(data, key);

        if (result is null)
        {
            string processName = context.Process.DisplayName;
            logger.LogWithIntervalPolicy(
                LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|MissingResult|{processName}|{key}", missingResultLogInterval),
                l => l.LogWarning(
                    LogEvents.StepResultMissing,
                    "No step result saved for step '{SourceName}', result number {ResultNumber} in process '{ProcessName}'. Add an earlier step of this type that saves its result with the same number.",
                    properties.SourceName,
                    properties.ResultNumber,
                    processName));

            return false;
        }

        return result.Succeeded;
    }
}
