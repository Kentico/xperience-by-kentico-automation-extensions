using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

/// <summary>
/// Marketer-configurable properties of <see cref="ContactDateAnniversaryTrigger"/>.
/// </summary>
public sealed class ContactDateAnniversaryTriggerProperties : IAutomationTriggerProperties
{
    /// <summary>
    /// Contact date field whose anniversary starts the process.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.contactdateanniversary.datefield.label$}",
        ExplanationText = "{$labs.automationextensions.contactdateanniversary.datefield.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ContactDateFieldOptionsProvider),
        Order = 10)]
    [RequiredValidationRule]
    public string DateField { get; set; } = string.Empty;


    /// <summary>
    /// Days before the anniversary the process starts (invariant integer string, 0 = on the day).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.contactdateanniversary.daysbefore.label$}",
        ExplanationText = "{$labs.automationextensions.contactdateanniversary.daysbefore.explanation$}",
        DataProviderType = typeof(DaysBeforeOptionsProvider),
        Order = 20)]
    [RequiredValidationRule]
    public string DaysBefore { get; set; } = "0";
}
