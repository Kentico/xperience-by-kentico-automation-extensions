using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ContactScoreCalculatorTests
{
    [TestCase(5, ContactScoreOperation.Add, 10, ExpectedResult = 15)]
    [TestCase(5, ContactScoreOperation.Subtract, 10, ExpectedResult = -5)]
    [TestCase(5, ContactScoreOperation.Set, 10, ExpectedResult = 10)]
    [TestCase(5, ContactScoreOperation.Add, 0, ExpectedResult = 5)]
    [TestCase(-20, ContactScoreOperation.Set, 0, ExpectedResult = 0)]
    public int Calculate_AppliesOperation(int current, ContactScoreOperation operation, int amount) =>
        ContactScoreCalculator.Calculate(current, operation, amount, false, 0, 0);


    [TestCase(int.MaxValue, ContactScoreOperation.Add, 1, ExpectedResult = int.MaxValue)]
    [TestCase(int.MaxValue - 5, ContactScoreOperation.Add, int.MaxValue, ExpectedResult = int.MaxValue)]
    [TestCase(int.MinValue, ContactScoreOperation.Subtract, 1, ExpectedResult = int.MinValue)]
    [TestCase(int.MinValue + 5, ContactScoreOperation.Subtract, int.MaxValue, ExpectedResult = int.MinValue)]
    [TestCase(-1, ContactScoreOperation.Add, int.MaxValue, ExpectedResult = int.MaxValue - 1)]
    public int Calculate_SaturatesAtIntBounds(int current, ContactScoreOperation operation, int amount) =>
        ContactScoreCalculator.Calculate(current, operation, amount, false, 0, 0);


    [TestCase(90, ContactScoreOperation.Add, 20, ExpectedResult = 100)]
    [TestCase(5, ContactScoreOperation.Subtract, 20, ExpectedResult = 0)]
    [TestCase(40, ContactScoreOperation.Add, 20, ExpectedResult = 60)]
    [TestCase(40, ContactScoreOperation.Set, 500, ExpectedResult = 100)]
    [TestCase(40, ContactScoreOperation.Set, 0, ExpectedResult = 0)]
    [TestCase(-50, ContactScoreOperation.Add, 10, ExpectedResult = 0)]
    [TestCase(int.MaxValue, ContactScoreOperation.Add, 1, ExpectedResult = 100)]
    public int Calculate_ClampsToRange(int current, ContactScoreOperation operation, int amount) =>
        ContactScoreCalculator.Calculate(current, operation, amount, true, 0, 100);


    [TestCase(0, ExpectedResult = 50)]
    [TestCase(80, ExpectedResult = 50)]
    public int Calculate_EqualMinimumAndMaximumAlwaysReturnsThatValue(int current) =>
        ContactScoreCalculator.Calculate(current, ContactScoreOperation.Add, 10, true, 50, 50);


    [Test]
    public void Calculate_LimitDisabledIgnoresMinimumAndMaximum() => Assert.That(
        ContactScoreCalculator.Calculate(90, ContactScoreOperation.Add, 20, false, 0, 100),
        Is.EqualTo(110));


    [Test]
    public void Calculate_UnknownOperationThrows() => Assert.That(
        () => ContactScoreCalculator.Calculate(0, (ContactScoreOperation)42, 1, false, 0, 0),
        Throws.TypeOf<ArgumentOutOfRangeException>());


    [Test]
    public void GetStoredValue_NullAndDbNullAreEmpty() => Assert.That(
        new[] { ContactScoreCalculator.GetStoredValue(null), ContactScoreCalculator.GetStoredValue(DBNull.Value) },
        Is.All.Null);


    [TestCase(0)]
    [TestCase(-7)]
    [TestCase(42)]
    public void GetStoredValue_ReturnsStoredInteger(int value) => Assert.That(
        ContactScoreCalculator.GetStoredValue(value),
        Is.EqualTo(value));


    [TestCase("Add", true, ContactScoreOperation.Add)]
    [TestCase("Subtract", true, ContactScoreOperation.Subtract)]
    [TestCase("Set", true, ContactScoreOperation.Set)]
    [TestCase("add", false, default(ContactScoreOperation))]
    [TestCase("1", false, default(ContactScoreOperation))]
    [TestCase("", false, default(ContactScoreOperation))]
    [TestCase(null, false, default(ContactScoreOperation))]
    public void TryParseOperation_AcceptsExactNamesOnly(string? value, bool expectedResult, ContactScoreOperation expectedOperation)
    {
        bool result = ContactScoreCalculator.TryParseOperation(value, out var operation);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(operation, Is.EqualTo(expectedOperation));
        });
    }


    [Test]
    public void GetConfigurationError_ValidPropertiesReturnNullAndOperation()
    {
        var properties = CreateValidProperties();
        properties.Operation = nameof(ContactScoreOperation.Subtract);
        properties.LimitRange = true;
        properties.Minimum = 5;
        properties.Maximum = 5;

        string? error = ContactScoreCalculator.GetConfigurationError(properties, true, out var operation);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(operation, Is.EqualTo(ContactScoreOperation.Subtract));
        });
    }


    [Test]
    public void GetConfigurationError_MinimumAboveMaximumIgnoredWhenLimitDisabled()
    {
        var properties = CreateValidProperties();
        properties.Minimum = 100;
        properties.Maximum = 0;

        Assert.That(ContactScoreCalculator.GetConfigurationError(properties, true, out _), Is.Null);
    }


    [TestCase("", true, "Add", 10, false, 0, 100, "no field")]
    [TestCase(" ", true, "Add", 10, false, 0, 100, "no field")]
    [TestCase("AcmeContactScore", false, "Add", 10, false, 0, 100, "isn't an integer custom contact field")]
    [TestCase("AcmeContactScore", true, "Multiply", 10, false, 0, 100, "operation 'Multiply' is unknown")]
    [TestCase("AcmeContactScore", true, "", 10, false, 0, 100, "operation '' is unknown")]
    [TestCase("AcmeContactScore", true, "Add", -1, false, 0, 100, "amount -1 is negative")]
    [TestCase("AcmeContactScore", true, "Set", -1, false, 0, 100, "amount -1 is negative")]
    [TestCase("AcmeContactScore", true, "Add", 10, true, 101, 100, "minimum 101 is greater than maximum 100")]
    public void GetConfigurationError_ReturnsReason(
        string field,
        bool fieldIsEligible,
        string operation,
        int amount,
        bool limitRange,
        int minimum,
        int maximum,
        string expectedReason)
    {
        var properties = new AdjustContactScoreActionProperties
        {
            Field = field,
            Operation = operation,
            Amount = amount,
            LimitRange = limitRange,
            Minimum = minimum,
            Maximum = maximum
        };

        string? error = ContactScoreCalculator.GetConfigurationError(properties, fieldIsEligible, out _);

        Assert.That(error, Does.Contain(expectedReason));
    }


    [Test]
    public void Properties_Defaults()
    {
        var properties = new AdjustContactScoreActionProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.Field, Is.Empty);
            Assert.That(properties.Operation, Is.EqualTo(nameof(ContactScoreOperation.Add)));
            Assert.That(properties.Amount, Is.EqualTo(10));
            Assert.That(properties.LimitRange, Is.False);
            Assert.That(properties.Minimum, Is.Zero);
            Assert.That(properties.Maximum, Is.EqualTo(100));
            Assert.That(properties.SkipStep, Is.False);
        });
    }


    private static AdjustContactScoreActionProperties CreateValidProperties() => new() { Field = "AcmeContactScore" };
}
