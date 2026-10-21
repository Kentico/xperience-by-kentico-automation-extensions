using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Shows a "contains" filter property only for activity types it applies to (<see cref="ActivityDetailFilters"/>).
/// Built-in <c>VisibleIfEqualTo</c> attributes combine with AND, so they can't express "one of several types".
/// </summary>
/// <remarks>
/// Depends on the activity type field, so the dialog re-evaluates it whenever the selected type changes.
/// </remarks>
[VisibilityConditionAttribute(typeof(VisibleIfActivityDetailFilterAttribute))]
public sealed class ActivityDetailFilterVisibilityCondition : VisibilityConditionWithDependency<ActivityDetailFilterVisibilityConditionProperties>
{
    /// <inheritdoc />
    public override bool Evaluate(IFormFieldValueProvider formFieldValueProvider) =>
        formFieldValueProvider.TryGet<string>(Properties.PropertyName, out string? activityType)
        && ActivityDetailFilters.IsAvailable(activityType, Properties.Filter);
}
