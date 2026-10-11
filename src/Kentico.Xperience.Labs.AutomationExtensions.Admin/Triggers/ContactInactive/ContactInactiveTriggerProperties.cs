using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Marketer-configurable properties of <see cref="ContactInactiveTrigger"/>.
/// </summary>
public sealed class ContactInactiveTriggerProperties : IAutomationTriggerProperties
{
    /// <summary>
    /// Number of days without activity that starts the process (invariant integer string).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.contactinactive.inactivedays.label$}",
        ExplanationText = "{$labs.automationextensions.contactinactive.inactivedays.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(InactiveDaysOptionsProvider),
        Order = 10)]
    [RequiredValidationRule]
    public string InactiveDays { get; set; } = "90";
}
