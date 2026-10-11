using System.Globalization;
using System.Resources;

using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class ActivityFrequencyConfigurationTests
{
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(365)]
    public void GetSince_SubtractsWholeDays(int days)
    {
        var now = new DateTime(2026, 10, 5, 14, 30, 15, DateTimeKind.Local);

        var since = ActivityWindow.GetSince(now, days);

        Assert.Multiple(() =>
        {
            Assert.That(now - since, Is.EqualTo(TimeSpan.FromDays(days)));
            Assert.That(since.TimeOfDay, Is.EqualTo(now.TimeOfDay));
        });
    }


    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void GetSince_KeepsTimeBasisOfNow(DateTimeKind kind)
    {
        // Activities are stored in server local time; the window must not convert the given local now to UTC.
        var now = new DateTime(2026, 3, 1, 0, 30, 0, kind);

        var since = ActivityWindow.GetSince(now, 1);

        Assert.Multiple(() =>
        {
            Assert.That(since, Is.EqualTo(new DateTime(2026, 2, 28, 0, 30, 0, kind)));
            Assert.That(since.Kind, Is.EqualTo(kind));
        });
    }


    [Test]
    public void GetConfigurationError_ValidPropertiesReturnNullAndOperator()
    {
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Operator = nameof(NumberComparisonOperator.LessThan), Count = 0, Days = 365 };

        string? error = ActivityFrequencyConfiguration.GetConfigurationError(properties, true, out var comparisonOperator);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(comparisonOperator, Is.EqualTo(NumberComparisonOperator.LessThan));
        });
    }


    [TestCase("", true, "GreaterThanOrEqual", 3, 7, "no activity type")]
    [TestCase(" ", true, "GreaterThanOrEqual", 3, 7, "no activity type")]
    [TestCase("missing", false, "GreaterThanOrEqual", 3, 7, "activity type 'missing' doesn't exist")]
    [TestCase("pagevisit", true, "Between", 3, 7, "operator 'Between' is unknown")]
    [TestCase("pagevisit", true, "greaterthan", 3, 7, "operator 'greaterthan' is unknown")]
    [TestCase("pagevisit", true, "3", 3, 7, "operator '3' is unknown")]
    [TestCase("pagevisit", true, "", 3, 7, "operator '' is unknown")]
    [TestCase("pagevisit", true, "GreaterThanOrEqual", -1, 7, "number of activities -1 is negative")]
    [TestCase("pagevisit", true, "GreaterThanOrEqual", int.MinValue, 7, "is negative")]
    [TestCase("pagevisit", true, "GreaterThanOrEqual", 3, 0, "number of days 0 is outside 1–365")]
    [TestCase("pagevisit", true, "GreaterThanOrEqual", 3, -7, "number of days -7 is outside")]
    [TestCase("pagevisit", true, "GreaterThanOrEqual", 3, 366, "number of days 366 is outside 1–365")]
    public void GetConfigurationError_ReturnsReason(string activityType, bool exists, string comparisonOperator, int count, int days, string expectedReason)
    {
        var properties = new ActivityFrequencyProperties { ActivityType = activityType, Operator = comparisonOperator, Count = count, Days = days };

        string? error = ActivityFrequencyConfiguration.GetConfigurationError(properties, exists, out _);

        Assert.That(error, Does.Contain(expectedReason));
    }


    [TestCase(1)]
    [TestCase(365)]
    public void GetConfigurationError_DayBoundariesAreValid(int days)
    {
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Days = days };

        Assert.That(ActivityFrequencyConfiguration.GetConfigurationError(properties, true, out _), Is.Null);
    }


    [Test]
    public void Properties_Defaults()
    {
        var properties = new ActivityFrequencyProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.ActivityType, Is.Empty);
            Assert.That(properties.Operator, Is.EqualTo(nameof(NumberComparisonOperator.GreaterThanOrEqual)));
            Assert.That(properties.Count, Is.EqualTo(3));
            Assert.That(properties.Days, Is.EqualTo(7));
            Assert.That(properties.UrlRules, Is.Empty);
            Assert.That(properties.Title, Is.Empty);
            Assert.That(properties.Value, Is.Empty);
            Assert.That(new[] { properties.TitleOperator, properties.ValueOperator }, Is.All.EqualTo(nameof(TextMatchOperator.Contains)));
            Assert.That(properties.SkipStep, Is.False);
        });
    }


    [Test]
    public async Task GetEnabledTypes_ReturnsEnabledTypesWithLocalizedTextSortedByText()
    {
        var resolver = CreateResolver(text => text.Replace("{$activitytype.formsubmit$}", "Form submission"));

        var types = await resolver.GetEnabledTypes(CancellationToken.None);

        Assert.That(types, Is.EqualTo(new[]
        {
            new ActivityTypeDefinition("custom_calculator", "custom_calculator", true),
            new ActivityTypeDefinition("bizformsubmit", "Form submission", true),
            new ActivityTypeDefinition("pagevisit", "Page visit", true)
        }));
    }


    [TestCase("pagevisit", ExpectedResult = "pagevisit")]
    [TestCase("PageVisit", ExpectedResult = "pagevisit")]
    [TestCase("emailclick", ExpectedResult = "emailclick")]
    [TestCase("missing", ExpectedResult = null)]
    [TestCase("", ExpectedResult = null)]
    [TestCase(" ", ExpectedResult = null)]
    [TestCase(null, ExpectedResult = null)]
    public async Task<string?> ResolveTypeName_AcceptsExistingTypesIncludingDisabled(string? activityType) =>
        await CreateResolver(text => text).ResolveTypeName(activityType, CancellationToken.None);


    [Test]
    public async Task ResolveTypeName_EmptyValueDoesNotQueryTypes()
    {
        var resolver = new ActivityTypeResolver(
            new ContactScoreFieldResolverTests.TestLocalization(text => text),
            _ => throw new InvalidOperationException("Types must not be read."),
            (_, _) => throw new InvalidOperationException("Types must not be read."));

        Assert.That(await resolver.ResolveTypeName(" ", CancellationToken.None), Is.Null);
    }


    [Test]
    public async Task ActivityTypeOptions_ValuesAreCodeNamesAndTextsAreLocalized()
    {
        var provider = new ActivityTypeOptionsProvider(CreateResolver(text => text.Replace("{$activitytype.formsubmit$}", "Form submission")));

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(new[] { "custom_calculator", "bizformsubmit", "pagevisit" }));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[] { "custom_calculator", "Form submission", "Page visit" }));
        });
    }


    [Test]
    public async Task GetLanguages_LocalizesAndSortsByDisplayName()
    {
        var languages = await CreateLanguageResolver(text => text.Replace("{$language.spanish$}", "Spanish")).GetLanguages(CancellationToken.None);

        Assert.That(languages, Is.EqualTo(new[]
        {
            new ActivityLanguageDefinition("cs", "cs", 3),
            new ActivityLanguageDefinition("en", "English", 1),
            new ActivityLanguageDefinition("es", "Spanish", 2)
        }));
    }


    [TestCase("es", ExpectedResult = 2)]
    [TestCase("ES", ExpectedResult = 2)]
    [TestCase("deleted", ExpectedResult = null)]
    [TestCase("", ExpectedResult = null)]
    [TestCase(" ", ExpectedResult = null)]
    [TestCase(null, ExpectedResult = null)]
    public async Task<int?> ResolveLanguageId_ReturnsIdOfExistingCodeName(string? languageName) =>
        await CreateLanguageResolver(text => text).ResolveLanguageId(languageName, CancellationToken.None);


    [Test]
    public async Task ResolveLanguageId_EmptyValueDoesNotQueryLanguages()
    {
        var resolver = new ActivityLanguageResolver(
            new ContactScoreFieldResolverTests.TestLocalization(text => text),
            _ => throw new InvalidOperationException("Languages must not be read."),
            (_, _) => throw new InvalidOperationException("Languages must not be read."));

        Assert.That(await resolver.ResolveLanguageId("", CancellationToken.None), Is.Null);
    }


    [Test]
    public async Task LanguageOptions_StartWithAnyThenCodeNamesWithLocalizedTexts()
    {
        var provider = new ActivityLanguageOptionsProvider(
            CreateLanguageResolver(text => text.Replace("{$language.spanish$}", "Spanish")),
            new ContactScoreFieldResolverTests.TestLocalization(text => text));

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(new[] { "", "cs", "en", "es" }));
            // Test localization returns resource keys as-is.
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[] { ActivityLanguageOptionsProvider.ANY_RESOURCE_KEY, "cs", "English", "Spanish" }));
            Assert.That(
                new ResourceManager(typeof(AutomationExtensionsResources)).GetString(ActivityLanguageOptionsProvider.ANY_RESOURCE_KEY, CultureInfo.InvariantCulture),
                Is.EqualTo("Any"));
        });
    }


    /// <summary>
    /// English (plain display name), Spanish (macro display name), and Czech without display name.
    /// </summary>
    internal static ActivityLanguageResolver CreateLanguageResolver(Func<string, string> localize)
    {
        ActivityLanguageDefinition[] languages =
        [
            new("es", "{$language.spanish$}", 2),
            new("en", "English", 1),
            new("cs", "", 3)
        ];

        return new ActivityLanguageResolver(
            new ContactScoreFieldResolverTests.TestLocalization(localize),
            _ => Task.FromResult<IEnumerable<ActivityLanguageDefinition>>(languages),
            (name, _) => Task.FromResult(languages.FirstOrDefault(language => string.Equals(language.Name, name, StringComparison.OrdinalIgnoreCase))));
    }


    /// <summary>
    /// Enabled page visit (plain display name), form submission (macro display name), custom type without display name,
    /// and disabled email click.
    /// </summary>
    internal static ActivityTypeResolver CreateResolver(Func<string, string> localize)
    {
        ActivityTypeDefinition[] types =
        [
            new("pagevisit", "Page visit", true),
            new("emailclick", "Email link click", false),
            new("bizformsubmit", "{$activitytype.formsubmit$}", true),
            new("custom_calculator", "", true)
        ];

        return new ActivityTypeResolver(
            new ContactScoreFieldResolverTests.TestLocalization(localize),
            _ => Task.FromResult<IEnumerable<ActivityTypeDefinition>>(types),
            (name, _) => Task.FromResult(types.FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase))));
    }
}
