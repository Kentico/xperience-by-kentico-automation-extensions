using CMS.Automation;
using CMS.Automation.Internal;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<WriteToLogAction>(
    WriteToLogAction.IDENTIFIER,
    "labs.automationextensions.writetolog.displayname",
    Description = "labs.automationextensions.writetolog.description",
    IconName = Icons.ClipboardList)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;

/// <summary>
/// Writes a marketer-defined message to the application log for debugging processes (which branch did a contact take, and why).
/// Optionally includes saved step results and trigger details. Never logs personal contact data.
/// </summary>
/// <remarks>
/// Entries use the category <c>Kentico.Xperience.Labs.AutomationExtensions.WriteToLog</c>, so the default Xperience event log
/// configuration for <c>Kentico</c> categories applies. Logs for every contact passing the step – enable in development only
/// or for short troubleshooting periods.
/// </remarks>
public sealed class WriteToLogAction(
    IAutomationComponentStatus componentStatus,
    ICustomAutomationTriggerProvider triggerProvider,
    ILoggerFactory loggerFactory) : AutomationAction<WriteToLogActionProperties>
{
    public const string NAME = "WriteToLog";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.WriteToLog));


    /// <inheritdoc />
    public override async Task Execute(WriteToLogActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.WriteToLog))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.WriteToLog, context.Process.DisplayName);
            return;
        }

        if (properties.SkipStep)
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return;
        }

        var level = Enum.TryParse(properties.Level, out LogLevel parsedLevel) ? parsedLevel : LogLevel.Information;
        if (!logger.IsEnabled(level))
        {
            return;
        }

        var contact = await context.GetProcessedObject(cancellationToken);
        string triggerIdentifier = await context.GetTriggerIdentifier(cancellationToken) ?? string.Empty;
        string message = LogMessageFormatter.Format(properties.Message, CreatePlaceholderValues(context, contact, triggerIdentifier));

        string stepResults = properties.IncludeStepResults
            ? StepResultSummary.Format(await context.GetProcessData<StepResultData>(cancellationToken))
            : "-";
        string triggerDetails = properties.IncludeTriggerDetails
            ? await TriggerDetailsDescriber.Describe(context, triggerProvider, cancellationToken)
            : "-";

        logger.Log(
            level,
            LogEvents.AutomationLog,
            "Automation '{ProcessName}', contact {ContactGuid}: {AutomationMessage} | Step results: {StepResults} | Trigger: {TriggerDetails}",
            context.Process.DisplayName,
            contact.ContactGUID,
            message,
            stepResults,
            triggerDetails);
    }


    private static Dictionary<string, string> CreatePlaceholderValues(
        AutomationProcessContext context,
        ContactInfo contact,
        string triggerIdentifier) => new()
        {
            [LogMessageFormatter.PROCESS_NAME] = context.Process.DisplayName,
            [LogMessageFormatter.CONTACT_ID] = contact.ContactID.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [LogMessageFormatter.CONTACT_GUID] = contact.ContactGUID.ToString(),
            [LogMessageFormatter.TRIGGER_IDENTIFIER] = triggerIdentifier
        };
}
