using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

/// <summary>
/// Marketer-configurable properties of <see cref="OrderCompletedTrigger"/>.
/// </summary>
public sealed class OrderCompletedTriggerProperties : IAutomationTriggerProperties
{
    /// <summary>
    /// Lowest order grand total that starts the process (inclusive). <c>0</c> = any order.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "{$labs.automationextensions.ordercompleted.minimumtotal.label$}",
        ExplanationText = "{$labs.automationextensions.ordercompleted.minimumtotal.explanation$}",
        Order = 10)]
    [MinimumDecimalValueValidationRule(0, ErrorMessage = "{$labs.automationextensions.ordercompleted.minimumtotal.minimum$}")]
    public decimal MinimumTotal { get; set; }


    /// <summary>
    /// Currency code the order must have (trimmed, case-insensitive). Empty = any currency.
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.ordercompleted.currency.label$}",
        ExplanationText = "{$labs.automationextensions.ordercompleted.currency.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 20)]
    [MaxLengthValidationRule(OrderCompletedTrigger.CURRENCY_MAX_LENGTH, ErrorMessage = "{$labs.automationextensions.ordercompleted.currency.maxlength$}")]
    public string? Currency { get; set; }


    /// <summary>
    /// Starts only for the customer's first order.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.ordercompleted.firstorderonly.label$}",
        ExplanationText = "{$labs.automationextensions.ordercompleted.firstorderonly.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 30)]
    public bool FirstOrderOnly { get; set; }
}
