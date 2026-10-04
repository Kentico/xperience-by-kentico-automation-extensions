using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;

/// <summary>
/// Marketer-configurable properties of <see cref="WriteToLogAction"/>.
/// </summary>
[FormCategory(Label = "{$labs.automationextensions.category.general$}", Order = 100)]
[FormCategory(Label = "{$labs.automationextensions.writetolog.category.details$}", Order = 200, Collapsible = true)]
public sealed class WriteToLogActionProperties : SkippableActionProperties
{
    /// <summary>
    /// Default message pre-filled for new steps. Process, contact GUID, step results, and trigger are added to every entry automatically.
    /// </summary>
    public const string DEFAULT_MESSAGE = "Contact {contactId} reached this step.";


    /// <summary>
    /// Message written to the log. Supports placeholders (see <see cref="LogMessageFormatter"/>).
    /// </summary>
    [TextAreaComponent(
        Label = "{$labs.automationextensions.writetolog.message.label$}",
        ExplanationText = "{$labs.automationextensions.writetolog.message.explanation$}",
        Order = 110)]
    [RequiredValidationRule]
    [MaxLengthValidationRule(1000)]
    public string Message { get; set; } = DEFAULT_MESSAGE;


    /// <summary>
    /// Log level name (<see cref="LogLevel"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.writetolog.level.label$}",
        ExplanationText = "{$labs.automationextensions.writetolog.level.explanation$}",
        DataProviderType = typeof(LogLevelOptionsProvider),
        Order = 120)]
    [RequiredValidationRule]
    public string Level { get; set; } = nameof(LogLevel.Information);


    /// <summary>
    /// Adds results saved by earlier steps (e.g. "Send webhook") to the log entry.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.writetolog.includestepresults.label$}",
        ExplanationText = "{$labs.automationextensions.writetolog.includestepresults.explanation$}",
        Order = 210)]
    public bool IncludeStepResults { get; set; } = true;


    /// <summary>
    /// Adds the trigger that started the process and its non-personal data to the log entry.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.writetolog.includetriggerdetails.label$}",
        ExplanationText = "{$labs.automationextensions.writetolog.includetriggerdetails.explanation$}",
        Order = 220)]
    public bool IncludeTriggerDetails { get; set; } = true;
}
