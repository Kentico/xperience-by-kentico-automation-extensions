using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;

/// <summary>
/// Marketer-configurable properties of <see cref="ContactNumberFieldCompareCondition"/>.
/// </summary>
public sealed class ContactNumberFieldCompareProperties : SkippableConditionProperties
{
    /// <summary>
    /// Name of the integer custom contact field to compare (same fields as <see cref="AdjustContactScoreAction"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.contactnumberfieldcompare.field.label$}",
        ExplanationText = "{$labs.automationextensions.contactnumberfieldcompare.field.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ContactScoreFieldOptionsProvider),
        Order = 10)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.contactnumberfieldcompare.field.required$}")]
    public string Field { get; set; } = string.Empty;


    /// <summary>
    /// Operator name (<see cref="NumberComparisonOperator"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.contactnumberfieldcompare.operator.label$}",
        ExplanationText = "{$labs.automationextensions.contactnumberfieldcompare.operator.explanation$}",
        DataProviderType = typeof(NumberComparisonOperatorOptionsProvider),
        Order = 20)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.contactnumberfieldcompare.operator.required$}")]
    public string Operator { get; set; } = nameof(NumberComparisonOperator.GreaterThanOrEqual);


    /// <summary>
    /// Number the field value is compared with. Negative values are allowed.
    /// </summary>
    [NumberInputComponent(
        Label = "{$labs.automationextensions.contactnumberfieldcompare.value.label$}",
        ExplanationText = "{$labs.automationextensions.contactnumberfieldcompare.value.explanation$}",
        Order = 30)]
    public int Value { get; set; } = 50;
}
