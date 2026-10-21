using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Shows the decorated property only when <see cref="Filter"/> applies to the activity type selected in the property
/// named <c>propertyName</c> (<see cref="ActivityDetailFilterVisibilityCondition"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class VisibleIfActivityDetailFilterAttribute(string propertyName, ActivityDetailFilter filter) : VisibilityConditionWithDependencyAttribute(propertyName)
{
    /// <summary>
    /// Filter whose availability decides the visibility. Copied by name to <see cref="ActivityDetailFilterVisibilityConditionProperties.Filter"/>.
    /// </summary>
    public ActivityDetailFilter Filter { get; } = filter;
}
