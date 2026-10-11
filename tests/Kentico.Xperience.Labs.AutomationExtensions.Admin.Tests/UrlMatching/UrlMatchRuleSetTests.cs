using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.UrlMatching;

public class UrlMatchRuleSetTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("""{"version":1,"rules":[]}""")]
    public void TryParse_EmptyIsValidRuleSetWithoutRules(string? json)
    {
        bool valid = UrlMatchRuleSet.TryParse(json, out var ruleSet, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(ruleSet.Rules, Is.Empty);
        });
    }


    [Test]
    public void TryParse_ReadsAllPartsAndTrimsValues()
    {
        const string json = """
            {"version":1,"rules":[
              {"part":"Url","operator":"Equal","value":" https://www.example.com/ ","parameter":null},
              {"part":"Host","operator":"EndsWith","value":"example.com"},
              {"part":"Path","operator":"Matches","value":"/blog/*","parameter":"ignored"},
              {"part":"QueryParameter","operator":"Equal","value":"spring","parameter":" utm_campaign "},
              {"part":"QueryParameter","operator":"Exists","value":"ignored","parameter":"gclid"}
            ]}
            """;

        bool valid = UrlMatchRuleSet.TryParse(json, out var ruleSet, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.True, error?.Describe());
            Assert.That(ruleSet.Rules, Is.EqualTo(new[]
            {
                new UrlMatchRule(UrlPart.Url, UrlMatchOperator.Equal, "https://www.example.com/"),
                new UrlMatchRule(UrlPart.Host, UrlMatchOperator.EndsWith, "example.com"),
                // Parameter only for query parameters, value only for operators that compare one.
                new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Matches, "/blog/*"),
                new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Equal, "spring", "utm_campaign"),
                new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Exists, null, "gclid")
            }));
        });
    }


    [Test]
    public void ToJson_RoundTrips()
    {
        var ruleSet = new UrlMatchRuleSet(
        [
            new UrlMatchRule(UrlPart.Path, UrlMatchOperator.StartsWith, "/blog/"),
            new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.DoesNotExist, null, "gclid"),
            new UrlMatchRule(UrlPart.Url, UrlMatchOperator.Contains, "ü \"quoted\"")
        ]);

        string json = ruleSet.ToJson();
        bool valid = UrlMatchRuleSet.TryParse(json, out var parsed, out _);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.StartWith("""{"version":1,"rules":[{"part":"Path","operator":"StartsWith","value":"/blog/","parameter":null}"""));
            Assert.That(valid, Is.True);
            Assert.That(parsed.Rules, Is.EqualTo(ruleSet.Rules));
        });
    }


    [Test]
    public void ToJson_EmptyRuleSetIsEmptyString() => Assert.That(UrlMatchRuleSet.Empty.ToJson(), Is.Empty);


    [TestCase("not json", UrlMatchRuleSetErrorCode.InvalidJson, null)]
    [TestCase("[]", UrlMatchRuleSetErrorCode.InvalidJson, null)]
    [TestCase("""{"rules":[]}""", UrlMatchRuleSetErrorCode.UnsupportedVersion, null)]
    [TestCase("""{"version":2,"rules":[]}""", UrlMatchRuleSetErrorCode.UnsupportedVersion, null)]
    [TestCase("""{"version":"1","rules":[]}""", UrlMatchRuleSetErrorCode.UnsupportedVersion, null)]
    [TestCase("""{"version":1}""", UrlMatchRuleSetErrorCode.InvalidJson, null)]
    [TestCase("""{"version":1,"rules":{}}""", UrlMatchRuleSetErrorCode.InvalidJson, null)]
    [TestCase("""{"version":1,"rules":[1]}""", UrlMatchRuleSetErrorCode.InvalidJson, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"Path","operator":"Equal","value":5}]}""", UrlMatchRuleSetErrorCode.InvalidJson, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"Fragment","operator":"Equal","value":"x"}]}""", UrlMatchRuleSetErrorCode.UnknownPart, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"path","operator":"Equal","value":"x"}]}""", UrlMatchRuleSetErrorCode.UnknownPart, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"2","operator":"Equal","value":"x"}]}""", UrlMatchRuleSetErrorCode.UnknownPart, 1)]
    [TestCase("""{"version":1,"rules":[{"operator":"Equal","value":"x"}]}""", UrlMatchRuleSetErrorCode.UnknownPart, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"Path","operator":"Regex","value":"x"}]}""", UrlMatchRuleSetErrorCode.UnknownOperator, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"Path","operator":"Exists"}]}""", UrlMatchRuleSetErrorCode.OperatorNotAllowed, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"Host","operator":"DoesNotExist"}]}""", UrlMatchRuleSetErrorCode.OperatorNotAllowed, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"QueryParameter","operator":"Equal","value":"x","parameter":" "}]}""", UrlMatchRuleSetErrorCode.MissingParameter, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"QueryParameter","operator":"Exists"}]}""", UrlMatchRuleSetErrorCode.MissingParameter, 1)]
    [TestCase("""{"version":1,"rules":[{"part":"Path","operator":"StartsWith","value":"/a"},{"part":"Path","operator":"Equal","value":"  "}]}""", UrlMatchRuleSetErrorCode.MissingValue, 2)]
    [TestCase("""{"version":1,"rules":[{"part":"Path","operator":"Contains","value":null}]}""", UrlMatchRuleSetErrorCode.MissingValue, 1)]
    public void TryParse_InvalidRulesReturnError(string json, UrlMatchRuleSetErrorCode expectedCode, int? expectedRule)
    {
        bool valid = UrlMatchRuleSet.TryParse(json, out var ruleSet, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.False);
            Assert.That(error, Is.EqualTo(new UrlMatchRuleSetError(expectedCode, expectedRule)));
            Assert.That(ruleSet.Rules, Is.Empty);
        });
    }


    [Test]
    public void TryParse_TooManyRulesIsError()
    {
        string rule = """{"part":"Path","operator":"Contains","value":"x"}""";
        string json = $$"""{"version":1,"rules":[{{string.Join(",", Enumerable.Repeat(rule, UrlMatchRuleSet.MAX_RULES + 1))}}]}""";

        UrlMatchRuleSet.TryParse(json, out _, out var error);

        Assert.That(error, Is.EqualTo(new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.TooManyRules)));
    }


    [Test]
    public void TryParse_MaxRulesIsValid()
    {
        string rule = """{"part":"Path","operator":"Contains","value":"x"}""";
        string json = $$"""{"version":1,"rules":[{{string.Join(",", Enumerable.Repeat(rule, UrlMatchRuleSet.MAX_RULES))}}]}""";

        Assert.That(UrlMatchRuleSet.TryParse(json, out _, out _), Is.True);
    }


    [TestCase(UrlMatchRuleSet.MAX_VALUE_LENGTH, true)]
    [TestCase(UrlMatchRuleSet.MAX_VALUE_LENGTH + 1, false)]
    public void Validate_ValueLengthLimit(int length, bool expectedValid)
    {
        var ruleSet = new UrlMatchRuleSet([new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Contains, new string('a', length))]);

        Assert.That(ruleSet.Validate(), Is.EqualTo(expectedValid ? null : new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.ValueTooLong, 1)));
    }


    [TestCase(UrlMatchRuleSet.MAX_PARAMETER_LENGTH, true)]
    [TestCase(UrlMatchRuleSet.MAX_PARAMETER_LENGTH + 1, false)]
    public void Validate_ParameterLengthLimit(int length, bool expectedValid)
    {
        var ruleSet = new UrlMatchRuleSet([new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Exists, null, new string('a', length))]);

        Assert.That(ruleSet.Validate(), Is.EqualTo(expectedValid ? null : new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.ParameterTooLong, 1)));
    }


    [Test]
    public void Validate_UndefinedEnumValuesAreErrors() => Assert.Multiple(() =>
    {
        Assert.That(new UrlMatchRuleSet([new UrlMatchRule((UrlPart)42, UrlMatchOperator.Equal, "x")]).Validate()?.Code, Is.EqualTo(UrlMatchRuleSetErrorCode.UnknownPart));
        Assert.That(new UrlMatchRuleSet([new UrlMatchRule(UrlPart.Path, (UrlMatchOperator)42, "x")]).Validate()?.Code, Is.EqualTo(UrlMatchRuleSetErrorCode.UnknownOperator));
    });


    [Test]
    public void OperatorAvailability_ExistenceOperatorsOnlyForQueryParameters() => Assert.Multiple(() =>
    {
        foreach (var part in Enum.GetValues<UrlPart>())
        {
            foreach (var matchOperator in Enum.GetValues<UrlMatchOperator>())
            {
                bool existence = matchOperator is UrlMatchOperator.Exists or UrlMatchOperator.DoesNotExist;
                Assert.That(UrlMatchRuleSet.IsOperatorAllowed(part, matchOperator), Is.EqualTo(!existence || part == UrlPart.QueryParameter), $"{part} {matchOperator}");
                Assert.That(UrlMatchRuleSet.RequiresValue(matchOperator), Is.EqualTo(!existence), $"{matchOperator}");
            }
        }
    });


    [Test]
    public void PersistedNamesNeverChange() => Assert.Multiple(() =>
    {
        Assert.That(Enum.GetNames<UrlPart>(), Is.EqualTo(new[] { "Url", "Host", "Path", "QueryParameter" }));
        Assert.That(Enum.GetNames<UrlMatchOperator>(), Is.EqualTo(new[]
        {
            "Equal", "NotEqual", "Contains", "DoesNotContain", "StartsWith", "EndsWith", "Matches", "Exists", "DoesNotExist"
        }));
    });


    [Test]
    public void Describe_NamesRuleButNeverValue()
    {
        var error = new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.MissingValue, 3);

        Assert.That(error.Describe(), Is.EqualTo("rule 3: value is empty"));
    }


    [Test]
    public void Describe_HasTextForEveryCode() => Assert.That(
        Enum.GetValues<UrlMatchRuleSetErrorCode>().Select(code => new UrlMatchRuleSetError(code).Describe()),
        Has.None.EqualTo(string.Empty).And.All.Not.Matches("^[A-Z]"));
}
