using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Marketer-configurable properties of <see cref="RecipientListMemberTrigger"/>.
/// </summary>
public sealed class RecipientListMemberTriggerProperties : IAutomationTriggerProperties
{
    /// <summary>
    /// Code name of the recipient list whose subscribed contacts start the process.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.recipientlistmember.recipientlist.label$}",
        ExplanationText = "{$labs.automationextensions.recipientlistmember.recipientlist.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(RecipientListOptionsProvider),
        Order = 10)]
    [RequiredValidationRule]
    public string RecipientList { get; set; } = string.Empty;
}
