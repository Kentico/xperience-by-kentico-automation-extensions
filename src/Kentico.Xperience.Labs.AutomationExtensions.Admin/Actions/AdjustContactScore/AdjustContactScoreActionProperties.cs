using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Marketer-configurable properties of <see cref="AdjustContactScoreAction"/>.
/// </summary>
[FormCategory(Label = "{$labs.automationextensions.category.general$}", Order = 100)]
[FormCategory(Label = "{$labs.automationextensions.adjustcontactscore.category.limits$}", Order = 200, Collapsible = true)]
public sealed class AdjustContactScoreActionProperties : SkippableActionProperties
{
    /// <summary>
    /// Name of the integer custom contact field holding the score.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.adjustcontactscore.field.label$}",
        ExplanationText = "{$labs.automationextensions.adjustcontactscore.field.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ContactScoreFieldOptionsProvider),
        Order = 110)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.adjustcontactscore.field.required$}")]
    public string Field { get; set; } = string.Empty;


    /// <summary>
    /// Operation name (<see cref="ContactScoreOperation"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.adjustcontactscore.operation.label$}",
        ExplanationText = "{$labs.automationextensions.adjustcontactscore.operation.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ContactScoreOperationOptionsProvider),
        Order = 120)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.adjustcontactscore.operation.required$}")]
    public string Operation { get; set; } = nameof(ContactScoreOperation.Add);


    /// <summary>
    /// Points added or subtracted, or the new value for <see cref="ContactScoreOperation.Set"/>. Never negative – the
    /// direction comes from <see cref="Operation"/>.
    /// </summary>
    [NumberInputComponent(
        Label = "{$labs.automationextensions.adjustcontactscore.amount.label$}",
        ExplanationText = "{$labs.automationextensions.adjustcontactscore.amount.explanation$}",
        Order = 130)]
    [MinimumIntegerValueValidationRule(0, ErrorMessage = "{$labs.automationextensions.adjustcontactscore.amount.minimum$}")]
    public int Amount { get; set; } = 10;


    /// <summary>
    /// Keeps the new value within <see cref="Minimum"/> and <see cref="Maximum"/>.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.adjustcontactscore.limitrange.label$}",
        ExplanationText = "{$labs.automationextensions.adjustcontactscore.limitrange.explanation$}",
        Order = 210)]
    public bool LimitRange { get; set; }


    /// <summary>
    /// Lowest allowed value (inclusive).
    /// </summary>
    [NumberInputComponent(
        Label = "{$labs.automationextensions.adjustcontactscore.minimum.label$}",
        ExplanationText = "{$labs.automationextensions.adjustcontactscore.minimum.explanation$}",
        Order = 220)]
    [VisibleIfTrue(nameof(LimitRange))]
    public int Minimum { get; set; }


    /// <summary>
    /// Highest allowed value (inclusive).
    /// </summary>
    [NumberInputComponent(
        Label = "{$labs.automationextensions.adjustcontactscore.maximum.label$}",
        ExplanationText = "{$labs.automationextensions.adjustcontactscore.maximum.explanation$}",
        Order = 230)]
    [VisibleIfTrue(nameof(LimitRange))]
    public int Maximum { get; set; } = 100;
}
