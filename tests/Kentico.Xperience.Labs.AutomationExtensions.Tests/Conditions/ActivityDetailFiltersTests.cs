using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;

using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;
using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class ActivityDetailFiltersTests
{
    [TestCase("pagevisit", true, true, false, true)]
    [TestCase("landingpage", true, true, false, true)]
    [TestCase("bizformsubmit", true, true, false, false)]
    [TestCase("memberregistration", false, true, false, false)]
    [TestCase("emailclick", true, true, false, false)]
    [TestCase("click", false, false, true, false)]
    [TestCase("datainput", false, false, true, false)]
    [TestCase("custom_calculator", false, false, true, false)]
    [TestCase("PageVisit", true, true, false, true)]
    [TestCase("LandingPage", true, true, false, true)]
    [TestCase("BIZFORMSUBMIT", true, true, false, false)]
    [TestCase("MemberRegistration", false, true, false, false)]
    [TestCase("EmailClick", true, true, false, false)]
    [TestCase("", false, false, false, false)]
    [TestCase(" ", false, false, false, false)]
    [TestCase(null, false, false, false, false)]
    public void IsAvailable_MapsActivityTypesToFilters(string? activityType, bool url, bool title, bool value, bool page) =>
        Assert.Multiple(() =>
        {
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Url), Is.EqualTo(url));
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Title), Is.EqualTo(title));
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Value), Is.EqualTo(value));
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Page), Is.EqualTo(page));
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Language), Is.EqualTo(page), "Language is stored with the page.");
        });


    [TestCase(ActivityDetailFilter.Url, ExpectedResult = "ActivityURL")]
    [TestCase(ActivityDetailFilter.Title, ExpectedResult = "ActivityTitle")]
    [TestCase(ActivityDetailFilter.Value, ExpectedResult = "ActivityValue")]
    [TestCase(ActivityDetailFilter.Page, ExpectedResult = "ActivityWebPageItemGUID")]
    [TestCase(ActivityDetailFilter.Language, ExpectedResult = "ActivityLanguageID")]
    public string GetColumn_NeverReferrer(ActivityDetailFilter filter) => ActivityDetailFilters.GetColumn(filter);


    [Test]
    public void GetColumn_UnknownFilterThrows() => Assert.That(
        () => ActivityDetailFilters.GetColumn((ActivityDetailFilter)42),
        Throws.TypeOf<ArgumentOutOfRangeException>());


    private const string PRICING_RULES = """{"version":1,"rules":[{"part":"Path","operator":"StartsWith","value":"/pricing","parameter":null}]}""";


    [TestCase("pagevisit", new[] { "ActivityURL rules", "ActivityTitle Equal Contact form" })]
    [TestCase("landingpage", new[] { "ActivityURL rules", "ActivityTitle Equal Contact form" })]
    [TestCase("bizformsubmit", new[] { "ActivityURL rules", "ActivityTitle Equal Contact form" })]
    [TestCase("memberregistration", new[] { "ActivityTitle Equal Contact form" })]
    [TestCase("emailclick", new[] { "ActivityValue rules", "ActivityTitle Equal Contact form" })]
    [TestCase("custom_calculator", new[] { "ActivityValue DoesNotContain utm_campaign=fall" })]
    public void GetCriteria_AppliesOnlyFiltersVisibleForTypeAndTrimsText(string activityType, string[] expected)
    {
        // All filters are filled, as if left over from previously selected activity types.
        var properties = new ActivityFrequencyProperties
        {
            UrlRules = PRICING_RULES,
            TitleOperator = nameof(TextMatchOperator.Equal),
            Title = "Contact form",
            ValueOperator = nameof(TextMatchOperator.DoesNotContain),
            Value = "utm_campaign=fall "
        };

        string? error = ActivityDetailFilters.GetCriteria(activityType, properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria.Select(criterion => criterion switch
            {
                ActivityDetailCriterion detail => $"{detail.Column} {detail.Operator} {detail.Text}",
                ActivityUrlMatchCriterion url => $"{url.Column} rules",
                _ => criterion.ToString()
            }), Is.EqualTo(expected));
        });
    }


    [TestCase("pagevisit", ExpectedResult = "ActivityURL")]
    [TestCase("LandingPage", ExpectedResult = "ActivityURL")]
    [TestCase("bizformsubmit", ExpectedResult = "ActivityURL")]
    [TestCase("emailclick", ExpectedResult = "ActivityValue")]
    [TestCase("EmailClick", ExpectedResult = "ActivityValue")]
    public string GetUrlColumn_ClickedLinkForEmailClicksPageUrlOtherwise(string activityType) => ActivityDetailFilters.GetUrlColumn(activityType);


    [Test]
    public void GetCriteria_UrlRulesAreParsedIntoCriterion()
    {
        var properties = new ActivityFrequencyProperties { UrlRules = PRICING_RULES };

        string? error = ActivityDetailFilters.GetCriteria("pagevisit", properties, null, out var criteria);

        var criterion = criteria.OfType<ActivityUrlMatchCriterion>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criterion.Column, Is.EqualTo("ActivityURL"));
            Assert.That(criterion.RuleSet.Rules, Is.EqualTo(new[] { new UrlMatchRule(UrlPart.Path, UrlMatchOperator.StartsWith, "/pricing") }));
        });
    }


    [TestCase("")]
    [TestCase("  ")]
    [TestCase("""{"version":1,"rules":[]}""")]
    public void GetCriteria_EmptyUrlRulesMeanNoFilter(string urlRules)
    {
        string? error = ActivityDetailFilters.GetCriteria("pagevisit", new ActivityFrequencyProperties { UrlRules = urlRules }, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.Empty);
        });
    }


    [TestCase("not json")]
    [TestCase("""{"version":2,"rules":[]}""")]
    [TestCase("""{"version":1,"rules":[{"part":"Path","operator":"Equal","value":""}]}""")]
    public void GetCriteria_InvalidUrlRulesOfVisibleFilterAreError(string urlRules)
    {
        string? error = ActivityDetailFilters.GetCriteria("bizformsubmit", new ActivityFrequencyProperties { UrlRules = urlRules }, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Does.StartWith("URL rules are invalid ("));
            Assert.That(criteria, Is.Empty);
        });
    }


    [Test]
    public void GetCriteria_InvalidUrlRulesOfHiddenFilterAreIgnored()
    {
        string? error = ActivityDetailFilters.GetCriteria("custom_calculator", new ActivityFrequencyProperties { UrlRules = "not json" }, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.Empty);
        });
    }


    [Test]
    public void ToWhereCondition_UrlRulesBecomeParameterizedPrefilterForPositiveRulesOnly()
    {
        UrlMatchRuleSet.TryParse(
            """
            {"version":1,"rules":[
              {"part":"Path","operator":"StartsWith","value":"/pricing_[50%]"},
              {"part":"Host","operator":"NotEqual","value":"internal.example.com"},
              {"part":"QueryParameter","operator":"Exists","parameter":"utm_campaign"}
            ]}
            """,
            out var ruleSet,
            out _);

        var condition = ActivityDetailFilters.ToWhereCondition(new ActivityUrlMatchCriterion("ActivityURL", ruleSet));

        string sql = condition.WhereCondition;
        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Match(@"^\[ActivityURL\] LIKE @\w+ AND \[ActivityURL\] LIKE @\w+$"));
            Assert.That(sql, Does.Not.Contain("pricing").And.Not.Contain("utm").And.Not.Contain("internal"), "Marketer text must not be part of the SQL text.");
            // Wildcard characters of the literal are escaped; the negated host rule adds nothing.
            Assert.That(condition.Parameters.Select(parameter => parameter.Value), Is.EqualTo(new object[] { "%pricing[_]%", "%utm[_]campaign%" }));
        });
    }


    [Test]
    public void ToWhereCondition_UrlRulesWithoutLiteralAreEmpty()
    {
        UrlMatchRuleSet.TryParse("""{"version":1,"rules":[{"part":"Path","operator":"DoesNotContain","value":"/blog"},{"part":"Path","operator":"Equal","value":"/"}]}""", out var ruleSet, out _);

        var condition = ActivityDetailFilters.ToWhereCondition(new ActivityUrlMatchCriterion("ActivityURL", ruleSet));

        Assert.That(condition.WhereIsEmpty, Is.True);
    }


    [TestCase("", nameof(TextMatchOperator.Contains))]
    [TestCase("   ", nameof(TextMatchOperator.NotEqual))]
    [TestCase("", "Unknown")]
    public void GetCriteria_EmptyTextsMeanNoFilterWhateverOperator(string text, string textOperator)
    {
        var properties = new ActivityFrequencyProperties
        {
            TitleOperator = textOperator,
            Title = text,
            ValueOperator = textOperator,
            Value = text
        };

        string? error = ActivityDetailFilters.GetCriteria("bizformsubmit", properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.Empty);
        });
    }


    [Test]
    public void GetCriteria_UnknownOperatorOfHiddenFilterIsIgnored()
    {
        var properties = new ActivityFrequencyProperties
        {
            ValueOperator = "Unknown",
            Value = "stale",
            TitleOperator = nameof(TextMatchOperator.Equal),
            Title = "Home"
        };

        string? error = ActivityDetailFilters.GetCriteria("pagevisit", properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.EqualTo(new[] { new ActivityDetailCriterion("ActivityTitle", TextMatchOperator.Equal, "Home") }));
        });
    }


    [TestCase("Unknown")]
    [TestCase("contains")]
    [TestCase("1")]
    [TestCase("")]
    public void GetCriteria_UnknownOperatorOfAppliedFilterIsError(string textOperator)
    {
        var properties = new ActivityFrequencyProperties { TitleOperator = textOperator, Title = "Contact form" };

        string? error = ActivityDetailFilters.GetCriteria("bizformsubmit", properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.EqualTo($"title operator '{textOperator}' is unknown"));
            Assert.That(criteria, Is.Empty);
        });
    }


    [Test]
    public void GetPageGuids_SkipsEmptyAndDuplicatesInSelectionOrder()
    {
        var first = Guid.Parse("8b4a3c7e-1d2f-4e5a-9b6c-0d1e2f3a4b5c");
        var second = Guid.Parse("0f9e8d7c-6b5a-4c3d-8e1f-a2b3c4d5e6f7");

        var guids = ActivityDetailFilters.GetPageGuids(
        [
            new WebPageRelatedItem { WebPageGuid = second },
            null,
            new WebPageRelatedItem { WebPageGuid = Guid.Empty },
            new WebPageRelatedItem { WebPageGuid = first },
            new WebPageRelatedItem { WebPageGuid = second }
        ]);

        Assert.That(guids, Is.EqualTo(new[] { second, first }));
    }


    [Test]
    public void GetPageGuids_NullSelectionIsEmpty() => Assert.That(ActivityDetailFilters.GetPageGuids(null), Is.Empty);


    [Test]
    public void GetPageGuids_ReadsValueStoredByAutomationStepJson()
    {
        // Step properties are stored per property as System.Text.Json (CMS.Automation CustomAutomationStepPropertyMapper);
        // the page selector value is a list of { WebPageGuid }.
        const string stored = """[{"WebPageGuid":"8b4a3c7e-1d2f-4e5a-9b6c-0d1e2f3a4b5c"},{"WebPageGuid":"0f9e8d7c-6b5a-4c3d-8e1f-a2b3c4d5e6f7"}]""";

        var pages = JsonSerializer.Deserialize<IEnumerable<WebPageRelatedItem>>(stored);

        Assert.That(ActivityDetailFilters.GetPageGuids(pages), Is.EqualTo(new[]
        {
            Guid.Parse("8b4a3c7e-1d2f-4e5a-9b6c-0d1e2f3a4b5c"),
            Guid.Parse("0f9e8d7c-6b5a-4c3d-8e1f-a2b3c4d5e6f7")
        }));
    }


    [TestCase("pagevisit", true, true)]
    [TestCase("LandingPage", true, true)]
    [TestCase("bizformsubmit", false, true)]
    [TestCase("emailclick", false, true)]
    [TestCase("custom_calculator", false, false)]
    public void GetCriteria_PageFilterAppliesOnlyToPageTypes(string activityType, bool expectPage, bool expectUrl)
    {
        var page = Guid.Parse("8b4a3c7e-1d2f-4e5a-9b6c-0d1e2f3a4b5c");
        var properties = new ActivityFrequencyProperties
        {
            Pages = [new WebPageRelatedItem { WebPageGuid = page }],
            UrlRules = PRICING_RULES
        };

        string? error = ActivityDetailFilters.GetCriteria(activityType, properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria.OfType<ActivityPageCriterion>().SelectMany(criterion => criterion.WebPageGuids),
                Is.EqualTo(expectPage ? new[] { page } : Array.Empty<Guid>()));
            Assert.That(criteria.OfType<ActivityUrlMatchCriterion>().Any(), Is.EqualTo(expectUrl));
        });
    }


    [Test]
    public void GetCriteria_EmptyPageSelectionMeansNoPageFilter()
    {
        var properties = new ActivityFrequencyProperties { Pages = [new WebPageRelatedItem { WebPageGuid = Guid.Empty }] };

        string? error = ActivityDetailFilters.GetCriteria("pagevisit", properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.Empty);
        });
    }


    [Test]
    public void ToWhereCondition_PageCriterionIsOneParameterPerPage()
    {
        var first = Guid.Parse("8b4a3c7e-1d2f-4e5a-9b6c-0d1e2f3a4b5c");
        var second = Guid.Parse("0f9e8d7c-6b5a-4c3d-8e1f-a2b3c4d5e6f7");

        var condition = ActivityDetailFilters.ToWhereCondition(new ActivityPageCriterion([first, second]));

        string sql = condition.WhereCondition;

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Match(@"^\[ActivityWebPageItemGUID\] = @\w+ OR \[ActivityWebPageItemGUID\] = @\w+$"));
            Assert.That(sql, Does.Not.Contain(first.ToString()), "GUIDs must not be part of the SQL text.");
            Assert.That(condition.Parameters.Select(parameter => parameter.Value), Is.EquivalentTo(new object[] { first, second }));
        });
    }


    [Test]
    public void ToWhereCondition_PageAndTextCriteriaCombineWithAnd()
    {
        // Same composition as ActivityCounter.
        var combined = new WhereCondition()
            .WhereEquals("ActivityContactID", 42)
            .Where(ActivityDetailFilters.ToWhereCondition(new ActivityPageCriterion([Guid.NewGuid(), Guid.NewGuid()])))
            .Where(ActivityDetailFilters.ToWhereCondition(new ActivityDetailCriterion("ActivityURL", TextMatchOperator.NotEqual, "x")));

        Assert.That(combined.WhereCondition, Does.Match(@"^\[ActivityContactID\] = @\w+ AND \(\[ActivityWebPageItemGUID\] = @\w+ OR \[ActivityWebPageItemGUID\] = @\w+\) AND \(\[ActivityURL\] <> @\w+ OR \[ActivityURL\] IS NULL\)$"));
    }


    [Test]
    public void ToWhereCondition_UnknownCriterionThrows() => Assert.That(
        () => ActivityDetailFilters.ToWhereCondition(new UnknownCriterion()),
        Throws.TypeOf<ArgumentOutOfRangeException>());


    [TestCase("pagevisit")]
    [TestCase("LandingPage")]
    public void GetCriteria_LanguageAppliesResolvedId(string activityType)
    {
        var properties = new ActivityFrequencyProperties { Language = "es" };

        string? error = ActivityDetailFilters.GetCriteria(activityType, properties, 7, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(ActivityDetailFilters.IsLanguageApplied(activityType, properties), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.EqualTo(new[] { new ActivityLanguageCriterion(7) }));
        });
    }


    [Test]
    public void GetCriteria_UnknownLanguageOfAppliedFilterIsError()
    {
        var properties = new ActivityFrequencyProperties { Language = "deleted", UrlRules = PRICING_RULES };

        string? error = ActivityDetailFilters.GetCriteria("pagevisit", properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.EqualTo("language 'deleted' doesn't exist"));
            Assert.That(criteria, Is.Empty);
        });
    }


    [TestCase("bizformsubmit", "deleted")]
    [TestCase("emailclick", "deleted")]
    [TestCase("pagevisit", "")]
    [TestCase("pagevisit", " ")]
    public void GetCriteria_HiddenOrAnyLanguageIsIgnored(string activityType, string language)
    {
        var properties = new ActivityFrequencyProperties { Language = language };

        string? error = ActivityDetailFilters.GetCriteria(activityType, properties, null, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(ActivityDetailFilters.IsLanguageApplied(activityType, properties), Is.False, "No lookup needed.");
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.Empty);
        });
    }


    [Test]
    public void ToWhereCondition_LanguageCriterionIsParameterized()
    {
        var condition = ActivityDetailFilters.ToWhereCondition(new ActivityLanguageCriterion(7));

        Assert.Multiple(() =>
        {
            Assert.That(condition.WhereCondition, Does.Match(@"^\[ActivityLanguageID\] = @\w+$"));
            Assert.That(condition.Parameters.Single().Value, Is.EqualTo(7));
        });
    }


    [TestCase("Contains", true, TextMatchOperator.Contains)]
    [TestCase("DoesNotContain", true, TextMatchOperator.DoesNotContain)]
    [TestCase("Equal", true, TextMatchOperator.Equal)]
    [TestCase("NotEqual", true, TextMatchOperator.NotEqual)]
    [TestCase("StartsWith", true, TextMatchOperator.StartsWith)]
    [TestCase("EndsWith", true, TextMatchOperator.EndsWith)]
    [TestCase("contains", false, default(TextMatchOperator))]
    [TestCase("1", false, default(TextMatchOperator))]
    [TestCase("", false, default(TextMatchOperator))]
    [TestCase(null, false, default(TextMatchOperator))]
    [TestCase("Contains,Equal", false, default(TextMatchOperator))]
    public void TryParseOperator_AcceptsExactNamesOnly(string? value, bool expectedResult, TextMatchOperator expectedOperator)
    {
        bool result = ActivityDetailFilters.TryParseOperator(value, out var textOperator);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(textOperator, Is.EqualTo(expectedOperator));
        });
    }


    [Test]
    public void TextMatchOperator_PersistedNamesNeverChange() => Assert.That(
        Enum.GetNames<TextMatchOperator>(),
        Is.EqualTo(new[] { "Contains", "DoesNotContain", "Equal", "NotEqual", "StartsWith", "EndsWith" }));


    [TestCase(TextMatchOperator.Contains, "LIKE", "%a[[]b[%]c[_]d%", false)]
    [TestCase(TextMatchOperator.DoesNotContain, "NOT LIKE", "%a[[]b[%]c[_]d%", true)]
    [TestCase(TextMatchOperator.Equal, "=", "a[b%c_d", false)]
    [TestCase(TextMatchOperator.NotEqual, "<>", "a[b%c_d", true)]
    [TestCase(TextMatchOperator.StartsWith, "LIKE", "a[[]b[%]c[_]d%", false)]
    [TestCase(TextMatchOperator.EndsWith, "LIKE", "%a[[]b[%]c[_]d", false)]
    public void ToWhereCondition_IsParameterizedEscapedAndNegationsIncludeNull(TextMatchOperator textOperator, string sqlOperator, string expectedParameter, bool includesNull)
    {
        var condition = ActivityDetailFilters.ToWhereCondition(new ActivityDetailCriterion("ActivityURL", textOperator, "a[b%c_d"));

        string sql = condition.WhereCondition;
        var parameter = condition.Parameters.Single();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain($"[ActivityURL] {sqlOperator} {parameter.Name}"));
            Assert.That(sql, Does.Not.Contain("a[b"), "Marketer text must not be part of the SQL text.");
            Assert.That(parameter.Value, Is.EqualTo(expectedParameter));
            Assert.That(sql.Contains("[ActivityURL] IS NULL", StringComparison.Ordinal), Is.EqualTo(includesNull));
            Assert.That(sql.Contains(" OR ", StringComparison.Ordinal), Is.EqualTo(includesNull));
        });
    }


    [TestCase(TextMatchOperator.Equal, "Home", "Page visit 'Home'", true)]
    [TestCase(TextMatchOperator.Equal, "home", "Landing page 'Home'", true)]
    [TestCase(TextMatchOperator.Equal, "Home", "Page visit 'Home page'", false)]
    [TestCase(TextMatchOperator.Equal, "Home", "Page visit 'My Home'", false)]
    [TestCase(TextMatchOperator.Equal, "Page visit 'Home", "Page visit 'Home'", false)]
    [TestCase(TextMatchOperator.Equal, "O'Brien", "Member registered as 'O'Brien'", true)]
    [TestCase(TextMatchOperator.Equal, "Brien", "Member registered as 'O'Brien'", false)]
    [TestCase(TextMatchOperator.Equal, "'Brien", "Member registered as 'O'Brien'", false)]
    [TestCase(TextMatchOperator.Equal, "50%_[x]", "Form submitted '50%_[x]'", true)]
    [TestCase(TextMatchOperator.Equal, "50%_[x]", "Form submitted '50 off x'", false)]
    [TestCase(TextMatchOperator.Equal, "50%_[x]", "Form submitted '50ab[x]'", false)]
    [TestCase(TextMatchOperator.NotEqual, "Home", "Page visit 'Home page'", true)]
    [TestCase(TextMatchOperator.NotEqual, "Home", "Page visit 'Home'", false)]
    [TestCase(TextMatchOperator.NotEqual, "Brien", "Member registered as 'O'Brien'", true)]
    [TestCase(TextMatchOperator.StartsWith, "Home", "Page visit 'Home page'", true)]
    [TestCase(TextMatchOperator.StartsWith, "Home", "Page visit 'Home'", true)]
    [TestCase(TextMatchOperator.StartsWith, "Home", "Page visit 'My Home'", false)]
    [TestCase(TextMatchOperator.StartsWith, "Page", "Page visit 'Home'", false)]
    [TestCase(TextMatchOperator.StartsWith, "O'B", "Member registered as 'O'Brien'", true)]
    [TestCase(TextMatchOperator.StartsWith, "a_", "Clicked link in email 'ab news'", false)]
    // Known limitation: an apostrophe inside the name also starts a match.
    [TestCase(TextMatchOperator.StartsWith, "Brien", "Member registered as 'O'Brien'", true)]
    [TestCase(TextMatchOperator.EndsWith, "page", "Page visit 'Home page'", true)]
    [TestCase(TextMatchOperator.EndsWith, "Home", "Page visit 'My Home'", true)]
    [TestCase(TextMatchOperator.EndsWith, "Home", "Page visit 'Home page'", false)]
    [TestCase(TextMatchOperator.EndsWith, "visit 'Home", "Page visit 'Home'", false)]
    [TestCase(TextMatchOperator.EndsWith, "'Brien", "Member registered as 'O'Brien'", true)]
    [TestCase(TextMatchOperator.Contains, "ome", "Page visit 'Home'", true)]
    [TestCase(TextMatchOperator.Contains, "visit", "Page visit 'Home'", false)]
    [TestCase(TextMatchOperator.Contains, "'", "Member registered as 'O'Brien'", true)]
    [TestCase(TextMatchOperator.Contains, "'", "Page visit 'Home'", false)]
    [TestCase(TextMatchOperator.Contains, "100%", "Clicked link in email '100% off'", true)]
    [TestCase(TextMatchOperator.Contains, "100%", "Clicked link in email '1000 off'", false)]
    [TestCase(TextMatchOperator.DoesNotContain, "visit", "Page visit 'Home'", true)]
    [TestCase(TextMatchOperator.DoesNotContain, "ome", "Page visit 'Home'", false)]
    public void GetTitlePatterns_MatchQuotedNameOnly(TextMatchOperator textOperator, string text, string title, bool expected)
    {
        var (like, notLike) = ActivityDetailFilters.GetTitlePatterns(textOperator, text);

        bool match = SqlLike(title, like) && (notLike is null || !SqlLike(title, notLike));
        bool negated = textOperator is TextMatchOperator.NotEqual or TextMatchOperator.DoesNotContain;

        Assert.That(negated ? !match : match, Is.EqualTo(expected), $"LIKE {like}, NOT LIKE {notLike}");
    }


    [TestCase(TextMatchOperator.Equal, "NOT LIKE", false)]
    [TestCase(TextMatchOperator.NotEqual, "NOT LIKE", true)]
    [TestCase(TextMatchOperator.Contains, "LIKE", false)]
    [TestCase(TextMatchOperator.DoesNotContain, "LIKE", true)]
    [TestCase(TextMatchOperator.StartsWith, "LIKE", false)]
    [TestCase(TextMatchOperator.EndsWith, "LIKE", false)]
    public void ToWhereCondition_TitleUsesParameterizedQuotePatterns(TextMatchOperator textOperator, string sqlOperator, bool negated)
    {
        var condition = ActivityDetailFilters.ToWhereCondition(new ActivityDetailCriterion("ActivityTitle", textOperator, "O'Brien_1"));
        var (like, notLike) = ActivityDetailFilters.GetTitlePatterns(textOperator, "O'Brien_1");

        string sql = condition.WhereCondition;

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Not.Contain("Brien"), "Marketer text must not be part of the SQL text.");
            Assert.That(sql, Does.Contain($"[ActivityTitle] {sqlOperator} @"));
            Assert.That(condition.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(notLike is null ? new object[] { like } : new object[] { like, notLike }));
            Assert.That(sql.StartsWith("NOT ", StringComparison.Ordinal), Is.EqualTo(negated));
            Assert.That(sql.Contains("[ActivityTitle] IS NULL", StringComparison.Ordinal), Is.EqualTo(negated));
        });
    }


    [Test]
    public void ToWhereCondition_NegationStaysGroupedWhenCombinedWithOtherConditions()
    {
        // Same composition as ActivityCounter: AND with the contact/type conditions must not leak the OR.
        var combined = new WhereCondition()
            .WhereEquals("ActivityContactID", 42)
            .Where(ActivityDetailFilters.ToWhereCondition(new ActivityDetailCriterion("ActivityURL", TextMatchOperator.NotEqual, "x")));

        Assert.That(combined.WhereCondition, Does.Match(@"^\[ActivityContactID\] = @\w+ AND \(\[ActivityURL\] <> @\w+ OR \[ActivityURL\] IS NULL\)$"));
    }


    [Test]
    public void ToWhereCondition_UnknownOperatorThrows() => Assert.That(
        () => ActivityDetailFilters.ToWhereCondition(new ActivityDetailCriterion("ActivityURL", (TextMatchOperator)42, "x")),
        Throws.TypeOf<ArgumentOutOfRangeException>());


    [Test]
    public async Task TextOperatorOptions_ListAllOperatorsByNameWithLocalizedTexts()
    {
        var provider = new TextMatchOperatorOptionsProvider(new ResourceLocalization());

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(Enum.GetNames<TextMatchOperator>()));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[]
            {
                "contains",
                "does not contain",
                "is equal to",
                "is not equal to",
                "starts with",
                "ends with"
            }));
        });
    }


    [TestCase("pagevisit", ActivityDetailFilter.Url, ExpectedResult = true)]
    [TestCase("PageVisit", ActivityDetailFilter.Url, ExpectedResult = true)]
    [TestCase("pagevisit", ActivityDetailFilter.Value, ExpectedResult = false)]
    [TestCase("bizformsubmit", ActivityDetailFilter.Title, ExpectedResult = true)]
    [TestCase("custom_calculator", ActivityDetailFilter.Value, ExpectedResult = true)]
    [TestCase("custom_calculator", ActivityDetailFilter.Url, ExpectedResult = false)]
    [TestCase("landingpage", ActivityDetailFilter.Page, ExpectedResult = true)]
    [TestCase("bizformsubmit", ActivityDetailFilter.Page, ExpectedResult = false)]
    [TestCase("pagevisit", ActivityDetailFilter.Title, ExpectedResult = true)]
    [TestCase("emailclick", ActivityDetailFilter.Title, ExpectedResult = true)]
    [TestCase("emailclick", ActivityDetailFilter.Value, ExpectedResult = false)]
    [TestCase("emailclick", ActivityDetailFilter.Url, ExpectedResult = true)]
    [TestCase("landingpage", ActivityDetailFilter.Language, ExpectedResult = true)]
    [TestCase("emailclick", ActivityDetailFilter.Language, ExpectedResult = false)]
    [TestCase("", ActivityDetailFilter.Value, ExpectedResult = false)]
    public bool VisibilityCondition_UsesMappingForDependencyValue(string activityType, ActivityDetailFilter filter)
    {
        var condition = CreateVisibilityCondition(filter);

        return condition.Evaluate(new StubFieldValueProvider(new() { [nameof(ActivityFrequencyProperties.ActivityType)] = activityType }));
    }


    [Test]
    public void VisibilityCondition_HiddenWhenDependencyMissing() => Assert.That(
        CreateVisibilityCondition(ActivityDetailFilter.Value).Evaluate(new StubFieldValueProvider([])),
        Is.False);


    [Test]
    public void VisibilityCondition_DependsOnActivityTypeSoItReevaluatesOnChange() => Assert.That(
        ((IVisibilityCondition)CreateVisibilityCondition(ActivityDetailFilter.Url)).DependsOnFields,
        Is.EqualTo(new[] { nameof(ActivityFrequencyProperties.ActivityType) }));


    [Test]
    public void VisibilityCondition_IsBoundToAttribute() => Assert.That(
        typeof(ActivityDetailFilterVisibilityCondition).GetCustomAttribute<VisibilityConditionAttributeAttribute>()!.AttributeType,
        Is.EqualTo(typeof(VisibleIfActivityDetailFilterAttribute)));


    [Test]
    public void VisibilityAttribute_PropertiesMatchConditionPropertiesByNameAndType()
    {
        // The platform copies attribute properties to condition properties by name.
        var conditionProperties = typeof(ActivityDetailFilterVisibilityConditionProperties);

        Assert.Multiple(() =>
        {
            foreach (string? name in new[] { "PropertyName", nameof(VisibleIfActivityDetailFilterAttribute.Filter) })
            {
                Assert.That(conditionProperties.GetProperty(name)?.PropertyType,
                    Is.EqualTo(typeof(VisibleIfActivityDetailFilterAttribute).GetProperty(name)!.PropertyType), name);
            }
        });
    }


    [TestCase(nameof(ActivityFrequencyProperties.UrlRules), ActivityDetailFilter.Url)]
    [TestCase(nameof(ActivityFrequencyProperties.TitleOperator), ActivityDetailFilter.Title)]
    [TestCase(nameof(ActivityFrequencyProperties.Title), ActivityDetailFilter.Title)]
    [TestCase(nameof(ActivityFrequencyProperties.ValueOperator), ActivityDetailFilter.Value)]
    [TestCase(nameof(ActivityFrequencyProperties.Value), ActivityDetailFilter.Value)]
    [TestCase(nameof(ActivityFrequencyProperties.Pages), ActivityDetailFilter.Page)]
    [TestCase(nameof(ActivityFrequencyProperties.Language), ActivityDetailFilter.Language)]
    public void Properties_FilterVisibilityDependsOnActivityType(string propertyName, ActivityDetailFilter expectedFilter)
    {
        var attribute = typeof(ActivityFrequencyProperties).GetProperty(propertyName)!.GetCustomAttribute<VisibleIfActivityDetailFilterAttribute>()!;

        Assert.Multiple(() =>
        {
            Assert.That(attribute.PropertyName, Is.EqualTo(nameof(ActivityFrequencyProperties.ActivityType)));
            Assert.That(attribute.Filter, Is.EqualTo(expectedFilter));
        });
    }


    private static ActivityDetailFilterVisibilityCondition CreateVisibilityCondition(ActivityDetailFilter filter)
    {
        var condition = new ActivityDetailFilterVisibilityCondition();
        ((IVisibilityCondition)condition).SetProperties(new ActivityDetailFilterVisibilityConditionProperties
        {
            PropertyName = nameof(ActivityFrequencyProperties.ActivityType),
            Filter = filter
        });

        return condition;
    }


    /// <summary>
    /// Evaluates a SQL Server LIKE pattern as produced by the filters (%, _, and single-character [x] escapes), case-insensitive
    /// like the default collation.
    /// </summary>
    private static bool SqlLike(string value, string pattern)
    {
        var regex = new StringBuilder("^");
        int i = 0;
        while (i < pattern.Length)
        {
            char c = pattern[i];
            if (c == '%')
            {
                regex.Append(".*");
            }
            else if (c == '_')
            {
                regex.Append('.');
            }
            else if (c == '[' && i + 2 < pattern.Length && pattern[i + 2] == ']')
            {
                regex.Append(Regex.Escape(pattern[i + 1].ToString()));
                i += 2;
            }
            else
            {
                regex.Append(Regex.Escape(c.ToString()));
            }

            i++;
        }

        return Regex.IsMatch(value, regex.Append('$').ToString(),
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }


    private sealed record UnknownCriterion : ActivityCriterion;


    private sealed class StubFieldValueProvider(Dictionary<string, object?> values) : IFormFieldValueProvider
    {
        public bool TryGet<TValue>(string fieldName, out TValue value)
        {
            if (values.TryGetValue(fieldName, out object? stored) && stored is TValue typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }


    private sealed class ResourceLocalization : ILocalizationService
    {
        private static readonly ResourceManager resources = new(typeof(AutomationExtensionsResources));


        public string GetString(string resourceKey, string? culture = null, bool useDefaultCulture = true) =>
            resources.GetString(resourceKey, CultureInfo.InvariantCulture) ?? resourceKey;


        public string LocalizeString(string inputText, string? culture = null, bool encode = false, bool useDefaultCulture = true) => inputText;


        public string LocalizeExpression(string expression, string? culture = null, bool encode = false,
            Func<string, string, bool, string>? getStringMethod = null, bool useDefaultCulture = true) => expression;
    }
}
