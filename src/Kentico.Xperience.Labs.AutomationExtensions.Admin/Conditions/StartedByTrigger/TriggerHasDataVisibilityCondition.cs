using CMS.Automation.Internal;

using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Hides the trigger data properties of <see cref="StartedByTriggerProperties"/> for triggers without data (no data type
/// or no readable properties) and for unregistered triggers.
/// </summary>
/// <remarks>
/// Depends on the trigger field, so the dialog re-evaluates it whenever the selected trigger changes.
/// </remarks>
[VisibilityConditionAttribute(typeof(VisibleIfTriggerHasDataAttribute))]
public sealed class TriggerHasDataVisibilityCondition(ICustomAutomationTriggerProvider triggerProvider) : VisibilityConditionWithDependency
{
    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;


    /// <inheritdoc />
    public override bool Evaluate(IFormFieldValueProvider formFieldValueProvider) =>
        formFieldValueProvider.TryGet(Properties.PropertyName, out string? identifier)
        && RegisteredTriggers.Find(triggerProvider, identifier)?.DataProperties.Count > 0;
}
