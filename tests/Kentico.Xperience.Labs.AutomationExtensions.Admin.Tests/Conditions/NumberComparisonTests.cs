using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class NumberComparisonTests
{
    [TestCase(NumberComparisonOperator.Equal, false, true, false)]
    [TestCase(NumberComparisonOperator.NotEqual, true, false, true)]
    [TestCase(NumberComparisonOperator.GreaterThan, false, false, true)]
    [TestCase(NumberComparisonOperator.GreaterThanOrEqual, false, true, true)]
    [TestCase(NumberComparisonOperator.LessThan, true, false, false)]
    [TestCase(NumberComparisonOperator.LessThanOrEqual, true, true, false)]
    public void Compare_LessEqualGreater(NumberComparisonOperator comparisonOperator, bool whenLess, bool whenEqual, bool whenGreater) =>
        Assert.Multiple(() =>
        {
            Assert.That(NumberComparison.Compare(49, comparisonOperator, 50), Is.EqualTo(whenLess));
            Assert.That(NumberComparison.Compare(50, comparisonOperator, 50), Is.EqualTo(whenEqual));
            Assert.That(NumberComparison.Compare(51, comparisonOperator, 50), Is.EqualTo(whenGreater));
        });


    [TestCase(int.MinValue, NumberComparisonOperator.LessThan, int.MaxValue, ExpectedResult = true)]
    [TestCase(int.MaxValue, NumberComparisonOperator.GreaterThan, int.MinValue, ExpectedResult = true)]
    [TestCase(int.MaxValue, NumberComparisonOperator.Equal, int.MaxValue, ExpectedResult = true)]
    [TestCase(int.MinValue, NumberComparisonOperator.Equal, int.MinValue, ExpectedResult = true)]
    [TestCase(int.MinValue, NumberComparisonOperator.GreaterThanOrEqual, int.MinValue, ExpectedResult = true)]
    [TestCase(int.MaxValue, NumberComparisonOperator.LessThanOrEqual, int.MaxValue, ExpectedResult = true)]
    [TestCase(int.MinValue, NumberComparisonOperator.GreaterThan, int.MaxValue, ExpectedResult = false)]
    [TestCase(int.MaxValue, NumberComparisonOperator.NotEqual, int.MinValue, ExpectedResult = true)]
    public bool Compare_IntBounds(int current, NumberComparisonOperator comparisonOperator, int value) =>
        NumberComparison.Compare(current, comparisonOperator, value);


    [TestCase(-5, NumberComparisonOperator.LessThan, 0, ExpectedResult = true)]
    [TestCase(-5, NumberComparisonOperator.GreaterThan, -10, ExpectedResult = true)]
    [TestCase(-10, NumberComparisonOperator.GreaterThanOrEqual, -5, ExpectedResult = false)]
    [TestCase(-5, NumberComparisonOperator.Equal, -5, ExpectedResult = true)]
    [TestCase(0, NumberComparisonOperator.GreaterThan, -1, ExpectedResult = true)]
    public bool Compare_NegativeValues(int current, NumberComparisonOperator comparisonOperator, int value) =>
        NumberComparison.Compare(current, comparisonOperator, value);


    [Test]
    public void Compare_UnknownOperatorThrows() => Assert.That(
        () => NumberComparison.Compare(0, (NumberComparisonOperator)42, 0),
        Throws.TypeOf<ArgumentOutOfRangeException>());


    [TestCase("Equal", true, NumberComparisonOperator.Equal)]
    [TestCase("NotEqual", true, NumberComparisonOperator.NotEqual)]
    [TestCase("GreaterThan", true, NumberComparisonOperator.GreaterThan)]
    [TestCase("GreaterThanOrEqual", true, NumberComparisonOperator.GreaterThanOrEqual)]
    [TestCase("LessThan", true, NumberComparisonOperator.LessThan)]
    [TestCase("LessThanOrEqual", true, NumberComparisonOperator.LessThanOrEqual)]
    [TestCase("greaterthan", false, default(NumberComparisonOperator))]
    [TestCase("GREATERTHAN", false, default(NumberComparisonOperator))]
    [TestCase("1", false, default(NumberComparisonOperator))]
    [TestCase("42", false, default(NumberComparisonOperator))]
    [TestCase("-1", false, default(NumberComparisonOperator))]
    [TestCase("", false, default(NumberComparisonOperator))]
    [TestCase(" ", false, default(NumberComparisonOperator))]
    [TestCase(null, false, default(NumberComparisonOperator))]
    [TestCase("Between", false, default(NumberComparisonOperator))]
    [TestCase("Equal,NotEqual", false, default(NumberComparisonOperator))]
    public void TryParseOperator_AcceptsExactNamesOnly(string? value, bool expectedResult, NumberComparisonOperator expectedOperator)
    {
        bool result = NumberComparison.TryParseOperator(value, out var comparisonOperator);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(comparisonOperator, Is.EqualTo(expectedOperator));
        });
    }


    [Test]
    public void GetConfigurationError_ValidPropertiesReturnNullAndOperator()
    {
        var properties = new ContactNumberFieldCompareProperties { Field = "AcmeContactScore", Operator = nameof(NumberComparisonOperator.LessThan), Value = -3 };

        string? error = NumberComparison.GetConfigurationError(properties, true, out var comparisonOperator);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(comparisonOperator, Is.EqualTo(NumberComparisonOperator.LessThan));
        });
    }


    [TestCase("", true, "GreaterThan", "no field")]
    [TestCase(" ", true, "GreaterThan", "no field")]
    [TestCase("AcmeContactScore", false, "GreaterThan", "isn't an integer custom contact field")]
    [TestCase("AcmeContactScore", true, "Between", "operator 'Between' is unknown")]
    [TestCase("AcmeContactScore", true, "greaterthan", "operator 'greaterthan' is unknown")]
    [TestCase("AcmeContactScore", true, "2", "operator '2' is unknown")]
    [TestCase("AcmeContactScore", true, "", "operator '' is unknown")]
    public void GetConfigurationError_ReturnsReason(string field, bool fieldIsEligible, string comparisonOperator, string expectedReason)
    {
        var properties = new ContactNumberFieldCompareProperties { Field = field, Operator = comparisonOperator };

        string? error = NumberComparison.GetConfigurationError(properties, fieldIsEligible, out _);

        Assert.That(error, Does.Contain(expectedReason));
    }


    [TestCase("ContactOwnerUserID")]
    [TestCase("AcmeContactRevenue")]
    [TestCase("Missing")]
    public void GetConfigurationError_IneligibleFieldFromResolver(string field)
    {
        var resolver = ContactScoreFieldResolverTests.CreateResolver();
        var properties = new ContactNumberFieldCompareProperties { Field = field };

        string? error = NumberComparison.GetConfigurationError(properties, resolver.ResolveFieldName(field) is not null, out _);

        Assert.That(error, Does.Contain("isn't an integer custom contact field"));
    }


    [Test]
    public void EmptyStoredValue_ComparesAsZero()
    {
        int current = ContactScoreCalculator.GetStoredValue(DBNull.Value) ?? 0;

        Assert.Multiple(() =>
        {
            Assert.That(ContactScoreCalculator.GetStoredValue(null) ?? 0, Is.Zero);
            Assert.That(NumberComparison.Compare(current, NumberComparisonOperator.Equal, 0), Is.True);
            Assert.That(NumberComparison.Compare(current, NumberComparisonOperator.GreaterThanOrEqual, 50), Is.False);
            Assert.That(NumberComparison.Compare(current, NumberComparisonOperator.GreaterThan, -1), Is.True);
        });
    }


    [Test]
    public void Properties_Defaults()
    {
        var properties = new ContactNumberFieldCompareProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.Field, Is.Empty);
            Assert.That(properties.Operator, Is.EqualTo(nameof(NumberComparisonOperator.GreaterThanOrEqual)));
            Assert.That(properties.Value, Is.EqualTo(50));
            Assert.That(properties.SkipStep, Is.False);
        });
    }


    [Test]
    public void Operator_PersistedNamesNeverChange() => Assert.That(
        Enum.GetNames<NumberComparisonOperator>(),
        Is.EqualTo(new[] { "Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual" }));
}
