using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Marketer-configurable properties of <see cref="SetContactFieldAction"/>.
/// </summary>
[FormCategory(Label = "{$labs.automationextensions.category.general$}", Order = 100)]
[FormCategory(Label = "{$labs.automationextensions.category.result$}", Order = 200, Collapsible = true)]
public sealed class SetContactFieldActionProperties : SkippableActionProperties
{
    /// <summary>
    /// Default operand pre-filled for new steps.
    /// </summary>
    public const string DEFAULT_OPERAND = "1";


    /// <summary>
    /// Name of the contact field to change (see <see cref="ContactFieldResolver"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.setcontactfield.field.label$}",
        ExplanationText = "{$labs.automationextensions.setcontactfield.field.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ContactFieldOptionsProvider),
        Order = 110)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.setcontactfield.field.required$}")]
    public string Field { get; set; } = string.Empty;


    /// <summary>
    /// Mode name (<see cref="SetContactFieldMode"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.setcontactfield.mode.label$}",
        ExplanationText = "{$labs.automationextensions.setcontactfield.mode.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(SetContactFieldModeOptionsProvider),
        Order = 120)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.setcontactfield.mode.required$}")]
    public string Mode { get; set; } = nameof(SetContactFieldMode.SetValue);


    /// <summary>
    /// Value with tokens (see <see cref="ContactFieldValueTemplate"/>). An empty result clears the field.
    /// </summary>
    [TextAreaComponent(
        Label = "{$labs.automationextensions.setcontactfield.valuetemplate.label$}",
        ExplanationText = "{$labs.automationextensions.setcontactfield.valuetemplate.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 130)]
    [MaxLengthValidationRule(4000)]
    [VisibleIfEqualTo(nameof(Mode), nameof(SetContactFieldMode.SetValue))]
    public string ValueTemplate { get; set; } = string.Empty;


    /// <summary>
    /// Leaves fields that already have a value unchanged.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.setcontactfield.onlyifempty.label$}",
        ExplanationText = "{$labs.automationextensions.setcontactfield.onlyifempty.explanation$}",
        Order = 140)]
    [VisibleIfEqualTo(nameof(Mode), nameof(SetContactFieldMode.SetValue))]
    public bool OnlyIfEmpty { get; set; }


    /// <summary>
    /// Operator name (<see cref="ArithmeticOperator"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.setcontactfield.operator.label$}",
        ExplanationText = "{$labs.automationextensions.setcontactfield.operator.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(ArithmeticOperatorOptionsProvider),
        Order = 150)]
    [VisibleIfEqualTo(nameof(Mode), nameof(SetContactFieldMode.Calculate))]
    public string Operator { get; set; } = nameof(ArithmeticOperator.Add);


    /// <summary>
    /// Number (or tokens resolving to a number) the current value is combined with.
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.setcontactfield.operand.label$}",
        ExplanationText = "{$labs.automationextensions.setcontactfield.operand.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 160)]
    [MaxLengthValidationRule(200)]
    [VisibleIfEqualTo(nameof(Mode), nameof(SetContactFieldMode.Calculate))]
    public string Operand { get; set; } = DEFAULT_OPERAND;


    /// <summary>
    /// Result number the outcome is recorded under (see <see cref="StepResultKeys"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.resultnumber.label$}",
        ExplanationText = "{$labs.automationextensions.setcontactfield.resultnumber.explanation$}",
        DataProviderType = typeof(StepResultNumberOptionsProvider),
        Order = 210)]
    public string ResultNumber { get; set; } = StepResultKeys.DEFAULT_RESULT_NUMBER;
}
