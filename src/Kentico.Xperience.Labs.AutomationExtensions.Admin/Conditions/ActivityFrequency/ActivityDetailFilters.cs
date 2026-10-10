using CMS.Activities;
using CMS.DataEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Which detail filters apply to which activity type, and how they translate to query conditions. Single source of truth
/// for the property visibility in the step dialog (<see cref="ActivityDetailFilterVisibilityCondition"/>) and the filters
/// applied at runtime, so values left in hidden properties after switching the activity type never filter.
/// </summary>
/// <remarks>
/// Xperience stores the useful details of built-in activities in different columns: page visits and form submissions have
/// no <c>ActivityValue</c>. The referrer URL is never searched – it would count a visit of /contact coming from /pricing as
/// a pricing visit. The built-in "with value that contains" rule searches <c>ActivityValue</c> only.
/// </remarks>
internal static class ActivityDetailFilters
{
    private static readonly HashSet<string> urlTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PredefinedActivityType.PAGE_VISIT,
        PredefinedActivityType.LANDING_PAGE,
        PredefinedActivityType.BIZFORM_SUBMIT
    };

    private static readonly HashSet<string> titleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PredefinedActivityType.BIZFORM_SUBMIT,
        PredefinedActivityType.MEMBER_REGISTRATION
    };


    /// <summary>
    /// Returns whether <paramref name="filter"/> applies to <paramref name="activityType"/> (case-insensitive code name).
    /// The value filter applies to all types without a URL or title filter (email click, custom, and other types).
    /// No filter applies while no activity type is selected.
    /// </summary>
    public static bool IsAvailable(string? activityType, ActivityDetailFilter filter)
    {
        if (string.IsNullOrWhiteSpace(activityType))
        {
            return false;
        }

        return filter switch
        {
            ActivityDetailFilter.Url => urlTypes.Contains(activityType),
            ActivityDetailFilter.Title => titleTypes.Contains(activityType),
            ActivityDetailFilter.Value => !urlTypes.Contains(activityType) && !titleTypes.Contains(activityType),
            _ => false
        };
    }


    /// <summary>
    /// Returns the activity column searched by <paramref name="filter"/>.
    /// </summary>
    public static string GetColumn(ActivityDetailFilter filter) => filter switch
    {
        ActivityDetailFilter.Url => nameof(ActivityInfo.ActivityURL),
        ActivityDetailFilter.Title => nameof(ActivityInfo.ActivityTitle),
        ActivityDetailFilter.Value => nameof(ActivityInfo.ActivityValue),
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown filter.")
    };


    /// <summary>
    /// Parses an operator persisted by name (exact, case-sensitive). Numeric values are rejected.
    /// </summary>
    public static bool TryParseOperator(string? value, out TextMatchOperator textOperator)
    {
        textOperator = default;

        return value is not null
            && Enum.GetNames<TextMatchOperator>().Contains(value, StringComparer.Ordinal)
            && Enum.TryParse(value, out textOperator);
    }


    /// <summary>
    /// Builds the filters to apply for <paramref name="activityType"/> (all must match) from the filters visible for the
    /// type that have text (trimmed). Hidden filters and empty texts are ignored, whatever their operator.
    /// </summary>
    /// <returns>Why a filter can't be applied (unknown operator), or <c>null</c> when <paramref name="criteria"/> is valid.</returns>
    public static string? GetCriteria(string activityType, ActivityFrequencyProperties properties, out IReadOnlyList<ActivityDetailCriterion> criteria)
    {
        var result = new List<ActivityDetailCriterion>();
        criteria = result;

        foreach (var (filter, operatorName, text) in new[]
        {
            (ActivityDetailFilter.Url, properties.UrlOperator, properties.Url),
            (ActivityDetailFilter.Title, properties.TitleOperator, properties.Title),
            (ActivityDetailFilter.Value, properties.ValueOperator, properties.Value)
        })
        {
            if (!IsAvailable(activityType, filter) || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (!TryParseOperator(operatorName, out var textOperator))
            {
                criteria = [];
                return $"{filter.ToString().ToLowerInvariant()} operator '{operatorName}' is unknown";
            }

            result.Add(new ActivityDetailCriterion(GetColumn(filter), textOperator, text.Trim()));
        }

        return null;
    }


    /// <summary>
    /// Translates <paramref name="criterion"/> to a parameterized where condition (LIKE wildcards in the text are escaped by
    /// the DataEngine). Negated operators also match empty (NULL) columns.
    /// </summary>
    public static WhereCondition ToWhereCondition(ActivityDetailCriterion criterion)
    {
        string column = criterion.Column;
        string text = criterion.Text;

        return criterion.Operator switch
        {
            TextMatchOperator.Contains => new WhereCondition().WhereContains(column, text),
            TextMatchOperator.DoesNotContain => new WhereCondition().WhereNotContains(column, text).Or().WhereNull(column),
            TextMatchOperator.Equal => new WhereCondition().WhereEquals(column, text),
            TextMatchOperator.NotEqual => new WhereCondition().WhereNotEquals(column, text).Or().WhereNull(column),
            TextMatchOperator.StartsWith => new WhereCondition().WhereStartsWith(column, text),
            TextMatchOperator.EndsWith => new WhereCondition().WhereEndsWith(column, text),
            _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion.Operator, "Unknown operator.")
        };
    }
}
