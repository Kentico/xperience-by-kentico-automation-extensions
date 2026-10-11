using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Marketer-configurable properties of <see cref="StartedByTriggerCondition"/>.
/// </summary>
[FormCategory(Label = "{$labs.automationextensions.category.general$}", Order = 100)]
[FormCategory(Label = "{$labs.automationextensions.startedbytrigger.category.triggerdata$}", Order = 200)]
public sealed class StartedByTriggerProperties : SkippableConditionProperties
{
    /// <summary>
    /// Identifier of the trigger to check (built-in or registered custom trigger).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.startedbytrigger.trigger.label$}",
        ExplanationText = "{$labs.automationextensions.startedbytrigger.trigger.explanation$}",
        DataProviderType = typeof(TriggerOptionsProvider),
        Order = 110)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.startedbytrigger.trigger.required$}")]
    public string Trigger { get; set; } = string.Empty;


    /// <summary>
    /// Also compares a value of the trigger data. Shown only for triggers with data.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.startedbytrigger.checktriggerdata.label$}",
        ExplanationText = "{$labs.automationextensions.startedbytrigger.checktriggerdata.explanation$}",
        Order = 210)]
    [VisibleIfTriggerHasData(nameof(Trigger))]
    public bool CheckTriggerData { get; set; }


    /// <summary>
    /// Name of the trigger data property (e.g. <c>BizFormId</c>).
    /// </summary>
    /// <remarks>
    /// <see cref="TriggerDataFieldConfigurator"/> limits the options to the selected trigger;
    /// <see cref="TriggerDataFieldOptionsProvider"/> (all triggers) is the fallback.
    /// </remarks>
    [DropDownComponent(
        Label = "{$labs.automationextensions.startedbytrigger.field.label$}",
        ExplanationText = "{$labs.automationextensions.startedbytrigger.field.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(TriggerDataFieldOptionsProvider),
        Order = 220)]
    [FormComponentConfiguration(typeof(TriggerDataFieldConfigurator), nameof(Trigger))]
    [VisibleIfTriggerHasData(nameof(Trigger))]
    [VisibleIfTrue(nameof(CheckTriggerData))]
    public string Field { get; set; } = string.Empty;


    /// <summary>
    /// Operator name (<see cref="TriggerDataOperator"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.startedbytrigger.operator.label$}",
        ExplanationText = "{$labs.automationextensions.startedbytrigger.operator.explanation$}",
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(TriggerDataOperatorOptionsProvider),
        Order = 230)]
    [VisibleIfTriggerHasData(nameof(Trigger))]
    [VisibleIfTrue(nameof(CheckTriggerData))]
    public string Operator { get; set; } = nameof(TriggerDataOperator.Equal);


    /// <summary>
    /// Value the trigger data value is compared with. Not used by the empty checks.
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.startedbytrigger.value.label$}",
        ExplanationText = "{$labs.automationextensions.startedbytrigger.value.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 240)]
    [MaxLengthValidationRule(200)]
    [VisibleIfTriggerHasData(nameof(Trigger))]
    [VisibleIfTrue(nameof(CheckTriggerData))]
    [VisibleIfNotEqualTo(nameof(Operator), nameof(TriggerDataOperator.IsEmpty))]
    [VisibleIfNotEqualTo(nameof(Operator), nameof(TriggerDataOperator.IsNotEmpty))]
    public string Value { get; set; } = string.Empty;
}
