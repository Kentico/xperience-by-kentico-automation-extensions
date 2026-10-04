using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Base properties for conditions that marketers can skip. A skipped condition doesn't evaluate and sends every contact
/// down the branch selected in <see cref="SkippedBranch"/>.
/// </summary>
/// <remarks>
/// Adds a "Step behavior" category at the bottom of the configuration dialog. Conditions call
/// <see cref="SkippableStep.TryGetSkippedResult(SkippableConditionProperties, out bool)"/> first.
/// </remarks>
[FormCategory(Label = "{$labs.automationexperiments.category.stepbehavior$}", Order = SkippableStep.CATEGORY_ORDER, Collapsible = true)]
public abstract class SkippableConditionProperties : IAutomationConditionProperties
{
    /// <summary>
    /// Contacts follow <see cref="SkippedBranch"/> without the condition being evaluated.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationexperiments.skipstep.label$}",
        ExplanationText = "{$labs.automationexperiments.skipstep.condition.explanation$}",
        Order = SkippableStep.CATEGORY_ORDER + 10)]
    public bool SkipStep { get; set; }


    /// <summary>
    /// Branch followed while the step is skipped (<see cref="SkippableStep.BRANCH_YES"/> or <see cref="SkippableStep.BRANCH_NO"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationexperiments.skippedbranch.label$}",
        ExplanationText = "{$labs.automationexperiments.skippedbranch.explanation$}",
        DataProviderType = typeof(SkippedBranchOptionsProvider),
        Order = SkippableStep.CATEGORY_ORDER + 20)]
    [RequiredValidationRule]
    [VisibleIfTrue(nameof(SkipStep))]
    public string SkippedBranch { get; set; } = SkippableStep.BRANCH_NO;
}
