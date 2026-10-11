namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Filter applied when counting activities; all criteria must match. Translated to a parameterized where condition by
/// <see cref="ActivityDetailFilters.ToWhereCondition"/>.
/// </summary>
#pragma warning disable S2094 // Empty base groups detail and page criteria; translation stays in ActivityDetailFilters
public abstract record ActivityCriterion;
#pragma warning restore S2094
