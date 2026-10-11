using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class SetContactFieldEvaluatorTests
{
    [TestCase("SetValue", true, SetContactFieldMode.SetValue)]
    [TestCase("Calculate", true, SetContactFieldMode.Calculate)]
    [TestCase("setvalue", false, default(SetContactFieldMode))]
    [TestCase("0", false, default(SetContactFieldMode))]
    [TestCase("1", false, default(SetContactFieldMode))]
    [TestCase("", false, default(SetContactFieldMode))]
    [TestCase(null, false, default(SetContactFieldMode))]
    public void TryParseMode_ExactNamesOnly(string? value, bool expected, SetContactFieldMode expectedMode) => Assert.Multiple(() =>
    {
        Assert.That(SetContactFieldEvaluator.TryParseMode(value, out var mode), Is.EqualTo(expected));
        Assert.That(mode, Is.EqualTo(expectedMode));
    });


    [TestCase("Add", true)]
    [TestCase("Subtract", true)]
    [TestCase("Multiply", true)]
    [TestCase("Divide", true)]
    [TestCase("Modulo", true)]
    [TestCase("add", false)]
    [TestCase("4", false)]
    [TestCase("Add,Subtract", false)]
    [TestCase(null, false)]
    public void TryParseOperator_ExactNamesOnly(string? value, bool expected) =>
        Assert.That(SetContactFieldEvaluator.TryParseOperator(value, out _), Is.EqualTo(expected));


    [Test]
    public void DefaultProperties_SetValueAddOperandOneResultOne()
    {
        var properties = new SetContactFieldActionProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.Field, Is.Empty);
            Assert.That(properties.Mode, Is.EqualTo("SetValue"));
            Assert.That(properties.ValueTemplate, Is.Empty);
            Assert.That(properties.OnlyIfEmpty, Is.False);
            Assert.That(properties.Operator, Is.EqualTo("Add"));
            Assert.That(properties.Operand, Is.EqualTo("1"));
            Assert.That(properties.ResultNumber, Is.EqualTo("1"));
            Assert.That(properties.SkipStep, Is.False);
        });
    }


    [TestCase("", "SetValue", "Add", "1", "no field is selected")]
    [TestCase("Missing", "SetValue", "Add", "1", "doesn't exist or can't be changed")]
    [TestCase("ContactCreated", "SetValue", "Add", "1", "doesn't exist or can't be changed")]
    [TestCase("AcmeCounter", "Unknown", "Add", "1", "mode 'Unknown' is unknown")]
    [TestCase("AcmeCounter", "1", "Add", "1", "mode '1' is unknown")]
    [TestCase("AcmeLastCampaign", "Calculate", "Add", "1", "isn't a number field")]
    [TestCase("AcmeVip", "Calculate", "Add", "1", "isn't a number field")]
    [TestCase("AcmeLastVisit", "Calculate", "Add", "1", "isn't a number field")]
    [TestCase("AcmeCounter", "Calculate", "Power", "1", "operator 'Power' is unknown")]
    [TestCase("AcmeCounter", "Calculate", "Add", " ", "no operand is entered")]
    public void GetConfigurationError_Invalid(string field, string mode, string arithmeticOperator, string operand, string expected)
    {
        var properties = new SetContactFieldActionProperties { Field = field, Mode = mode, Operator = arithmeticOperator, Operand = operand };

        string? error = SetContactFieldEvaluator.GetConfigurationError(properties, ContactFieldResolverTests.CreateResolver().Resolve(field), out _, out _);

        Assert.That(error, Does.Contain(expected));
    }


    [TestCase("AcmeLastCampaign", "SetValue", "Power", "")]
    [TestCase("AcmeCounter", "Calculate", "Divide", "2")]
    [TestCase("AcmeRating", "Calculate", "Modulo", "{triggerData.Years}")]
    [TestCase("AcmeWeight", "Calculate", "Multiply", "1.5")]
    [TestCase("AcmeRevenue", "Calculate", "Subtract", "1")]
    public void GetConfigurationError_Valid(string field, string mode, string arithmeticOperator, string operand)
    {
        var properties = new SetContactFieldActionProperties { Field = field, Mode = mode, Operator = arithmeticOperator, Operand = operand };

        Assert.That(SetContactFieldEvaluator.GetConfigurationError(properties, ContactFieldResolverTests.CreateResolver().Resolve(field), out _, out _), Is.Null);
    }


    [Test]
    public void Evaluate_SetValueWritesConvertedValue()
    {
        var outcome = Evaluate(new() { Field = "AcmeLastCampaign", ValueTemplate = "{processName}" }, null);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(SetContactFieldOutcomeKind.Write));
            Assert.That(outcome.NewValue, Is.EqualTo("Win-back"));
            Assert.That(outcome.Message, Is.EqualTo(SetContactFieldEvaluator.MESSAGE_UPDATED));
            Assert.That(outcome.Succeeded, Is.True);
        });
    }


    [Test]
    public void Evaluate_SetValueSameValueIsUnchanged() =>
        Assert.That(Evaluate(new() { Field = "AcmeCounter", ValueTemplate = "5" }, 5).Kind, Is.EqualTo(SetContactFieldOutcomeKind.Unchanged));


    [Test]
    public void Evaluate_EmptyTemplateClearsField()
    {
        var outcome = Evaluate(new() { Field = "AcmeCounter", ValueTemplate = "" }, 5);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(SetContactFieldOutcomeKind.Write));
            Assert.That(outcome.NewValue, Is.Null);
        });
    }


    [Test]
    public void Evaluate_EmptyTemplateOnEmptyFieldIsUnchanged() =>
        Assert.That(Evaluate(new() { Field = "AcmeLastCampaign", ValueTemplate = "" }, DBNull.Value).Kind, Is.EqualTo(SetContactFieldOutcomeKind.Unchanged));


    [TestCase("existing", "NotEmpty")]
    [TestCase("", "Write")]
    [TestCase(null, "Write")]
    public void Evaluate_OnlyIfEmpty(string? current, string expected)
    {
        var outcome = Evaluate(new() { Field = "AcmeLastCampaign", ValueTemplate = "New", OnlyIfEmpty = true }, current);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind.ToString(), Is.EqualTo(expected));
            Assert.That(outcome.Succeeded, Is.True);
        });
    }


    [Test]
    public void Evaluate_OnlyIfEmptyIgnoredInCalculateMode() =>
        Assert.That(Evaluate(new() { Field = "AcmeCounter", Mode = "Calculate", Operand = "1", OnlyIfEmpty = true }, 5).NewValue, Is.EqualTo(6));


    [Test]
    public void Evaluate_UnresolvedTokenBecomesEmptyAndStepSucceeds()
    {
        var outcome = Evaluate(new() { Field = "AcmeLastCampaign", ValueTemplate = "A{missing}B" }, null);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(SetContactFieldOutcomeKind.Write));
            Assert.That(outcome.NewValue, Is.EqualTo("AB"));
            Assert.That(outcome.UnresolvedTokens, Is.EqualTo(new[] { "missing" }));
        });
    }


    [Test]
    public void Evaluate_TruncationReported()
    {
        var outcome = Evaluate(new() { Field = "AcmeLastCampaign", ValueTemplate = "{processName} campaign" }, null);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.NewValue, Is.EqualTo("Win-back c"));
            Assert.That(outcome.Truncated, Is.True);
        });
    }


    [Test]
    public void Evaluate_ConversionFailureFails()
    {
        var outcome = Evaluate(new() { Field = "AcmeCounter", ValueTemplate = "{processName}" }, 1);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(SetContactFieldOutcomeKind.Failed));
            Assert.That(outcome.Message, Is.EqualTo(SetContactFieldEvaluator.MESSAGE_CONVERSION_FAILED));
            Assert.That(outcome.Error, Is.Not.Empty.And.Not.Contain("Win-back"));
            Assert.That(outcome.Succeeded, Is.False);
        });
    }


    [Test]
    public void Evaluate_UnresolvedTokenInNumberFieldClearsField() =>
        Assert.That(Evaluate(new() { Field = "AcmeCounter", ValueTemplate = "{triggerData.Missing}" }, 3).NewValue, Is.Null);


    [Test]
    public void Evaluate_CalculateUsesTokensInOperand() =>
        Assert.That(Evaluate(new() { Field = "AcmeCounter", Mode = "Calculate", Operator = "Add", Operand = "{contact.AcmeOther}" }, 10).NewValue, Is.EqualTo(13));


    [Test]
    public void Evaluate_CalculateUnresolvedOperandFails()
    {
        var outcome = Evaluate(new() { Field = "AcmeCounter", Mode = "Calculate", Operand = "{triggerData.Years}" }, 10);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(SetContactFieldOutcomeKind.Failed));
            Assert.That(outcome.Message, Is.EqualTo(SetContactFieldEvaluator.MESSAGE_CALCULATION_FAILED));
            Assert.That(outcome.UnresolvedTokens, Is.EqualTo(new[] { "triggerData.Years" }));
        });
    }


    [Test]
    public void Evaluate_CalculateDivisionByZeroFails() =>
        Assert.That(Evaluate(new() { Field = "AcmeCounter", Mode = "Calculate", Operator = "Divide", Operand = "0" }, 10).Error,
            Is.EqualTo(ContactFieldCalculator.DIVISION_BY_ZERO));


    [Test]
    public void Evaluate_CalculateZeroResultOnEmptyFieldIsWritten() =>
        Assert.That(Evaluate(new() { Field = "AcmeCounter", Mode = "Calculate", Operator = "Multiply", Operand = "5" }, null).Kind,
            Is.EqualTo(SetContactFieldOutcomeKind.Write));


    [TestCase(null, null, ExpectedResult = true)]
    [TestCase("", null, ExpectedResult = true)]
    [TestCase("a", "a", ExpectedResult = true)]
    [TestCase("a", "A", ExpectedResult = false)]
    [TestCase(0, null, ExpectedResult = false)]
    [TestCase(1, 1, ExpectedResult = true)]
    public bool AreEqual(object? current, object? newValue) => SetContactFieldEvaluator.AreEqual(current, newValue);


    [Test]
    public void AreEqual_DecimalScaleIgnored() => Assert.That(SetContactFieldEvaluator.AreEqual(1.50m, 1.5m), Is.True);


    private static SetContactFieldOutcome Evaluate(SetContactFieldActionProperties properties, object? current)
    {
        var field = ContactFieldResolverTests.CreateResolver().Resolve(properties.Field)!;
        Assert.That(SetContactFieldEvaluator.GetConfigurationError(properties, field, out var mode, out var arithmeticOperator), Is.Null);

        return SetContactFieldEvaluator.Evaluate(properties, field, mode, arithmeticOperator, current, new ContactFieldTokenValues
        {
            ProcessName = "Win-back",
            GetContactFieldValue = name => string.Equals(name, "AcmeOther", StringComparison.OrdinalIgnoreCase) ? "3" : null
        });
    }
}
