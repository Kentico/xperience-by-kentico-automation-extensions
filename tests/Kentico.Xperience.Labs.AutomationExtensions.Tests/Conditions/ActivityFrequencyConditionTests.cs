using System.Reflection;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

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


    [TestCase("pagevisit", "Unknown", "/pricing")]
    [TestCase("pagevisit", "contains", "/pricing")]
    [TestCase("pagevisit", "", "/pricing")]
    [TestCase("bizformsubmit", "2", "/contact")]
    public async Task Evaluate_UnknownTextOperatorOfAppliedFilterReturnsNo(string activityType, string urlOperator, string url)
    {
        var properties = new ActivityFrequencyProperties { ActivityType = activityType, UrlOperator = urlOperator, Url = url };

        await AssertInvalidConfiguration(properties);
    }


    [TestCase("pagevisit")]
    [TestCase("PageVisit")]
    [TestCase("emailclick")]
    // emailclick is disabled in the test types: disabled types stay valid (contacts may have older activities).
    public void Evaluate_ValidConfigurationPropagatesCancellationBeforeReadingProcessedContact(string activityType)
    {
        var counter = new FakeActivityCounter(5);
        var condition = CreateCondition(enabled: true, ActivityFrequencyConfigurationTests.CreateResolver(text => text), counter);
        // Unknown operators of hidden or empty filters don't make the configuration invalid.
        var properties = new ActivityFrequencyProperties { ActivityType = activityType, UrlOperator = "Unknown", TitleOperator = "Unknown", Title = "stale", ValueOperator = "Unknown" };
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
        ActivityDetailCriterion[] criteria = [new("ActivityURL", TextMatchOperator.StartsWith, "https://www.example.com/pricing")];
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
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.Operator)).DataProviderType, Is.EqualTo(typeof(NumberComparisonOperatorOptionsProvider)));
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.UrlOperator)).DataProviderType, Is.EqualTo(typeof(TextMatchOperatorOptionsProvider)));
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.TitleOperator)).DataProviderType, Is.EqualTo(typeof(TextMatchOperatorOptionsProvider)));
            Assert.That(GetDropDown(nameof(ActivityFrequencyProperties.ValueOperator)).DataProviderType, Is.EqualTo(typeof(TextMatchOperatorOptionsProvider)));
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
                nameof(ActivityFrequencyProperties.UrlOperator),
                nameof(ActivityFrequencyProperties.Url),
                nameof(ActivityFrequencyProperties.TitleOperator),
                nameof(ActivityFrequencyProperties.Title),
                nameof(ActivityFrequencyProperties.ValueOperator),
                nameof(ActivityFrequencyProperties.Value),
                nameof(ActivityFrequencyProperties.Operator),
                nameof(ActivityFrequencyProperties.Count),
                nameof(ActivityFrequencyProperties.Days)
            }));
            Assert.That(orders.Select(item => item.Order), Is.EqualTo(Enumerable.Range(1, 10).Select(step => step * 10)));
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


    private static ActivityFrequencyCondition CreateCondition(bool enabled, ActivityTypeResolver resolver, IActivityCounter counter) =>
        new(new StubComponentStatus(enabled), resolver, counter, new FixedTimeProvider(now), NullLoggerFactory.Instance);


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


        public List<(int ContactId, string ActivityType, DateTime Since, IReadOnlyList<ActivityDetailCriterion> Criteria, CancellationToken CancellationToken)> Calls { get; } = [];


        public Task<int> Count(int contactId, string activityType, DateTime since, IReadOnlyList<ActivityDetailCriterion> criteria, CancellationToken cancellationToken)
        {
            Calls.Add((contactId, activityType, since, criteria, cancellationToken));

            return Task.FromResult(getCount());
        }
    }
}
