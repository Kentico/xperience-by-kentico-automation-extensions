using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExperiments.Core;

namespace Kentico.Xperience.Labs.AutomationExperiments.Conditions.PreviousStepSucceeded;

/// <summary>
/// Marketer-configurable properties of <see cref="PreviousStepSucceededCondition"/>.
/// </summary>
public sealed class PreviousStepSucceededConditionProperties : SkippableConditionProperties
{
    /// <summary>
    /// Name of the registered <see cref="StepResultSource"/> (step type) whose result is checked.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationexperiments.resultsource.label$}",
        ExplanationText = "{$labs.automationexperiments.resultsource.explanation$}",
        DataProviderType = typeof(StepResultSourceOptionsProvider),
        Order = 10)]
    [RequiredValidationRule]
    public string SourceName { get; set; } = StepResultKeys.SendWebhook.Name;


    /// <summary>
    /// Result number selected on the earlier step.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationexperiments.resultnumber.label$}",
        ExplanationText = "{$labs.automationexperiments.resultnumber.read.explanation$}",
        DataProviderType = typeof(StepResultNumberOptionsProvider),
        Order = 20)]
    [RequiredValidationRule]
    public string ResultNumber { get; set; } = StepResultKeys.DEFAULT_RESULT_NUMBER;
}
