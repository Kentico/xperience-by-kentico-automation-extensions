using System.Reflection;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Labs.AutomationExtensions.Conditions.RandomSplit;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class RandomSplitConditionTests
{
    private const string VECTOR_GUID = "8f6c2b8e-6d33-4c4a-9c47-0e2b1d1f6a10";


    [TestCase(VECTOR_GUID, "default", 3)]
    [TestCase(VECTOR_GUID, "newsletter", 87)]
    [TestCase(VECTOR_GUID, "Newsletter", 31)]
    [TestCase(VECTOR_GUID, "key with spaces", 14)]
    [TestCase(VECTOR_GUID, "日本語-🧪", 51)]
    [TestCase("00000000-0000-0000-0000-000000000001", "default", 2)]
    public void GetBucket_UsesStableSha256Vectors(string guid, string key, int expectedBucket)
    {
        int bucket = RandomSplitBucket.GetBucket(Guid.Parse(guid), key);

        Assert.That(bucket, Is.EqualTo(expectedBucket));
    }


    [Test]
    public void GetBucket_IsStableForRepeatedContactAndKey()
    {
        var contactGuid = Guid.Parse(VECTOR_GUID);

        Assert.That(
            RandomSplitBucket.GetBucket(contactGuid, "default"),
            Is.EqualTo(RandomSplitBucket.GetBucket(contactGuid, "default")));
    }


    [Test]
    public void GetBucket_AlwaysReturnsValueInRange_AndPopulationIsBalanced()
    {
        int yesCount = 0;

        for (int index = 1; index <= 10_000; index++)
        {
            var contactGuid = new Guid(index, 0, 0, new byte[8]);
            int bucket = RandomSplitBucket.GetBucket(contactGuid, "population");

            Assert.That(bucket, Is.InRange(0, 99));
            if (bucket < 50)
            {
                yesCount++;
            }
        }

        Assert.That(yesCount, Is.InRange(4_500, 5_500));
    }


    [Test]
    public void IsInYesBranch_UsesInclusiveBoundariesAndExactThreshold()
    {
        var contactGuid = Guid.Parse(VECTOR_GUID);
        int bucket = 3;

        Assert.Multiple(() =>
        {
            Assert.That(RandomSplitBucket.IsInYesBranch(contactGuid, "default", 0), Is.False);
            Assert.That(RandomSplitBucket.IsInYesBranch(contactGuid, "default", 100), Is.True);
            Assert.That(RandomSplitBucket.IsInYesBranch(contactGuid, "default", bucket), Is.False);
            Assert.That(RandomSplitBucket.IsInYesBranch(contactGuid, "default", bucket + 1), Is.True);
        });
    }


    [Test]
    public void IsInYesBranch_IsMonotonicAsPercentageIncreases()
    {
        var contactGuid = Guid.Parse(VECTOR_GUID);
        bool wasYes = false;

        for (int percentage = 0; percentage <= 100; percentage++)
        {
            bool isYes = RandomSplitBucket.IsInYesBranch(contactGuid, "default", percentage);

            Assert.That(isYes || !wasYes, Is.True, $"Percentage {percentage} moved a Yes contact to No.");
            wasYes = isYes;
        }
    }


    [TestCase(-1)]
    [TestCase(101)]
    public void ValidateConfiguration_RejectsPercentagesOutsideRange(int percentage) => Assert.That(
            () => RandomSplitBucket.ValidateConfiguration(percentage, "default"),
            Throws.TypeOf<ArgumentOutOfRangeException>());


    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void ValidateConfiguration_RejectsNullOrBlankKeys(string? key) => Assert.That(
            () => RandomSplitBucket.ValidateConfiguration(50, key),
            Throws.InstanceOf<ArgumentException>());


    [Test]
    public void GetBucket_RejectsEmptyContactGuid() => Assert.That(
            () => RandomSplitBucket.GetBucket(Guid.Empty, "default"),
            Throws.TypeOf<ArgumentException>());


    [Test]
    public void Properties_DefaultToHalfAndDefaultKey()
    {
        var properties = new RandomSplitConditionProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.Percentage, Is.EqualTo(50));
            Assert.That(properties.SplitKey, Is.EqualTo("default"));
        });
    }


    [TestCase(false, "no")]
    [TestCase(true, "no")]
    [TestCase(true, "yes")]
    public async Task Evaluate_DisabledComponentReturnsYesBeforeSkipOrValidation(bool skipStep, string skippedBranch)
    {
        var condition = CreateCondition(enabled: false);
        var properties = new RandomSplitConditionProperties
        {
            SkipStep = skipStep,
            SkippedBranch = skippedBranch,
            Percentage = 101,
            SplitKey = ""
        };
        var context = CreateContext();

        bool result = await condition.Evaluate(properties, context, CancellationToken.None);

        Assert.That(result, Is.True);
    }


    [TestCase("yes", true)]
    [TestCase("no", false)]
    public async Task Evaluate_SkippedConditionReturnsSelectedBranch(string branch, bool expectedResult)
    {
        var condition = CreateCondition(enabled: true);
        var properties = new RandomSplitConditionProperties
        {
            SkipStep = true,
            SkippedBranch = branch,
            Percentage = 101,
            SplitKey = ""
        };
        var context = CreateContext();

        bool result = await condition.Evaluate(properties, context, CancellationToken.None);

        Assert.That(result, Is.EqualTo(expectedResult));
    }


    [TestCase(-1)]
    [TestCase(101)]
    public void Evaluate_ActivePathValidatesPercentageBeforeReadingContact(int percentage)
    {
        var condition = CreateCondition(enabled: true);
        var properties = new RandomSplitConditionProperties { Percentage = percentage };

        Assert.That(
            async () => await condition.Evaluate(properties, null!, CancellationToken.None),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }


    [Test]
    public void Evaluate_PropagatesCancellationBeforeReadingProcessedContact()
    {
        var condition = CreateCondition(enabled: true);
        var properties = new RandomSplitConditionProperties();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.That(
            async () => await condition.Evaluate(properties, null!, cancellationSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }


    private static RandomSplitCondition CreateCondition(bool enabled) =>
        new(new StubComponentStatus(enabled), NullLoggerFactory.Instance);


    private static AutomationProcessContext CreateContext()
    {
        var contact = new ContactInfo();
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        // The platform exposes no public test factory for its process context.
        object?[] arguments =
        [
            contact,
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
}
