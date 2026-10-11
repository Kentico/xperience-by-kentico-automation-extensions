using System.Globalization;
using System.Reflection;
using System.Resources;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Labs.AutomationExtensions.FormComponents;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.FormComponents;

public class UrlMatchComponentTests
{
    private const string RULES = """
        {"version":1,"rules":[
          {"part":"Path","operator":"StartsWith","value":"/pricing","parameter":null},
          {"part":"QueryParameter","operator":"Equal","value":"spring","parameter":"utm_campaign"}
        ]}
        """;


    [Test]
    public async Task TestUrl_ReturnsOverallAndPerRuleResultWithNormalizedParts()
    {
        var component = new UrlMatchComponent(new ResourceLocalizationService());

        var response = await component.TestUrl(new UrlMatchTestArguments
        {
            Rules = RULES,
            Url = "https://WWW.example.com/Pricing/?utm_campaign=spring+sale&utm_source=news#plans"
        });

        var result = ((CommandResponse<UrlMatchTestResult>)response).Result;
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.RuleResults, Is.EqualTo(new[] { true, false }));
            Assert.That(result.TimedOut, Is.False);
            Assert.That(result.IsRecognized, Is.True);
            Assert.That(result.Url, Is.EqualTo("https://www.example.com/Pricing?utm_campaign=spring+sale&utm_source=news"));
            Assert.That(result.Host, Is.EqualTo("www.example.com"));
            Assert.That(result.Path, Is.EqualTo("/Pricing"));
            Assert.That(result.QueryParameters.Select(parameter => $"{parameter.Name}={parameter.Value}"),
                Is.EqualTo(new[] { "utm_campaign=spring sale", "utm_source=news" }));
        });
    }


    [Test]
    public void Test_InvalidRulesReturnLocalizedErrorAndNoMatch()
    {
        var result = UrlMatchComponent.Test(
            """{"version":1,"rules":[{"part":"Path","operator":"Equal","value":""}]}""",
            "https://www.example.com/",
            new ResourceLocalizationService());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Rule 1: Enter a value."));
        });
    }


    [Test]
    public void Test_UnrecognizedUrlHasNoParts()
    {
        var result = UrlMatchComponent.Test(RULES, "pricing", new ResourceLocalizationService());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsRecognized, Is.False);
            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.Host, Is.Null);
            Assert.That(result.Path, Is.Null);
        });
    }


    [Test]
    public void Test_NoRulesMatchEveryUrl() => Assert.That(UrlMatchComponent.Test("", "https://www.example.com/", new ResourceLocalizationService()).IsMatch, Is.True);


    [Test]
    public void TestUrl_IsCommandWithClientName()
    {
        var method = typeof(UrlMatchComponent).GetMethod(nameof(UrlMatchComponent.TestUrl))!;

        Assert.Multiple(() =>
        {
            Assert.That(method.GetCustomAttribute<FormComponentCommandAttribute>()!.CommandName, Is.EqualTo(UrlMatchComponent.TEST_URL_COMMAND));
            Assert.That(method.IsPublic, Is.True);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<ICommandResponse<UrlMatchTestResult>>)));
        });
    }


    [TestCase(UrlMatchRuleSetErrorCode.TooManyRules, null, "Use at most 5 rules.")]
    [TestCase(UrlMatchRuleSetErrorCode.MissingParameter, 2, "Rule 2: Enter the query parameter name.")]
    [TestCase(UrlMatchRuleSetErrorCode.ValueTooLong, 1, "Rule 1: Enter at most 400 characters.")]
    [TestCase(UrlMatchRuleSetErrorCode.ParameterTooLong, 1, "Rule 1: Enter a parameter name of at most 100 characters.")]
    public void GetErrorMessage_FormatsLimitsAndRuleNumber(UrlMatchRuleSetErrorCode code, int? rule, string expected) =>
        Assert.That(UrlMatchComponent.GetErrorMessage(new UrlMatchRuleSetError(code, rule), new ResourceLocalizationService()), Is.EqualTo(expected));


    [Test]
    public async Task ValidationRule_UsesRuleSetValidation()
    {
        var component = new UrlMatchComponent(new ResourceLocalizationService());
        var rule = component.ValidationRules.OfType<ValidationRule<string>>().Single();

        var valid = await rule.Validate(RULES, null!);
        var empty = await rule.Validate(string.Empty, null!);
        var invalid = await rule.Validate("""{"version":1,"rules":[{"part":"QueryParameter","operator":"Exists"}]}""", null!);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(empty.IsValid, Is.True);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.ErrorMessage, Is.EqualTo("Rule 1: Enter the query parameter name."));
        });
    }


    [Test]
    public void Registration_MatchesClientComponentAndAttribute()
    {
        var registration = typeof(UrlMatchComponent).Assembly
            .GetCustomAttributes<RegisterFormComponentAttribute>()
            .Single(attribute => attribute.Identifier == UrlMatchComponent.IDENTIFIER);

        Assert.Multiple(() =>
        {
            Assert.That(registration.MarkedType, Is.EqualTo(typeof(UrlMatchComponent)));
            Assert.That(typeof(UrlMatchComponent).GetCustomAttribute<ComponentAttributeAttribute>()!.AttributeType, Is.EqualTo(typeof(UrlMatchComponentAttribute)));
            Assert.That(new UrlMatchComponent(new ResourceLocalizationService()).ClientComponentName,
                Is.EqualTo("@kentico/xperience-labs-automation-extensions-admin/UrlMatch"));
        });
    }


    [Test]
    public async Task ClientProperties_OffersPartsOperatorsPerPartLimitsAndTexts()
    {
        var component = new UrlMatchComponent(new ResourceLocalizationService());
        var properties = new UrlMatchClientProperties();

        // GetClientProperties also serializes validation rules, whose default message needs the Xperience container.
        await (Task)typeof(UrlMatchComponent)
            .GetMethod("ConfigureClientProperties", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(component, [properties])!;

        var exists = properties.Operators.Single(option => option.Value == nameof(UrlMatchOperator.Exists));
        var startsWith = properties.Operators.Single(option => option.Value == nameof(UrlMatchOperator.StartsWith));
        Assert.Multiple(() =>
        {
            Assert.That(properties.MaxRules, Is.EqualTo(UrlMatchRuleSet.MAX_RULES));
            Assert.That(properties.MaxValueLength, Is.EqualTo(UrlMatchRuleSet.MAX_VALUE_LENGTH));
            Assert.That(properties.MaxParameterLength, Is.EqualTo(UrlMatchRuleSet.MAX_PARAMETER_LENGTH));
            Assert.That(properties.Parts.Select(option => option.Value), Is.EqualTo(Enum.GetNames<UrlPart>()));
            Assert.That(properties.Parts.Select(option => option.Text), Is.EqualTo(new[] { "Full URL", "Host", "Path", "Query parameter" }));
            Assert.That(properties.Operators.Select(option => option.Value), Is.EqualTo(Enum.GetNames<UrlMatchOperator>()));
            Assert.That(exists.Parts, Is.EqualTo(new[] { nameof(UrlPart.QueryParameter) }));
            Assert.That(exists.RequiresValue, Is.False);
            Assert.That(startsWith.Parts, Is.EqualTo(Enum.GetNames<UrlPart>()));
            Assert.That(startsWith.RequiresValue, Is.True);
            Assert.That(startsWith.Text, Is.EqualTo("starts with"));
            Assert.That(properties.Texts.Keys, Is.EquivalentTo(UrlMatchComponent.TextKeys.Keys));
            Assert.That(properties.Texts["rule"], Is.EqualTo("Rule {0}"));
        });
    }


    [Test]
    public void ResourceKeys_AllExist()
    {
        var resources = new ResourceManager(typeof(AutomationExtensionsResources));
        var keys = UrlMatchComponent.TextKeys.Values
            .Concat(Enum.GetNames<UrlPart>().Select(name => $"part.{name.ToLowerInvariant()}"))
            .Concat(Enum.GetNames<UrlMatchOperator>().Select(name => $"operator.{name.ToLowerInvariant()}"))
            .Concat(Enum.GetNames<UrlMatchRuleSetErrorCode>().Select(name => $"error.{name.ToLowerInvariant()}"))
            .Append("error.rule")
            .Select(key => UrlMatchComponent.KEY_PREFIX + key)
            .ToList();

        Assert.That(keys.Where(key => resources.GetString(key, CultureInfo.InvariantCulture) is null), Is.Empty);
    }
}
