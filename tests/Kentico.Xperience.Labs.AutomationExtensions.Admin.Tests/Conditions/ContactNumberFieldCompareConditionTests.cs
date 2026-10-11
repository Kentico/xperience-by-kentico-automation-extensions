using System.Globalization;
using System.Reflection;
using System.Resources;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

/// <summary>
/// Reading the field of the processed contact needs the Xperience container (contact class structure), so these tests
/// cover the gating and validation paths that return before <c>GetProcessedObject</c>.
/// </summary>
public class ContactNumberFieldCompareConditionTests
{
    [TestCase(false, "no")]
    [TestCase(true, "no")]
    [TestCase(true, "yes")]
    public async Task Evaluate_DisabledComponentReturnsNoBeforeSkipOrValidation(bool skipStep, string skippedBranch)
    {
        var condition = CreateCondition(enabled: false, ThrowingResolver());
        var properties = new ContactNumberFieldCompareProperties
        {
            SkipStep = skipStep,
            SkippedBranch = skippedBranch,
            Field = "AcmeContactScore",
            Operator = "Unknown"
        };

        bool result = await condition.Evaluate(properties, CreateContext(), CancellationToken.None);

        Assert.That(result, Is.False);
    }


    [TestCase("yes", true)]
    [TestCase("no", false)]
    public async Task Evaluate_SkippedConditionReturnsSelectedBranchBeforeValidation(string branch, bool expectedResult)
    {
        var condition = CreateCondition(enabled: true, ThrowingResolver());
        var properties = new ContactNumberFieldCompareProperties
        {
            SkipStep = true,
            SkippedBranch = branch,
            Field = "AcmeContactScore",
            Operator = "Unknown"
        };

        bool result = await condition.Evaluate(properties, CreateContext(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(expectedResult));
    }


    [TestCase("", nameof(NumberComparisonOperator.GreaterThanOrEqual))]
    [TestCase("ContactOwnerUserID", nameof(NumberComparisonOperator.GreaterThanOrEqual))]
    [TestCase("AcmeContactRevenue", nameof(NumberComparisonOperator.GreaterThanOrEqual))]
    [TestCase("Missing", nameof(NumberComparisonOperator.GreaterThanOrEqual))]
    [TestCase("AcmeContactScore", "Between")]
    [TestCase("AcmeContactScore", "3")]
    [TestCase("AcmeContactScore", "greaterthan")]
    public async Task Evaluate_InvalidConfigurationReturnsNoWithoutReadingContact(string field, string comparisonOperator)
    {
        var condition = CreateCondition(enabled: true, ContactScoreFieldResolverTests.CreateResolver());
        var properties = new ContactNumberFieldCompareProperties { Field = field, Operator = comparisonOperator };
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        // A cancelled token throws right before the contact is read, so returning normally proves no read happened.
        bool result = await condition.Evaluate(properties, CreateContext(), cancellationSource.Token);

        Assert.That(result, Is.False);
    }


    [Test]
    public void Evaluate_PropagatesCancellationBeforeReadingProcessedContact()
    {
        var condition = CreateCondition(enabled: true, ContactScoreFieldResolverTests.CreateResolver());
        var properties = new ContactNumberFieldCompareProperties { Field = "AcmeContactScore" };
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.That(
            async () => await condition.Evaluate(properties, CreateContext(), cancellationSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }


    [Test]
    public async Task OperatorOptions_ListAllOperatorsByNameWithLocalizedTexts()
    {
        var provider = new NumberComparisonOperatorOptionsProvider(new ResourceLocalization());

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(Enum.GetNames<NumberComparisonOperator>()));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[]
            {
                "is equal to",
                "is not equal to",
                "is greater than",
                "is greater than or equal to",
                "is less than",
                "is less than or equal to"
            }));
        });
    }


    [Test]
    public async Task FieldOptions_ReuseScoreFieldList()
    {
        var provider = new ContactScoreFieldOptionsProvider(ContactScoreFieldResolverTests.CreateResolver());

        var items = (await provider.GetOptionItems()).ToList();

        Assert.That(items.Select(item => item.Value), Is.EqualTo(new[] { "AcmeContactEngagement", "AcmeContactScore" }));
    }


    [Test]
    public void Properties_FieldUsesScoreFieldOptions()
    {
        var attribute = typeof(ContactNumberFieldCompareProperties)
            .GetProperty(nameof(ContactNumberFieldCompareProperties.Field))!
            .GetCustomAttribute<DropDownComponentAttribute>()!;

        Assert.That(attribute.DataProviderType, Is.EqualTo(typeof(ContactScoreFieldOptionsProvider)));
    }


    private static ContactNumberFieldCompareCondition CreateCondition(bool enabled, ContactScoreFieldResolver resolver) =>
        new(new StubComponentStatus(enabled), resolver, NullLoggerFactory.Instance);


    private static ContactScoreFieldResolver ThrowingResolver() => new(
        new ContactScoreFieldResolverTests.TestLocalization(text => text),
        () => throw new InvalidOperationException("Field definition must not be read."));


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
