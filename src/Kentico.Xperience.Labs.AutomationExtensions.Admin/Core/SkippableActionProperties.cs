using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Base properties for actions that marketers can skip. Xperience doesn't allow deleting steps from processes with contact
/// history, so skipping is the only way to take an action out of such a process.
/// </summary>
/// <remarks>
/// Adds a "Step behavior" category at the bottom of the configuration dialog. Actions check <see cref="SkipStep"/> first
/// and return immediately (see <see cref="StepResult.CreateSkipped"/> for actions that save results).
/// </remarks>
[FormCategory(Label = "{$labs.automationextensions.category.stepbehavior$}", Order = SkippableStep.CATEGORY_ORDER, Collapsible = true)]
public abstract class SkippableActionProperties : IAutomationActionProperties
{
    /// <summary>
    /// Contacts pass through the step without running it.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.skipstep.label$}",
        ExplanationText = "{$labs.automationextensions.skipstep.action.explanation$}",
        Order = SkippableStep.CATEGORY_ORDER + 10)]
    public bool SkipStep { get; set; }
}
