using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Properties of <see cref="ActivityDetailFilterVisibilityCondition"/>.
/// </summary>
public sealed class ActivityDetailFilterVisibilityConditionProperties : VisibilityConditionWithDependencyProperties
{
    /// <summary>
    /// Filter whose availability decides the visibility.
    /// </summary>
    public ActivityDetailFilter Filter { get; set; }
}
