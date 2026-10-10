using System.Globalization;
using System.Reflection;
using System.Resources;

using CMS.Core;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class ActivityDetailFiltersTests
{
    [TestCase("pagevisit", true, false, false)]
    [TestCase("landingpage", true, false, false)]
    [TestCase("bizformsubmit", true, true, false)]
    [TestCase("memberregistration", false, true, false)]
    [TestCase("emailclick", false, false, true)]
    [TestCase("click", false, false, true)]
    [TestCase("datainput", false, false, true)]
    [TestCase("custom_calculator", false, false, true)]
    [TestCase("PageVisit", true, false, false)]
    [TestCase("BIZFORMSUBMIT", true, true, false)]
    [TestCase("MemberRegistration", false, true, false)]
    [TestCase("EmailClick", false, false, true)]
    [TestCase("", false, false, false)]
    [TestCase(" ", false, false, false)]
    [TestCase(null, false, false, false)]
    public void IsAvailable_MapsActivityTypesToFilters(string? activityType, bool url, bool title, bool value) =>
        Assert.Multiple(() =>
        {
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Url), Is.EqualTo(url));
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Title), Is.EqualTo(title));
            Assert.That(ActivityDetailFilters.IsAvailable(activityType, ActivityDetailFilter.Value), Is.EqualTo(value));
        });


    [TestCase(ActivityDetailFilter.Url, ExpectedResult = "ActivityURL")]
    [TestCase(ActivityDetailFilter.Title, ExpectedResult = "ActivityTitle")]
    [TestCase(ActivityDetailFilter.Value, ExpectedResult = "ActivityValue")]
    public string GetColumn_NeverReferrer(ActivityDetailFilter filter) => ActivityDetailFilters.GetColumn(filter);


    [Test]
    public void GetColumn_UnknownFilterThrows() => Assert.That(
        () => ActivityDetailFilters.GetColumn((ActivityDetailFilter)42),
        Throws.TypeOf<ArgumentOutOfRangeException>());


    [TestCase("pagevisit", new[] { "ActivityURL StartsWith https://www.example.com/pricing" })]
    [TestCase("landingpage", new[] { "ActivityURL StartsWith https://www.example.com/pricing" })]
    [TestCase("bizformsubmit", new[] { "ActivityURL StartsWith https://www.example.com/pricing", "ActivityTitle Equal Contact form" })]
    [TestCase("memberregistration", new[] { "ActivityTitle Equal Contact form" })]
    [TestCase("emailclick", new[] { "ActivityValue DoesNotContain utm_campaign=fall" })]
    [TestCase("custom_calculator", new[] { "ActivityValue DoesNotContain utm_campaign=fall" })]
    public void GetCriteria_AppliesOnlyFiltersVisibleForTypeAndTrimsText(string activityType, string[] expected)
    {
        // All three filters are filled, as if left over from previously selected activity types.
        var properties = new ActivityFrequencyProperties
        {
            UrlOperator = nameof(TextMatchOperator.StartsWith),
            Url = " https://www.example.com/pricing ",
            TitleOperator = nameof(TextMatchOperator.Equal),
            Title = "Contact form",
            ValueOperator = nameof(TextMatchOperator.DoesNotContain),
            Value = "utm_campaign=fall "
        };

        string? error = ActivityDetailFilters.GetCriteria(activityType, properties, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria.Select(criterion => $"{criterion.Column} {criterion.Operator} {criterion.Text}"), Is.EqualTo(expected));
        });
    }


    [TestCase("", nameof(TextMatchOperator.Contains))]
    [TestCase("   ", nameof(TextMatchOperator.NotEqual))]
    [TestCase("", "Unknown")]
    public void GetCriteria_EmptyTextsMeanNoFilterWhateverOperator(string text, string textOperator)
    {
        var properties = new ActivityFrequencyProperties
        {
            UrlOperator = textOperator,
            Url = text,
            TitleOperator = textOperator,
            Title = text,
            ValueOperator = textOperator,
            Value = text
        };

        string? error = ActivityDetailFilters.GetCriteria("bizformsubmit", properties, out var criteria);

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
            UrlOperator = nameof(TextMatchOperator.Equal),
            Url = "https://www.example.com/"
        };

        string? error = ActivityDetailFilters.GetCriteria("pagevisit", properties, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(criteria, Is.EqualTo(new[] { new ActivityDetailCriterion("ActivityURL", TextMatchOperator.Equal, "https://www.example.com/") }));
        });
    }


    [TestCase("Unknown")]
    [TestCase("contains")]
    [TestCase("1")]
    [TestCase("")]
    public void GetCriteria_UnknownOperatorOfAppliedFilterIsError(string textOperator)
    {
        var properties = new ActivityFrequencyProperties { TitleOperator = textOperator, Title = "Contact form" };

        string? error = ActivityDetailFilters.GetCriteria("bizformsubmit", properties, out var criteria);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.EqualTo($"title operator '{textOperator}' is unknown"));
            Assert.That(criteria, Is.Empty);
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


    [TestCase(nameof(ActivityFrequencyProperties.UrlOperator), ActivityDetailFilter.Url)]
    [TestCase(nameof(ActivityFrequencyProperties.Url), ActivityDetailFilter.Url)]
    [TestCase(nameof(ActivityFrequencyProperties.TitleOperator), ActivityDetailFilter.Title)]
    [TestCase(nameof(ActivityFrequencyProperties.Title), ActivityDetailFilter.Title)]
    [TestCase(nameof(ActivityFrequencyProperties.ValueOperator), ActivityDetailFilter.Value)]
    [TestCase(nameof(ActivityFrequencyProperties.Value), ActivityDetailFilter.Value)]
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
