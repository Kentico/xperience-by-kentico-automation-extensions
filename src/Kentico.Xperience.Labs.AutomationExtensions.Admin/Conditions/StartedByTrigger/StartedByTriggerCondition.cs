using CMS.Automation;
using CMS.Automation.Internal;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationCondition<StartedByTriggerCondition>(
    StartedByTriggerCondition.IDENTIFIER,
    "labs.automationextensions.startedbytrigger.displayname",
    Description = "labs.automationextensions.startedbytrigger.description",
    IconName = Icons.Flag)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Checks which trigger started the process and, optionally, a value of its trigger data. Lets one process started by
/// several triggers branch per trigger. Read-only.
/// </summary>
public sealed class StartedByTriggerCondition(
    IAutomationComponentStatus componentStatus,
    ICustomAutomationTriggerProvider triggerProvider,
    ILoggerFactory loggerFactory) : AutomationCondition<StartedByTriggerProperties>
{
    public const string NAME = "StartedByTrigger";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private static readonly TimeSpan warningLogInterval = TimeSpan.FromHours(1);


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.StartedByTrigger));


    /// <inheritdoc />
    public override async Task<bool> Evaluate(
        StartedByTriggerProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.StartedByTrigger))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.StartedByTrigger, context.Process.DisplayName, "sends all contacts through the No branch");
            return false;
        }

        if (SkippableStep.TryGetSkippedResult(properties, out bool skippedResult))
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return skippedResult;
        }

        var trigger = properties.CheckTriggerData ? RegisteredTriggers.Find(triggerProvider, properties.Trigger) : null;
        string? error = StartedByTriggerConfiguration.GetConfigurationError(properties, trigger, out var configuration);
        if (error is not null)
        {
            LogInvalidConfiguration(context.Process.DisplayName, properties.Trigger, error);
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        string? triggerIdentifier = await context.GetTriggerIdentifier(cancellationToken);
        if (!configuration!.Matches(triggerIdentifier))
        {
            return false;
        }

        if (configuration.Property is null)
        {
            return true;
        }

        var triggerData = await context.GetTriggerData<IAutomationTriggerData>(cancellationToken);
        if (triggerData is null || !configuration.DataType!.IsInstanceOfType(triggerData))
        {
            LogDataMissing(context.Process.DisplayName, configuration.TriggerIdentifier);
            return false;
        }

        object? actual = configuration.Property.GetValue(triggerData);

        return TriggerDataComparison.Compare(actual, configuration.Kind, configuration.Operator, configuration.Value);
    }


    private void LogInvalidConfiguration(string processName, string? triggerIdentifier, string reason) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|InvalidConfiguration|{processName}|{triggerIdentifier}", warningLogInterval),
            l => l.LogWarning(
                LogEvents.InvalidStartedByTriggerConfiguration,
                "Step '{ComponentIdentifier}' in process '{ProcessName}' is not configured correctly and sends all contacts through the No branch: {Reason}.",
                IDENTIFIER,
                processName,
                reason));


    private void LogDataMissing(string processName, string triggerIdentifier) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|DataMissing|{processName}|{triggerIdentifier}", warningLogInterval),
            l => l.LogWarning(
                LogEvents.StartedByTriggerDataMissing,
                "Step '{ComponentIdentifier}' in process '{ProcessName}' found no trigger data of trigger '{TriggerIdentifier}'; the contact follows the No branch.",
                IDENTIFIER,
                processName,
                triggerIdentifier));
}
