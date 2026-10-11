using CMS.Activities;
using CMS.DataEngine;
using CMS.Websites;

using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

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
/// Titles are built from a localized template, e.g. <c>Page visit 'Home'</c>, <c>Clicked link in email 'Newsletter'</c>, so
/// the title filter compares the quoted name only (<see cref="GetTitlePatterns"/>).
/// <para>
/// URL rules (<see cref="ActivityDetailFilter.Url"/>) match the page URL (<c>ActivityURL</c>) of page visits, landing pages, and
/// form submissions, and the clicked link (<c>ActivityValue</c>) of email link clicks (<see cref="GetUrlColumn"/>).
/// </para>
/// <para>
/// In 31.9.1 only the page visit and landing page loggers set <c>ActivityWebPageItemGUID</c> and <c>ActivityLanguageID</c> reliably (pages from the
/// content tree tracked by the activity logging script). Form submissions, member registrations, email clicks, cross-site
/// and headless activities leave it empty; custom activities have it only when logged by the tracking script's custom
/// activity function on a content tree page.
/// </para>
/// </remarks>
internal static class ActivityDetailFilters
{
    private static readonly HashSet<string> urlTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PredefinedActivityType.PAGE_VISIT,
        PredefinedActivityType.LANDING_PAGE,
        PredefinedActivityType.BIZFORM_SUBMIT,
        PredefinedActivityType.EMAIL_CLICK
    };

    private static readonly HashSet<string> titleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PredefinedActivityType.PAGE_VISIT,
        PredefinedActivityType.LANDING_PAGE,
        PredefinedActivityType.BIZFORM_SUBMIT,
        PredefinedActivityType.MEMBER_REGISTRATION,
        PredefinedActivityType.EMAIL_CLICK
    };

    // Built-in types without a useful ActivityValue, or whose value (email click link URL) is matched by URL rules.
    // All other types (custom, …) get the value filter.
    private static readonly HashSet<string> noValueTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PredefinedActivityType.PAGE_VISIT,
        PredefinedActivityType.LANDING_PAGE,
        PredefinedActivityType.BIZFORM_SUBMIT,
        PredefinedActivityType.MEMBER_REGISTRATION,
        PredefinedActivityType.EMAIL_CLICK
    };

    // Types whose loggers store the content tree page and language.
    private static readonly HashSet<string> pageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PredefinedActivityType.PAGE_VISIT,
        PredefinedActivityType.LANDING_PAGE
    };


    /// <summary>
    /// Returns whether <paramref name="filter"/> applies to <paramref name="activityType"/> (case-insensitive code name).
    /// URL rules apply to page visit, landing page, form submission, and email link click. The value filter applies to all
    /// types except those and member registration. Page and language filters apply to page visit and landing page. No filter
    /// applies while no activity type is selected.
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
            ActivityDetailFilter.Value => !noValueTypes.Contains(activityType),
            ActivityDetailFilter.Page or ActivityDetailFilter.Language => pageTypes.Contains(activityType),
            _ => false
        };
    }


    /// <summary>
    /// Returns the activity column searched by <paramref name="filter"/>. For URL rules the column depends on the activity
    /// type – use <see cref="GetUrlColumn"/>.
    /// </summary>
    public static string GetColumn(ActivityDetailFilter filter) => filter switch
    {
        ActivityDetailFilter.Url => nameof(ActivityInfo.ActivityURL),
        ActivityDetailFilter.Title => nameof(ActivityInfo.ActivityTitle),
        ActivityDetailFilter.Value => nameof(ActivityInfo.ActivityValue),
        ActivityDetailFilter.Page => nameof(ActivityInfo.ActivityWebPageItemGUID),
        ActivityDetailFilter.Language => nameof(ActivityInfo.ActivityLanguageID),
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown filter.")
    };


    /// <summary>
    /// Returns the activity column URL rules match for <paramref name="activityType"/>: the clicked link
    /// (<c>ActivityValue</c>) for email link clicks, the page URL (<c>ActivityURL</c>) otherwise. Never the referrer.
    /// </summary>
    public static string GetUrlColumn(string activityType) =>
        string.Equals(activityType, PredefinedActivityType.EMAIL_CLICK, StringComparison.OrdinalIgnoreCase)
            ? nameof(ActivityInfo.ActivityValue)
            : nameof(ActivityInfo.ActivityURL);


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
    /// Returns the distinct, non-empty page GUIDs of a stored page selection, in selection order. Pages deleted after
    /// selection keep their GUID – they just never match.
    /// </summary>
    public static IReadOnlyList<Guid> GetPageGuids(IEnumerable<WebPageRelatedItem?>? pages) =>
        pages is null
            ? []
            : pages
                .Where(page => page is not null && page.WebPageGuid != Guid.Empty)
                .Select(page => page!.WebPageGuid)
                .Distinct()
                .ToList();


    /// <summary>
    /// Builds the filters to apply for <paramref name="activityType"/> (all must match) from the filters visible for the
    /// type that have a value: text filters with text (trimmed), the page filter with at least one page, the language
    /// filter with a language other than "Any", and URL rules with at least one rule. Hidden filters and empty values are
    /// ignored, whatever their operator.
    /// </summary>
    /// <param name="activityType">Resolved activity type code name.</param>
    /// <param name="properties">Step properties.</param>
    /// <param name="languageId">ID resolved from <see cref="ActivityFrequencyProperties.Language"/>; <c>null</c> when it doesn't exist.</param>
    /// <param name="criteria">Filters to apply; empty when invalid.</param>
    /// <returns>Why a filter can't be applied (unknown operator or language, invalid URL rules), or <c>null</c> when <paramref name="criteria"/> is valid.</returns>
    public static string? GetCriteria(string activityType, ActivityFrequencyProperties properties, int? languageId, out IReadOnlyList<ActivityCriterion> criteria)
    {
        var result = new List<ActivityCriterion>();
        criteria = result;

        if (IsAvailable(activityType, ActivityDetailFilter.Page) && GetPageGuids(properties.Pages) is { Count: > 0 } pageGuids)
        {
            result.Add(new ActivityPageCriterion(pageGuids));
        }

        if (IsLanguageApplied(activityType, properties))
        {
            if (languageId is not { } id)
            {
                criteria = [];
                return $"language '{properties.Language}' doesn't exist";
            }

            result.Add(new ActivityLanguageCriterion(id));
        }

        if (IsAvailable(activityType, ActivityDetailFilter.Url))
        {
            if (!UrlMatchRuleSet.TryParse(properties.UrlRules, out var ruleSet, out var ruleError))
            {
                criteria = [];
                return $"URL rules are invalid ({ruleError!.Describe()})";
            }

            if (ruleSet.Rules.Count > 0)
            {
                result.Add(new ActivityUrlMatchCriterion(GetUrlColumn(activityType), ruleSet));
            }
        }

        foreach (var (filter, operatorName, text) in new[]
        {
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
    /// Returns whether the language filter is visible for <paramref name="activityType"/> and set to a language (not "Any"),
    /// i.e. whether <see cref="ActivityFrequencyProperties.Language"/> must be resolved before <see cref="GetCriteria"/>.
    /// </summary>
    public static bool IsLanguageApplied(string? activityType, ActivityFrequencyProperties properties) =>
        IsAvailable(activityType, ActivityDetailFilter.Language) && !string.IsNullOrWhiteSpace(properties.Language);


    /// <summary>
    /// Translates <paramref name="criterion"/> to a parameterized where condition. Text filters: LIKE wildcards in the text
    /// are escaped by the DataEngine; negated operators also match empty (NULL) columns. Page filter: one parameter per page
    /// GUID, combined with OR (= <c>IN</c>). Language filter: parameterized equality. URL rules: only the prefilter – one
    /// parameterized <c>LIKE '%literal%'</c> per positive rule with a literal (<see cref="UrlMatcher.GetPrefilterLiterals"/>),
    /// an empty condition when there is none; the rules themselves are matched in .NET.
    /// </summary>
    public static WhereCondition ToWhereCondition(ActivityCriterion criterion) => criterion switch
    {
        ActivityDetailCriterion detail => ToTextWhereCondition(detail),
        ActivityPageCriterion page => ToPageWhereCondition(page),
        ActivityUrlMatchCriterion urlMatch => ToUrlPrefilterWhereCondition(urlMatch),
        ActivityLanguageCriterion language => new WhereCondition().WhereEquals(GetColumn(ActivityDetailFilter.Language), language.LanguageId),
        _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion, "Unknown criterion.")
    };


    /// <remarks>
    /// Not <c>WhereIn</c>: for lists below <c>SqlHelper.DefaultSQLInlineLimit</c> it inlines the GUIDs as SQL literals and
    /// reads the limit from settings. Equality conditions stay parameterized.
    /// </remarks>
    private static WhereCondition ToPageWhereCondition(ActivityPageCriterion criterion)
    {
        string column = GetColumn(ActivityDetailFilter.Page);
        var condition = new WhereCondition();

        foreach (var guid in criterion.WebPageGuids)
        {
            condition = condition.Or().WhereEquals(column, guid);
        }

        return condition;
    }


    private static WhereCondition ToUrlPrefilterWhereCondition(ActivityUrlMatchCriterion criterion)
    {
        var condition = new WhereCondition();
        foreach (string literal in UrlMatcher.GetPrefilterLiterals(criterion.RuleSet))
        {
            condition = condition.WhereContains(criterion.Column, literal);
        }

        return condition;
    }


    private static WhereCondition ToTextWhereCondition(ActivityDetailCriterion criterion)
    {
        string column = criterion.Column;
        string text = criterion.Text;

        if (string.Equals(column, GetColumn(ActivityDetailFilter.Title), StringComparison.Ordinal))
        {
            return ToTitleWhereCondition(column, criterion.Operator, text);
        }

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


    /// <summary>
    /// Returns the LIKE patterns matching <paramref name="text"/> against the quoted name of a template title
    /// (<c>&lt;prefix&gt; '&lt;name&gt;'</c>): the title must match <c>Like</c> and, when set, must not match <c>NotLike</c>.
    /// Negated operators use the patterns of their positive counterpart.
    /// </summary>
    /// <remarks>
    /// Relies only on the quotes (template prefixes are localized resources and contain no apostrophe): the name starts
    /// after the first apostrophe and ends before the last character. Wildcards in <paramref name="text"/> are escaped;
    /// apostrophes in it need no escaping because the patterns are passed as parameters.
    /// <list type="bullet">
    /// <item>Equal: title ends with <c>'text'</c> and no apostrophe precedes that quote – so "Brien" doesn't match "O'Brien".</item>
    /// <item>Contains / EndsWith: text lies between the opening and the closing quote (never in the prefix).</item>
    /// <item>StartsWith: an apostrophe is directly followed by text – also matches a name containing an apostrophe followed
    /// by text (e.g. "Brien" matches "O'Brien").</item>
    /// </list>
    /// </remarks>
    internal static (string Like, string? NotLike) GetTitlePatterns(TextMatchOperator textOperator, string text)
    {
        string escaped = SqlHelper.EscapeLikeQueryPatterns(text);

        return textOperator switch
        {
            TextMatchOperator.Equal or TextMatchOperator.NotEqual => ($"%'{escaped}'", $"%'%'{escaped}'"),
            TextMatchOperator.Contains or TextMatchOperator.DoesNotContain => ($"%'%{escaped}%'", null),
            TextMatchOperator.StartsWith => ($"%'{escaped}%'", null),
            TextMatchOperator.EndsWith => ($"%'%{escaped}'", null),
            _ => throw new ArgumentOutOfRangeException(nameof(textOperator), textOperator, "Unknown operator.")
        };
    }


    private static WhereCondition ToTitleWhereCondition(string column, TextMatchOperator textOperator, string text)
    {
        var (like, notLike) = GetTitlePatterns(textOperator, text);
        var match = new WhereCondition().WhereLike(column, like);
        if (notLike is not null)
        {
            match = match.WhereNotLike(column, notLike);
        }

        return textOperator is TextMatchOperator.DoesNotContain or TextMatchOperator.NotEqual
            ? new WhereCondition().WhereNot(match).Or().WhereNull(column)
            : match;
    }
}
