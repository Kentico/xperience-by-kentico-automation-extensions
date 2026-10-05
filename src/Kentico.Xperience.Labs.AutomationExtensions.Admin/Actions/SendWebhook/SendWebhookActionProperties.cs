using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// Marketer-configurable properties of <see cref="SendWebhookAction"/>.
/// </summary>
[FormCategory(Label = "{$labs.automationextensions.category.general$}", Order = 100)]
[FormCategory(Label = "{$labs.automationextensions.category.result$}", Order = 200, Collapsible = true)]
public sealed class SendWebhookActionProperties : SkippableActionProperties
{
    /// <summary>
    /// Name of the configured endpoint the payload is posted to.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.sendwebhook.endpoint.label$}",
        ExplanationText = "{$labs.automationextensions.sendwebhook.endpoint.explanation$}",
        DataProviderType = typeof(WebhookEndpointOptionsProvider),
        Order = 110)]
    [RequiredValidationRule]
    public string EndpointName { get; set; } = string.Empty;


    /// <summary>
    /// Event name sent in the payload so the receiver can tell webhooks apart.
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.sendwebhook.eventname.label$}",
        ExplanationText = "{$labs.automationextensions.sendwebhook.eventname.explanation$}",
        Order = 120)]
    [RequiredValidationRule]
    [MaxLengthValidationRule(100)]
    public string EventName { get; set; } = string.Empty;


    /// <summary>
    /// Includes the contact's email, first name, last name, and company name in the payload.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.sendwebhook.includecontactdetails.label$}",
        ExplanationText = "{$labs.automationextensions.sendwebhook.includecontactdetails.explanation$}",
        Order = 130)]
    public bool IncludeContactDetails { get; set; }


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
