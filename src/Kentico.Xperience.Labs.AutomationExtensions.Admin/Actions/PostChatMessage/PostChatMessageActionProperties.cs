using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Marketer-configurable properties of <see cref="PostChatMessageAction"/>.
/// </summary>
[FormCategory(Label = "{$labs.automationextensions.category.general$}", Order = 100)]
[FormCategory(Label = "{$labs.automationextensions.category.result$}", Order = 200, Collapsible = true)]
public sealed class PostChatMessageActionProperties : SkippableActionProperties
{
    /// <summary>
    /// Name of the configured chat channel the message is posted to.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.postchatmessage.channel.label$}",
        ExplanationText = "{$labs.automationextensions.postchatmessage.channel.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ChatChannelOptionsProvider),
        Order = 110)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.postchatmessage.channel.required$}")]
    public string ChannelName { get; set; } = string.Empty;


    /// <summary>
    /// Optional title with tokens (see <c>ContactFieldValueTemplate</c>).
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.postchatmessage.title.label$}",
        ExplanationText = "{$labs.automationextensions.postchatmessage.title.explanation$}",
        Order = 120)]
    [MaxLengthValidationRule(150)]
    public string Title { get; set; } = string.Empty;


    /// <summary>
    /// Message text with tokens (see <c>ContactFieldValueTemplate</c>). Plain text; line breaks are kept.
    /// </summary>
    [TextAreaComponent(
        Label = "{$labs.automationextensions.postchatmessage.message.label$}",
        ExplanationText = "{$labs.automationextensions.postchatmessage.message.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 130)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.postchatmessage.message.required$}")]
    [MaxLengthValidationRule(2000)]
    public string Message { get; set; } = string.Empty;


    /// <summary>
    /// Adds the contact's email, name, and company name to the message.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.postchatmessage.includecontactdetails.label$}",
        ExplanationText = "{$labs.automationextensions.postchatmessage.includecontactdetails.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 140)]
    public bool IncludeContactDetails { get; set; }


    /// <summary>
    /// Adds a link to the contact in the admin. Ignored when no admin URL is configured.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.postchatmessage.includeadminlink.label$}",
        ExplanationText = "{$labs.automationextensions.postchatmessage.includeadminlink.explanation$}",
        Order = 150)]
    public bool IncludeAdminLink { get; set; } = true;


    /// <summary>
    /// Adds a link to the form submission in the admin when the process was started by the built-in form submission trigger.
    /// Ignored for other triggers, without trigger data, or when no admin URL is configured.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.postchatmessage.includeformsubmissionlink.label$}",
        ExplanationText = "{$labs.automationextensions.postchatmessage.includeformsubmissionlink.explanation$}",
        Order = 160)]
    public bool IncludeFormSubmissionLink { get; set; } = true;


    /// <summary>
    /// Adds a link to the Contacts tab of this automation process. The process is found by display name; no link when several
    /// processes share the name or no admin URL is configured.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.postchatmessage.includeprocesslink.label$}",
        ExplanationText = "{$labs.automationextensions.postchatmessage.includeprocesslink.explanation$}",
        Order = 170)]
    public bool IncludeProcessLink { get; set; } = true;


    /// <summary>
    /// Records the outcome in process data so later steps (e.g. "Previous step succeeded") can branch on it.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.recordresult.label$}",
        ExplanationText = "{$labs.automationextensions.recordresult.explanation$}",
        Order = 210)]
    public bool RecordResult { get; set; }


    /// <summary>
    /// Result number the outcome is recorded under (see <see cref="StepResultKeys"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.resultnumber.label$}",
        ExplanationText = "{$labs.automationextensions.resultnumber.write.explanation$}",
        DataProviderType = typeof(StepResultNumberOptionsProvider),
        Order = 220)]
    [VisibleIfTrue(nameof(RecordResult))]
    public string ResultNumber { get; set; } = StepResultKeys.DEFAULT_RESULT_NUMBER;
}
