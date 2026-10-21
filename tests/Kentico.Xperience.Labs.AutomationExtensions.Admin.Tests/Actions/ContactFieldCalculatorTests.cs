using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ContactFieldCalculatorTests
{
    [TestCase(ArithmeticOperator.Add, 7, "3", 10)]
    [TestCase(ArithmeticOperator.Subtract, 7, "3", 4)]
    [TestCase(ArithmeticOperator.Multiply, 7, "3", 21)]
    [TestCase(ArithmeticOperator.Divide, 7, "2", 3)]
    [TestCase(ArithmeticOperator.Divide, -7, "2", -3)]
    [TestCase(ArithmeticOperator.Modulo, 7, "3", 1)]
    [TestCase(ArithmeticOperator.Modulo, -7, "3", -1)]
    [TestCase(ArithmeticOperator.Add, -5, "-5", -10)]
    [TestCase(ArithmeticOperator.Subtract, 0, " 4 ", -4)]
    public void Integer(ArithmeticOperator arithmeticOperator, int current, string operand, int expected) =>
        AssertCalculated("AcmeCounter", current, arithmeticOperator, operand, expected);


    [TestCase(ArithmeticOperator.Add, 9_000_000_000L, "1", 9_000_000_001L)]
    [TestCase(ArithmeticOperator.Subtract, 5L, "10", -5L)]
    [TestCase(ArithmeticOperator.Multiply, 3_000_000_000L, "3", 9_000_000_000L)]
    [TestCase(ArithmeticOperator.Divide, 9L, "4", 2L)]
    [TestCase(ArithmeticOperator.Modulo, 9L, "4", 1L)]
    public void LongInteger(ArithmeticOperator arithmeticOperator, long current, string operand, long expected) =>
        AssertCalculated("AcmeRevenue", current, arithmeticOperator, operand, expected);


    [TestCase(ArithmeticOperator.Add, "1.25", "0.5", "1.75")]
    [TestCase(ArithmeticOperator.Subtract, "1.25", "2", "-0.75")]
    [TestCase(ArithmeticOperator.Multiply, "1.5", "1.5", "2.25")]
    [TestCase(ArithmeticOperator.Divide, "7", "2", "3.5")]
    [TestCase(ArithmeticOperator.Modulo, "7.5", "2", "1.5")]
    public void Decimal(ArithmeticOperator arithmeticOperator, string current, string operand, string expected) =>
        AssertCalculated("AcmeRating", ParseDecimal(current), arithmeticOperator, operand, ParseDecimal(expected));


    [TestCase(ArithmeticOperator.Add, 1.5, "0.25", 1.75)]
    [TestCase(ArithmeticOperator.Subtract, 1.5, "2", -0.5)]
    [TestCase(ArithmeticOperator.Multiply, 1.5, "-2", -3d)]
    [TestCase(ArithmeticOperator.Divide, 7d, "2", 3.5)]
    [TestCase(ArithmeticOperator.Modulo, 7.5, "2", 1.5)]
    public void Double(ArithmeticOperator arithmeticOperator, double current, string operand, double expected) =>
        AssertCalculated("AcmeWeight", current, arithmeticOperator, operand, expected);


    [TestCase("AcmeCounter", 1)]
    [TestCase("AcmeRevenue", 1L)]
    [TestCase("AcmeWeight", 1d)]
    public void EmptyCurrentValueCountsAsZero(string fieldName, object expected) => Assert.Multiple(() =>
    {
        AssertCalculated(fieldName, null, ArithmeticOperator.Add, "1", expected);
        AssertCalculated(fieldName, DBNull.Value, ArithmeticOperator.Add, "1", expected);
    });


    [Test]
    public void EmptyDecimalCountsAsZero() => AssertCalculated("AcmeRating", null, ArithmeticOperator.Add, "1", 1m);


    [TestCase("AcmeCounter", ArithmeticOperator.Divide)]
    [TestCase("AcmeCounter", ArithmeticOperator.Modulo)]
    [TestCase("AcmeRevenue", ArithmeticOperator.Divide)]
    [TestCase("AcmeRevenue", ArithmeticOperator.Modulo)]
    [TestCase("AcmeRating", ArithmeticOperator.Divide)]
    [TestCase("AcmeRating", ArithmeticOperator.Modulo)]
    [TestCase("AcmeWeight", ArithmeticOperator.Divide)]
    [TestCase("AcmeWeight", ArithmeticOperator.Modulo)]
    public void DivisionByZeroFails(string fieldName, ArithmeticOperator arithmeticOperator) =>
        AssertFailed(fieldName, 5, arithmeticOperator, "0", ContactFieldCalculator.DIVISION_BY_ZERO);


    [TestCase(int.MaxValue, ArithmeticOperator.Add, "1")]
    [TestCase(int.MinValue, ArithmeticOperator.Subtract, "1")]
    [TestCase(int.MaxValue, ArithmeticOperator.Multiply, "2")]
    [TestCase(int.MinValue, ArithmeticOperator.Divide, "-1")]
    public void IntegerOverflowFails(int current, ArithmeticOperator arithmeticOperator, string operand) =>
        AssertFailed("AcmeCounter", current, arithmeticOperator, operand, ContactFieldCalculator.OVERFLOW);


    [Test]
    public void LongIntegerOverflowFails() => AssertFailed("AcmeRevenue", long.MaxValue, ArithmeticOperator.Add, "1", ContactFieldCalculator.OVERFLOW);


    [Test]
    public void DecimalOverflowFails() => AssertFailed("AcmeRating", decimal.MaxValue, ArithmeticOperator.Multiply, "2", ContactFieldCalculator.OVERFLOW);


    [Test]
    public void DoubleOverflowFails() => AssertFailed("AcmeWeight", double.MaxValue, ArithmeticOperator.Multiply, "2", ContactFieldCalculator.OVERFLOW);


    [Test]
    public void IntegerMinValueModuloMinusOneDoesNotThrow()
    {
        var result = ContactFieldCalculator.Calculate(int.MinValue, ArithmeticOperator.Modulo, "-1", ContactFieldResolverTests.GetField("AcmeCounter"));

        Assert.That(result.Succeeded ? result.Value : result.Error, Is.EqualTo(0).Or.EqualTo(ContactFieldCalculator.OVERFLOW));
    }


    [TestCase("AcmeCounter", "1.5")]
    [TestCase("AcmeCounter", "abc")]
    [TestCase("AcmeCounter", "")]
    [TestCase("AcmeCounter", "99999999999")]
    [TestCase("AcmeRevenue", "1.0")]
    [TestCase("AcmeRating", "1,5")]
    [TestCase("AcmeWeight", "NaN")]
    [TestCase("AcmeWeight", "Infinity")]
    public void InvalidOperandFails(string fieldName, string operand)
    {
        var result = ContactFieldCalculator.Calculate(1, ArithmeticOperator.Add, operand, ContactFieldResolverTests.GetField(fieldName));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Does.StartWith("the operand isn't"));
        });
    }


    [Test]
    public void NonNumberFieldFails() =>
        Assert.That(ContactFieldCalculator.Calculate(1, ArithmeticOperator.Add, "1", ContactFieldResolverTests.GetField("AcmeLastCampaign")).Succeeded, Is.False);


    [Test]
    public void UnreadableCurrentValueFails() =>
        Assert.That(ContactFieldCalculator.Calculate("abc", ArithmeticOperator.Add, "1", ContactFieldResolverTests.GetField("AcmeCounter")).Succeeded, Is.False);


    [Test]
    public void StoredValueOfOtherNumberTypeIsConverted() =>
        AssertCalculated("AcmeRevenue", 5, ArithmeticOperator.Add, "1", 6L);


    private static decimal ParseDecimal(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);


    private static void AssertCalculated(string fieldName, object? current, ArithmeticOperator arithmeticOperator, string operand, object expected)
    {
        var result = ContactFieldCalculator.Calculate(current, arithmeticOperator, operand, ContactFieldResolverTests.GetField(fieldName));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.Error);
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.Value?.GetType(), Is.EqualTo(expected.GetType()));
        });
    }


    private static void AssertFailed(string fieldName, object current, ArithmeticOperator arithmeticOperator, string operand, string error)
    {
        var result = ContactFieldCalculator.Calculate(current, arithmeticOperator, operand, ContactFieldResolverTests.GetField(fieldName));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
        });
    }
}
