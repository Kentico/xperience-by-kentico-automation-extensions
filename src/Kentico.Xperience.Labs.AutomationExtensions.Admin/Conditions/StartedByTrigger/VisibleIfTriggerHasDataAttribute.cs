using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Shows the decorated property only when the trigger selected in the property named <c>propertyName</c> provides trigger
/// data with at least one readable property (<see cref="TriggerHasDataVisibilityCondition"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class VisibleIfTriggerHasDataAttribute(string propertyName) : VisibilityConditionWithDependencyAttribute(propertyName)
{
}
