using System.Reflection;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.Websites;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.FormComponents;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;
using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

/// <summary>
/// The activity count is faked (<see cref="IActivityCounter"/>), so gating, validation, window, and comparison paths run
/// without the Xperience container. The database query itself is covered by the running-app check.
/// </summary>
public class ActivityFrequencyConditionTests
{
    private static readonly DateTimeOffset now = new(2026, 10, 5, 12, 30, 0, TimeSpan.Zero);


    [TestCase(false, "no")]
    [TestCase(true, "no")]
    [TestCase(true, "yes")]
    public async Task Evaluate_DisabledComponentReturnsNoBeforeSkipOrValidation(bool skipStep, string skippedBranch)
    {
        var counter = new FakeActivityCounter(10);
        var condition = CreateCondition(enabled: false, ThrowingResolver(), counter);
        var properties = new ActivityFrequencyProperties
        {
            SkipStep = skipStep,
            SkippedBranch = skippedBranch,
            ActivityType = "pagevisit",
            Operator = "Unknown"
        };

        bool result = await condition.Evaluate(properties, CreateContext(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(counter.Calls, Is.Empty);
        });
    }


    [TestCase("yes", true)]
    [TestCase("no", false)]
    public async Task Evaluate_SkippedConditionReturnsSelectedBranchBeforeValidation(string branch, bool expectedResult)
    {
        var counter = new FakeActivityCounter(0);
        var condition = CreateCondition(enabled: true, ThrowingResolver(), counter);
        var properties = new ActivityFrequencyProperties
        {
            SkipStep = true,
            SkippedBranch = branch,
            ActivityType = "pagevisit",
            Operator = "Unknown"
        };

        bool result = await condition.Evaluate(properties, CreateContext(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(counter.Calls, Is.Empty);
        });
    }


    [TestCase("", nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, 7)]
    [TestCase("missing", nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, 7)]
    [TestCase("pagevisit", "Between", 3, 7)]
    [TestCase("pagevisit", "greaterthan", 3, 7)]
    [TestCase("pagevisit", nameof(NumberComparisonOperator.GreaterThanOrEqual), -1, 7)]
    [TestCase("pagevisit", nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, 0)]
    [TestCase("pagevisit", nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, 366)]
    public async Task Evaluate_InvalidConfigurationReturnsNoWithoutReadingContactOrCounting(string activityType, string comparisonOperator, int count, int days)
    {
        var properties = new ActivityFrequencyProperties { ActivityType = activityType, Operator = comparisonOperator, Count = count, Days = days };

        await AssertInvalidConfiguration(properties);
    }


    [TestCase("pagevisit", "Unknown", "Pricing")]
    [TestCase("pagevisit", "contains", "Pricing")]
    [TestCase("pagevisit", "", "Pricing")]
    [TestCase("bizformsubmit", "2", "Contact")]
    public async Task Evaluate_UnknownTextOperatorOfAppliedFilterReturnsNo(string activityType, string titleOperator, string title)
    {
        var properties = new ActivityFrequencyProperties { ActivityType = activityType, TitleOperator = titleOperator, Title = title };

        await AssertInvalidConfiguration(properties);
    }


    [Test]
    public async Task Evaluate_UnknownLanguageOfAppliedFilterReturnsNo()
    {
        // Same as an unknown activity type: a language deleted after configuration is a broken step, not "0 activities".
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Language = "deleted" };

        await AssertInvalidConfiguration(properties);
    }


    [Test]
    public void Evaluate_HiddenLanguageIsNotResolved()
    {
        var counter = new FakeActivityCounter(5);
        var languageResolver = new ActivityLanguageResolver(
            new ContactScoreFieldResolverTests.TestLocalization(text => text),
            _ => throw new InvalidOperationException("Languages must not be read."),
            (_, _) => throw new InvalidOperationException("Languages must not be read."));
        var condition = CreateCondition(enabled: true, ActivityFrequencyConfigurationTests.CreateResolver(text => text), counter, languageResolver);
        var properties = new ActivityFrequencyProperties { ActivityType = "bizformsubmit", Language = "deleted" };
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Reaching the cancellation check before the contact read proves the configuration is valid.
        Assert.That(
            async () => await condition.Evaluate(properties, CreateContext(), cancellationSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }


    [TestCase("pagevisit")]
    [TestCase("PageVisit")]
    [TestCase("emailclick")]
    // emailclick is disabled in the test types: disabled types stay valid (contacts may have older activities).
    public void Evaluate_ValidConfigurationPropagatesCancellationBeforeReadingProcessedContact(string activityType)
    {
        var counter = new FakeActivityCounter(5);
        var condition = CreateCondition(enabled: true, ActivityFrequencyConfigurationTests.CreateResolver(text => text), counter);
        // Unknown operators of empty filters don't make the configuration invalid (hidden filters: ActivityDetailFiltersTests).
        var properties = new ActivityFrequencyProperties { ActivityType = activityType, TitleOperator = "Unknown", ValueOperator = "Unknown" };
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(
                async () => await condition.Evaluate(properties, CreateContext(), cancellationSource.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(counter.Calls, Is.Empty);
        });
    }


    [TestCase(2, nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, ExpectedResult = false)]
    [TestCase(3, nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, ExpectedResult = true)]
    [TestCase(4, nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, ExpectedResult = true)]
    [TestCase(1, nameof(NumberComparisonOperator.LessThan), 2, ExpectedResult = true)]
    [TestCase(2, nameof(NumberComparisonOperator.LessThan), 2, ExpectedResult = false)]
    [TestCase(1, nameof(NumberComparisonOperator.Equal), 1, ExpectedResult = true)]
    [TestCase(0, nameof(NumberComparisonOperator.Equal), 0, ExpectedResult = true)]
    [TestCase(0, nameof(NumberComparisonOperator.GreaterThan), 0, ExpectedResult = false)]
    [TestCase(5, nameof(NumberComparisonOperator.NotEqual), 5, ExpectedResult = false)]
    [TestCase(5, nameof(NumberComparisonOperator.LessThanOrEqual), 5, ExpectedResult = true)]
    public async Task<bool> CountAndCompare_ComparesCountWithOperator(int activityCount, string comparisonOperator, int count)
    {
        var condition = CreateCondition(enabled: true, ThrowingResolver(), new FakeActivityCounter(activityCount));
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Operator = comparisonOperator, Count = count };

        return await condition.CountAndCompare(42, "pagevisit", [], Enum.Parse<NumberComparisonOperator>(comparisonOperator), properties, "Process", CancellationToken.None);
    }


    [Test]
    public async Task CountAndCompare_CountsContactTypeAndCriteriaInLocalTimeWindow()
    {
        var counter = new FakeActivityCounter(3);
        var condition = CreateCondition(enabled: true, ThrowingResolver(), counter);
        var properties = new ActivityFrequencyProperties { ActivityType = "PageVisit", Days = 30 };
        ActivityCriterion[] criteria =
        [
            new ActivityPageCriterion([Guid.Parse("8b4a3c7e-1d2f-4e5a-9b6c-0d1e2f3a4b5c")]),
            new ActivityDetailCriterion("ActivityURL", TextMatchOperator.StartsWith, "https://www.example.com/pricing")
        ];
        using var cancellationSource = new CancellationTokenSource();

        bool result = await condition.CountAndCompare(42, "pagevisit", criteria, NumberComparisonOperator.GreaterThanOrEqual, properties, "Process", cancellationSource.Token);

        var call = counter.Calls.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(call.ContactId, Is.EqualTo(42));
            Assert.That(call.ActivityType, Is.EqualTo("pagevisit"));
            // Local now (UTC+2 test zone) 2026-10-05 14:30 minus 30 days – not the UTC time 12:30.
            Assert.That(call.Since, Is.EqualTo(new DateTime(2026, 9, 5, 14, 30, 0, DateTimeKind.Unspecified)));
            Assert.That(call.Criteria, Is.EqualTo(criteria));
            Assert.That(call.CancellationToken, Is.EqualTo(cancellationSource.Token));
        });
    }


    [Test]
    public void CountAndCompare_CounterCancellationPropagates()
    {
        var condition = CreateCondition(enabled: true, ThrowingResolver(), new FakeActivityCounter(() => throw new OperationCanceledException()));

        Assert.That(
            async () => await condition.CountAndCompare(1, "pagevisit", [], NumberComparisonOperator.GreaterThanOrEqual, new ActivityFrequencyProperties(), "Process", CancellationToken.None),
            Throws.InstanceOf<OperationCanceledException>());
    }


    [TestCase(nameof(NumberComparisonOperator.GreaterThanOrEqual), 3, ExpectedResult = true)]
    [TestCase(nameof(NumberComparisonOperator.GreaterThanOrEqual), 4, ExpectedResult = false)]
    [TestCase(nameof(NumberComparisonOperator.Equal), 3, ExpectedResult = true)]
    public async Task<bool> CountAndCompare_UrlRulesSumCountsOfMatchingDistinctUrls(string comparisonOperator, int count)
    {
        var counter = new FakeActivityCounter(() => throw new InvalidOperationException("URL rules must use grouped counts."))
        {
            Groups =
            [
                new("https://www.example.com/pricing", 1),
                new("https://www.example.com/pricing/?utm_source=news#plans", 2),
                new("https://www.example.com/pricing-faq", 5),
                new(null, 7)
            ]
        };
        var condition = CreateCondition(enabled: true, ThrowingResolver(), counter);
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Operator = comparisonOperator, Count = count };
        var urlCriterion = new ActivityUrlMatchCriterion("ActivityURL", new UrlMatchRuleSet([new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Equal, "/pricing")]));

        bool result = await condition.CountAndCompare(42, "pagevisit", [urlCriterion], Enum.Parse<NumberComparisonOperator>(comparisonOperator), properties, "Process", CancellationToken.None);

        var (Column, MaxValues, Criteria) = counter.GroupCalls.Single();
        Assert.Multiple(() =>
        {
            Assert.That(Column, Is.EqualTo("ActivityURL"));
            Assert.That(MaxValues, Is.EqualTo(ActivityFrequencyCondition.MAX_DISTINCT_URLS + 1));
            Assert.That(Criteria, Is.EqualTo(new[] { urlCriterion }), "The criterion is passed on for its database prefilter.");
            Assert.That(counter.Calls, Is.Empty);
        });

        return result;
    }


    [Test]
    public async Task CountAndCompare_NegatedUrlRuleCountsActivitiesWithoutUrl()
    {
        var counter = new FakeActivityCounter(0)
        {
            Groups = [new("https://www.example.com/blog/post", 4), new("https://www.example.com/pricing", 1), new(null, 2)]
        };
        var condition = CreateCondition(enabled: true, ThrowingResolver(), counter);
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Operator = nameof(NumberComparisonOperator.Equal), Count = 3 };
        var urlCriterion = new ActivityUrlMatchCriterion("ActivityURL", new UrlMatchRuleSet([new UrlMatchRule(UrlPart.Path, UrlMatchOperator.DoesNotContain, "/blog")]));

        bool result = await condition.CountAndCompare(42, "pagevisit", [urlCriterion], NumberComparisonOperator.Equal, properties, "Process", CancellationToken.None);

        Assert.That(result, Is.True, "1 pricing + 2 without URL = 3");
    }


    [TestCase(nameof(NumberComparisonOperator.LessThan))]
    [TestCase(nameof(NumberComparisonOperator.GreaterThanOrEqual))]
    public async Task CountAndCompare_MoreDistinctUrlsThanLimitReturnsNo(string comparisonOperator)
    {
        // "Fewer than N" would be true for any count of 0 – the limit must fail closed instead of counting.
        var counter = new FakeActivityCounter(0)
        {
            Groups = Enumerable.Range(0, ActivityFrequencyCondition.MAX_DISTINCT_URLS + 1)
                .Select(index => new ActivityValueCount($"https://www.example.com/page-{index}", 1))
                .ToList()
        };
        var condition = CreateCondition(enabled: true, ThrowingResolver(), counter);
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Operator = comparisonOperator, Count = 1 };
        var urlCriterion = new ActivityUrlMatchCriterion("ActivityURL", new UrlMatchRuleSet([new UrlMatchRule(UrlPart.Path, UrlMatchOperator.StartsWith, "/page")]));

        bool result = await condition.CountAndCompare(42, "pagevisit", [urlCriterion], Enum.Parse<NumberComparisonOperator>(comparisonOperator), properties, "Process", CancellationToken.None);

        Assert.That(result, Is.False);
    }


    [Test]
    public async Task CountAndCompare_ExactlyLimitDistinctUrlsAreEvaluated()
    {
        var counter = new FakeActivityCounter(0)
        {
            Groups = Enumerable.Range(0, ActivityFrequencyCondition.MAX_DISTINCT_URLS)
                .Select(index => new ActivityValueCount($"https://www.example.com/page-{index}", 1))
                .ToList()
        };
        var condition = CreateCondition(enabled: true, ThrowingResolver(), counter);
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", Operator = nameof(NumberComparisonOperator.Equal), Count = ActivityFrequencyCondition.MAX_DISTINCT_URLS };
        var urlCriterion = new ActivityUrlMatchCriterion("ActivityURL", new UrlMatchRuleSet([new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Matches, "/page-*")]));

        bool result = await condition.CountAndCompare(42, "pagevisit", [urlCriterion], NumberComparisonOperator.Equal, properties, "Process", CancellationToken.None);

        Assert.That(result, Is.True);
    }


    [TestCase("not json")]
    [TestCase("""{"version":1,"rules":[{"part":"QueryParameter","operator":"Equal","value":"spring"}]}""")]
    public async Task Evaluate_InvalidUrlRulesOfAppliedFilterReturnsNo(string urlRules)
    {
        var properties = new ActivityFrequencyProperties { ActivityType = "pagevisit", UrlRules = urlRules };

        await AssertInvalidConfiguration(properties);
    }


    [Test]
    public void Properties_UrlRulesUseUrlMatchComponent()
    {
        var urlRules = typeof(ActivityFrequencyProperties).GetProperty(nameof(ActivityFrequencyProperties.UrlRules))!;

        Assert.Multiple(() =>
        {
            Assert.That(urlRules.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(urlRules.GetCustomAttribute<UrlMatchComponentAttribute>(), Is.Not.Null);
            Assert.That(urlRules.GetCustomAttribute<UrlMatchComponentAttribute>()!.ExplanationTextAsHtml, Is.True);
            Assert.That(new ActivityFrequencyProperties().UrlRules, Is.Empty);
        });
    }


    private static async Task AssertInvalidConfiguration(ActivityFrequencyProperties properties)
    {
        var counter = new FakeActivityCounter(100);
        var condition = CreateCondition(enabled: true, ActivityFrequencyConfigurationTests.CreateResolver(text => text), counter);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        // A cancelled token throws right before the contact is read, so returning normally proves no read happened.
        bool result = await condition.Evaluate(properties, CreateContext(), cancellationSource.Token);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(counter.Calls, Is.Empty);
        });
    }


    [Test]
    public void Properties_UseActivityTypeAndSharedOperatorOptions()
    {
        static DropDownComponentAttribute GetDropDown(string name) => typeof(ActivityFrequencyProperties)
            .GetProperty(name)!
            .GetCustomAttribute<DropDownComponentAttribute>()!;

        Assert.Multiple(() =>
        {
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.ActivityType)).DataProviderType, Is.EqualTo(typeof(ActivityTypeOptionsProvider)));
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.Language)).DataProviderType, Is.EqualTo(typeof(ActivityLanguageOptionsProvider)));
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.Operator)).DataProviderType, Is.EqualTo(typeof(NumberComparisonOperatorOptionsProvider)));
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.TitleOperator)).DataProviderType, Is.EqualTo(typeof(TextMatchOperatorOptionsProvider)));
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.ValueOperator)).DataProviderType, Is.EqualTo(typeof(TextMatchOperatorOptionsProvider)));
        });
    }


    [Test]
    public void Properties_PagesUseMultiPageSelectorLimitedToPagesWithUrl()
    {
        var pages = typeof(ActivityFrequencyProperties).GetProperty(nameof(ActivityFrequencyProperties.Pages))!;
        var selector = pages.GetCustomAttribute<WebPageSelectorComponentAttribute>()!;

        Assert.Multiple(() =>
        {
            Assert.That(pages.PropertyType, Is.EqualTo(typeof(IEnumerable<WebPageRelatedItem>)));
            Assert.That(selector.MaximumPages, Is.Zero, "0 = any number of pages");
            Assert.That(selector.ItemModifierType, Is.EqualTo(typeof(WebPagesWithUrlWebPagePanelItemModifier)));
            Assert.That(selector.ExplanationTextAsHtml, Is.True);
            Assert.That(new ActivityFrequencyProperties().Pages, Is.Empty);
        });
    }


    [Test]
    public void Properties_FiltersFollowActivityTypeThenComparisonInOrderSteps()
    {
        var orders = typeof(ActivityFrequencyProperties)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => (property.Name, property.GetCustomAttributes().OfType<FormComponentAttribute>().Single().Order))
            .OrderBy(item => item.Order)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(orders.Select(item => item.Name), Is.EqualTo(new[]
            {
                nameof(ActivityFrequencyProperties.ActivityType),
                nameof(ActivityFrequencyProperties.Pages),
                nameof(ActivityFrequencyProperties.Language),
                nameof(ActivityFrequencyProperties.UrlRules),
                nameof(ActivityFrequencyProperties.TitleOperator),
                nameof(ActivityFrequencyProperties.Title),
                nameof(ActivityFrequencyProperties.ValueOperator),
                nameof(ActivityFrequencyProperties.Value),
                nameof(ActivityFrequencyProperties.Operator),
                nameof(ActivityFrequencyProperties.Count),
                nameof(ActivityFrequencyProperties.Days)
            }));
            Assert.That(orders.Select(item => item.Order), Is.EqualTo(Enumerable.Range(1, 11).Select(step => step * 10)));
        });
    }


    [Test]
    public void Properties_DaysAndCountValidationMatchRuntimeLimits()
    {
        var days = typeof(ActivityFrequencyProperties).GetProperty(nameof(ActivityFrequencyProperties.Days))!;
        var count = typeof(ActivityFrequencyProperties).GetProperty(nameof(ActivityFrequencyProperties.Count))!;

        Assert.Multiple(() =>
        {
            Assert.That(days.GetCustomAttribute<MinimumIntegerValueValidationRuleAttribute>()!.MinValue, Is.EqualTo(ActivityFrequencyConfiguration.MIN_DAYS));
            Assert.That(days.GetCustomAttribute<MaximumIntegerValueValidationRuleAttribute>()!.MaxValue, Is.EqualTo(ActivityFrequencyConfiguration.MAX_DAYS));
            Assert.That(count.GetCustomAttribute<MinimumIntegerValueValidationRuleAttribute>()!.MinValue, Is.Zero);
        });
    }


    private static ActivityFrequencyCondition CreateCondition(bool enabled, ActivityTypeResolver resolver, IActivityCounter counter, ActivityLanguageResolver? languageResolver = null) =>
        new(new StubComponentStatus(enabled), resolver, languageResolver ?? ActivityFrequencyConfigurationTests.CreateLanguageResolver(text => text), counter, new FixedTimeProvider(now), NullLoggerFactory.Instance);


    private static ActivityTypeResolver ThrowingResolver() => new(
        new ContactScoreFieldResolverTests.TestLocalization(text => text),
        _ => throw new InvalidOperationException("Activity types must not be read."),
        (_, _) => throw new InvalidOperationException("Activity types must not be read."));


    private static AutomationProcessContext CreateContext()
    {
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        // The platform exposes no public test factory for its process context.
        object?[] arguments =
        [
            new ContactInfo(),
            new AutomationProcessMetadata(),
            (Func<string>)(() => string.Empty),
            (Action<string>)(_ => { }),
            string.Empty,
            null,
            NullLogger<AutomationProcessContext>.Instance
        ];

        return (AutomationProcessContext)constructor.Invoke(arguments);
    }


    private sealed class StubComponentStatus(bool enabled) : IAutomationComponentStatus
    {
        public bool IsEnabled(AutomationComponentDescriptor component) => enabled;
    }


    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(2), "Test", "Test");
    }


    private sealed class FakeActivityCounter(Func<int> getCount) : IActivityCounter
    {
        public FakeActivityCounter(int count)
            : this(() => count)
        {
        }


        public List<(int ContactId, string ActivityType, DateTime Since, IReadOnlyList<ActivityCriterion> Criteria, CancellationToken CancellationToken)> Calls { get; } = [];


        public IReadOnlyList<ActivityValueCount> Groups { get; init; } = [];


        public List<(string Column, int MaxValues, IReadOnlyList<ActivityCriterion> Criteria)> GroupCalls { get; } = [];


        public Task<int> Count(int contactId, string activityType, DateTime since, IReadOnlyList<ActivityCriterion> criteria, CancellationToken cancellationToken)
        {
            Calls.Add((contactId, activityType, since, criteria, cancellationToken));

            return Task.FromResult(getCount());
        }


        public Task<IReadOnlyList<ActivityValueCount>> CountByValue(
            int contactId,
            string activityType,
            DateTime since,
            IReadOnlyList<ActivityCriterion> criteria,
            string column,
            int maxValues,
            CancellationToken cancellationToken)
        {
            GroupCalls.Add((column, maxValues, criteria));

            return Task.FromResult<IReadOnlyList<ActivityValueCount>>(Groups.Take(maxValues).ToList());
        }
    }
}
