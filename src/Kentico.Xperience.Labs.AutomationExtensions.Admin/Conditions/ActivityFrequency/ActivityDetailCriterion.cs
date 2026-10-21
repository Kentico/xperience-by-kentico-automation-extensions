namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// "Column matches text" filter applied when counting activities.
/// </summary>
/// <param name="Column">Activity column name (fixed set from <see cref="ActivityDetailFilters.GetColumn"/>, never marketer input).</param>
/// <param name="Operator">Match operator.</param>
/// <param name="Text">Non-empty, trimmed text (case-insensitive under the default database collation).</param>
public sealed record ActivityDetailCriterion(string Column, TextMatchOperator Operator, string Text) : ActivityCriterion;
