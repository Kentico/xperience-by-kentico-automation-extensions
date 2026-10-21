using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Marketer-configurable properties of <see cref="ContactGroupMemberTrigger"/>.
/// </summary>
public sealed class ContactGroupMemberTriggerProperties : IAutomationTriggerProperties
{
    /// <summary>
    /// Code name of the contact group whose members start the process.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.contactgroupmember.contactgroup.label$}",
        ExplanationText = "{$labs.automationextensions.contactgroupmember.contactgroup.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ContactGroupOptionsProvider),
        Order = 10)]
    [RequiredValidationRule]
    public string ContactGroup { get; set; } = string.Empty;
}
