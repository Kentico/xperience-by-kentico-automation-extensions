using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// "URL matches all rules" filter applied when counting activities. Matched in .NET on the distinct column values
/// (<see cref="IActivityCounter.CountByValue"/>); the database only applies a parameterized prefilter
/// (<see cref="ActivityDetailFilters.ToWhereCondition"/>).
/// </summary>
/// <param name="Column">Activity column holding the URL (<see cref="ActivityDetailFilters.GetUrlColumn"/>, never marketer input).</param>
/// <param name="RuleSet">Validated, non-empty rules.</param>
public sealed record ActivityUrlMatchCriterion(string Column, UrlMatchRuleSet RuleSet) : ActivityCriterion;
