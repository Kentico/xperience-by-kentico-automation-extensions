using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.UrlMatching;

public class UrlMatcherTests
{
    // Normalization of stored URLs.
    [TestCase("https://www.example.com/pricing", "https://www.example.com/pricing", "www.example.com", "/pricing")]
    [TestCase("HTTPS://WWW.Example.COM/Pricing/", "https://www.example.com/Pricing", "www.example.com", "/Pricing")]
    [TestCase("https://www.example.com", "https://www.example.com/", "www.example.com", "/")]
    [TestCase("https://www.example.com/", "https://www.example.com/", "www.example.com", "/")]
    [TestCase("https://www.example.com/#top", "https://www.example.com/", "www.example.com", "/")]
    [TestCase("https://www.example.com/?utm_source=news#plans", "https://www.example.com/?utm_source=news", "www.example.com", "/")]
    [TestCase("https://www.example.com:443/pricing", "https://www.example.com/pricing", "www.example.com", "/pricing")]
    [TestCase("http://www.example.com:80/pricing", "http://www.example.com/pricing", "www.example.com", "/pricing")]
    [TestCase("https://www.example.com:8443/pricing", "https://www.example.com:8443/pricing", "www.example.com", "/pricing")]
    [TestCase("https://www.example.com/caf%C3%A9%20menu/", "https://www.example.com/café menu", "www.example.com", "/café menu")]
    [TestCase("https://www.example.com/pricing?", "https://www.example.com/pricing", "www.example.com", "/pricing")]
    [TestCase("//cdn.example.com/a/", "//cdn.example.com/a", "cdn.example.com", "/a")]
    [TestCase("/pricing/?a=1#x", "/pricing?a=1", null, "/pricing")]
    [TestCase("?a=1", "/?a=1", null, "/")]
    [TestCase("  https://www.example.com/pricing  ", "https://www.example.com/pricing", "www.example.com", "/pricing")]
    public void Parse_NormalizesUrl(string value, string expectedUrl, string? expectedHost, string expectedPath)
    {
        var url = NormalizedUrl.Parse(value);

        Assert.Multiple(() =>
        {
            Assert.That(url.IsRecognized, Is.True);
            Assert.That(url.Url, Is.EqualTo(expectedUrl));
            Assert.That(url.Host, Is.EqualTo(expectedHost));
            Assert.That(url.Path, Is.EqualTo(expectedPath));
        });
    }


    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("pricing")]
    [TestCase("www.example.com/pricing")]
    [TestCase("mailto:jane@example.com")]
    [TestCase("https://")]
    [TestCase("#top")]
    public void Parse_UnrecognizedUrlsHaveNoParts(string? value)
    {
        var url = NormalizedUrl.Parse(value);

        Assert.Multiple(() =>
        {
            Assert.That(url.IsRecognized, Is.False);
            Assert.That(url.Url, Is.Null);
            Assert.That(url.Host, Is.Null);
            Assert.That(url.Path, Is.Null);
            Assert.That(url.QueryParameters, Is.Empty);
        });
    }


    [Test]
    public void Parse_DecodesQueryParametersInStoredOrder()
    {
        var url = NormalizedUrl.Parse("https://www.example.com/?utm_campaign=spring+sale&Tag=a&tag=b%26c&empty=&flag&=orphan&caf%C3%A9=%E2%9C%93");

        Assert.That(url.QueryParameters.Select(parameter => $"{parameter.Key}={parameter.Value}"), Is.EqualTo(new[]
        {
            "utm_campaign=spring sale",
            "Tag=a",
            "tag=b&c",
            "empty=",
            "flag=",
            "café=✓"
        }));
    }


    // Every part × operator.
    [TestCase(UrlPart.Url, UrlMatchOperator.Equal, "https://www.example.com/blog/post?utm_campaign=Spring", true)]
    [TestCase(UrlPart.Url, UrlMatchOperator.Equal, "HTTPS://www.example.com:443/blog/post/?utm_campaign=Spring", true)]
    [TestCase(UrlPart.Url, UrlMatchOperator.Equal, "https://www.example.com/blog", false)]
    [TestCase(UrlPart.Url, UrlMatchOperator.NotEqual, "https://www.example.com/blog", true)]
    [TestCase(UrlPart.Url, UrlMatchOperator.Contains, "/blog/", true)]
    [TestCase(UrlPart.Url, UrlMatchOperator.DoesNotContain, "/blog/", false)]
    [TestCase(UrlPart.Url, UrlMatchOperator.StartsWith, "https://www.example.com/blog", true)]
    [TestCase(UrlPart.Url, UrlMatchOperator.EndsWith, "=spring", true)]
    [TestCase(UrlPart.Url, UrlMatchOperator.EndsWith, "/post", false)]
    [TestCase(UrlPart.Url, UrlMatchOperator.Matches, "https://*.example.com/blog/*", true)]
    [TestCase(UrlPart.Host, UrlMatchOperator.Equal, "WWW.example.com", true)]
    [TestCase(UrlPart.Host, UrlMatchOperator.Equal, "example.com", false)]
    [TestCase(UrlPart.Host, UrlMatchOperator.NotEqual, "example.com", true)]
    [TestCase(UrlPart.Host, UrlMatchOperator.Contains, "example", true)]
    [TestCase(UrlPart.Host, UrlMatchOperator.DoesNotContain, "example", false)]
    [TestCase(UrlPart.Host, UrlMatchOperator.StartsWith, "www.", true)]
    [TestCase(UrlPart.Host, UrlMatchOperator.EndsWith, "example.com", true)]
    [TestCase(UrlPart.Host, UrlMatchOperator.Matches, "*.example.com", true)]
    [TestCase(UrlPart.Host, UrlMatchOperator.Matches, "example.com", false)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Equal, "/blog/post", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Equal, "/Blog/Post/", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Equal, "blog/post", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Equal, "/blog", false)]
    [TestCase(UrlPart.Path, UrlMatchOperator.NotEqual, "/blog", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Contains, "log/po", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.DoesNotContain, "utm", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.StartsWith, "/blog/", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.StartsWith, "blog", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.EndsWith, "post", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Matches, "/blog/*", true)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Matches, "/blog", false)]
    [TestCase(UrlPart.Path, UrlMatchOperator.Matches, "*/post", true)]
    public void Evaluate_EveryPartAndOperator(UrlPart part, UrlMatchOperator matchOperator, string value, bool expected) =>
        AssertMatch("https://www.example.com/blog/post?utm_campaign=Spring#comments", new UrlMatchRule(part, matchOperator, value), expected);


    [TestCase(UrlMatchOperator.Equal, "spring", true)]
    [TestCase(UrlMatchOperator.Equal, "Spring sale", false)]
    [TestCase(UrlMatchOperator.NotEqual, "fall", true)]
    [TestCase(UrlMatchOperator.NotEqual, "spring", false)]
    [TestCase(UrlMatchOperator.Contains, "pri", true)]
    [TestCase(UrlMatchOperator.DoesNotContain, "pri", false)]
    [TestCase(UrlMatchOperator.StartsWith, "spr", true)]
    [TestCase(UrlMatchOperator.EndsWith, "ING", true)]
    [TestCase(UrlMatchOperator.Matches, "s*g", true)]
    [TestCase(UrlMatchOperator.Exists, null, true)]
    [TestCase(UrlMatchOperator.DoesNotExist, null, false)]
    public void Evaluate_QueryParameterOperators(UrlMatchOperator matchOperator, string? value, bool expected) =>
        AssertMatch("https://www.example.com/?UTM_Campaign=Spring", new UrlMatchRule(UrlPart.QueryParameter, matchOperator, value, "utm_campaign"), expected);


    [TestCase(UrlMatchOperator.Equal, "spring", false)]
    [TestCase(UrlMatchOperator.NotEqual, "spring", true)]
    [TestCase(UrlMatchOperator.DoesNotContain, "spring", true)]
    [TestCase(UrlMatchOperator.Exists, null, false)]
    [TestCase(UrlMatchOperator.DoesNotExist, null, true)]
    public void Evaluate_MissingQueryParameterMatchesOnlyNegatedRules(UrlMatchOperator matchOperator, string? value, bool expected) =>
        AssertMatch("https://www.example.com/?utm_source=news", new UrlMatchRule(UrlPart.QueryParameter, matchOperator, value, "utm_campaign"), expected);


    [TestCase(UrlMatchOperator.Equal, "b", true)]
    [TestCase(UrlMatchOperator.NotEqual, "b", false)]
    [TestCase(UrlMatchOperator.DoesNotContain, "c", true)]
    public void Evaluate_RepeatedParameterMatchesIfAnyValueMatches(UrlMatchOperator matchOperator, string value, bool expected) =>
        AssertMatch("https://www.example.com/?tag=a&tag=b", new UrlMatchRule(UrlPart.QueryParameter, matchOperator, value, "tag"), expected);


    [Test]
    public void Evaluate_EmptyParameterValueExists() => Assert.Multiple(() =>
    {
        AssertMatch("https://www.example.com/?preview", new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Exists, null, "preview"), true);
        AssertMatch("https://www.example.com/?preview=", new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Equal, "x", "preview"), false);
    });


    [Test]
    public void Evaluate_PercentEncodingOfParameterNameAndValue() => Assert.Multiple(() =>
    {
        AssertMatch("https://www.example.com/?q=caf%C3%A9+menu", new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Equal, "café menu", "q"), true);
        AssertMatch("https://www.example.com/?q=caf%C3%A9+menu", new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Equal, "caf%C3%A9 menu", "q"), true);
        AssertMatch("https://www.example.com/?my%20param=1", new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Exists, null, "my param"), true);
    });


    [TestCase("https://www.example.com", "/", true)]
    [TestCase("https://www.example.com/", "/", true)]
    [TestCase("https://www.example.com/?utm_source=news", "/", true)]
    [TestCase("https://www.example.com/#top", "/", true)]
    [TestCase("https://www.example.com/en", "/", false)]
    [TestCase("https://www.example.com/pricing/", "/pricing", true)]
    [TestCase("https://www.example.com/pricing", "/pricing/", true)]
    [TestCase("https://www.example.com/pricing-faq", "/pricing", false)]
    [TestCase("https://www.example.com/caf%C3%A9", "/café", true)]
    [TestCase("https://www.example.com/caf%C3%A9", "/caf%C3%A9", true)]
    [TestCase("https://www.example.com/a%20b", "/a b", true)]
    [TestCase("/pricing?x=1", "/pricing", true)]
    public void Evaluate_PathEqualNormalizesBothSides(string url, string value, bool expected) =>
        AssertMatch(url, new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Equal, value), expected);


    [TestCase("https://www.example.com/blog/", "/blog/", false)]
    [TestCase("https://www.example.com/blog/post", "/blog/", true)]
    [TestCase("https://www.example.com/blogging", "/blog", true)]
    public void Evaluate_PathStartsWithKeepsTrailingSlashOfValue(string url, string value, bool expected) =>
        AssertMatch(url, new UrlMatchRule(UrlPart.Path, UrlMatchOperator.StartsWith, value), expected);


    [TestCase("https://www.example.com/blog/a/b", "/blog/*", true)]
    [TestCase("https://www.example.com/blog", "/blog/*", false)]
    [TestCase("https://www.example.com/blog/", "/blog*", true)]
    [TestCase("https://www.example.com/en/pricing", "/*/pricing", true)]
    [TestCase("https://www.example.com/pricing", "*", true)]
    [TestCase("https://www.example.com/a.b", "/a.b", true)]
    [TestCase("https://www.example.com/axb", "/a.b", false)]
    [TestCase("https://www.example.com/a(b)+c", "/a(b)+c", true)]
    [TestCase("https://www.example.com/a$b^c", "/a$b^c", true)]
    [TestCase("https://www.example.com/abc", "/a?c", false)]
    [TestCase("https://www.example.com/a[b]c", "/a[b]c", true)]
    [TestCase("https://www.example.com/a/b", "/a\\/b", false)]
    public void Evaluate_WildcardEscapesRegexCharacters(string url, string value, bool expected) =>
        AssertMatch(url, new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Matches, value), expected);


    [Test]
    public void Evaluate_WildcardTimeoutCountsAsNoMatch()
    {
        var ruleSet = new UrlMatchRuleSet([new UrlMatchRule(UrlPart.Url, UrlMatchOperator.Matches, "*a*a*a*a*a*a*a*a*a*a*a*a*a*b")]);
        var matcher = UrlMatcher.Create(ruleSet, TimeSpan.FromTicks(1));

        var evaluation = matcher.Evaluate("https://www.example.com/" + new string('a', 3000));

        Assert.Multiple(() =>
        {
            Assert.That(evaluation.IsMatch, Is.False);
            Assert.That(evaluation.TimedOut, Is.True);
        });
    }


    [TestCase("pricing")]
    [TestCase("mailto:jane@example.com")]
    [TestCase(null)]
    public void Evaluate_UnrecognizedUrlMatchesOnlyNegatedRules(string? url) => Assert.Multiple(() =>
    {
        AssertMatch(url, new UrlMatchRule(UrlPart.Url, UrlMatchOperator.Contains, "pricing"), false);
        AssertMatch(url, new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Matches, "*"), false);
        AssertMatch(url, new UrlMatchRule(UrlPart.Url, UrlMatchOperator.DoesNotContain, "pricing"), true);
        AssertMatch(url, new UrlMatchRule(UrlPart.Path, UrlMatchOperator.NotEqual, "/"), true);
        AssertMatch(url, new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.DoesNotExist, null, "a"), true);
    });


    [Test]
    public void Evaluate_RelativeUrlHasNoHost() => Assert.Multiple(() =>
    {
        AssertMatch("/pricing", new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Equal, "/pricing"), true);
        AssertMatch("/pricing", new UrlMatchRule(UrlPart.Host, UrlMatchOperator.Contains, "example"), false);
        AssertMatch("/pricing", new UrlMatchRule(UrlPart.Host, UrlMatchOperator.NotEqual, "www.example.com"), true);
    });


    [Test]
    public void Evaluate_FragmentIsNeverCompared() => Assert.Multiple(() =>
    {
        AssertMatch("https://www.example.com/pricing#plans", new UrlMatchRule(UrlPart.Url, UrlMatchOperator.Contains, "plans"), false);
        AssertMatch("https://www.example.com/pricing#plans", new UrlMatchRule(UrlPart.Url, UrlMatchOperator.Equal, "https://www.example.com/pricing"), true);
    });


    [Test]
    public void Evaluate_AllRulesMustMatchAndResultsArePerRule()
    {
        var matcher = UrlMatcher.Create(new UrlMatchRuleSet(
        [
            new UrlMatchRule(UrlPart.Path, UrlMatchOperator.StartsWith, "/pricing"),
            new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Equal, "spring", "utm_campaign"),
            new UrlMatchRule(UrlPart.Host, UrlMatchOperator.EndsWith, "example.com")
        ]));

        var evaluation = matcher.Evaluate("https://shop.example.com/pricing?utm_campaign=fall");

        Assert.Multiple(() =>
        {
            Assert.That(evaluation.IsMatch, Is.False);
            Assert.That(evaluation.RuleResults, Is.EqualTo(new[] { true, false, true }));
            Assert.That(evaluation.TimedOut, Is.False);
        });
    }


    [Test]
    public void Evaluate_NoRulesMatchEverything() => Assert.Multiple(() =>
    {
        Assert.That(UrlMatcher.Create(UrlMatchRuleSet.Empty).Evaluate("https://www.example.com/").IsMatch, Is.True);
        Assert.That(UrlMatcher.Create(UrlMatchRuleSet.Empty).Evaluate((string?)null).IsMatch, Is.True);
    });


    [Test]
    public void GetPrefilterLiterals_LongestUnreservedLiteralOfPositiveRules()
    {
        var ruleSet = new UrlMatchRuleSet(
        [
            new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Matches, "/blog/*/2026-spring"),
            new UrlMatchRule(UrlPart.Url, UrlMatchOperator.Equal, "https://www.example.com:443/caf%C3%A9-menu/"),
            new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Equal, "a b", "utm_campaign"),
            new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.Exists, null, "gclid"),
            new UrlMatchRule(UrlPart.Path, UrlMatchOperator.DoesNotContain, "/internal-preview"),
            new UrlMatchRule(UrlPart.QueryParameter, UrlMatchOperator.DoesNotExist, null, "preview_mode"),
            new UrlMatchRule(UrlPart.Path, UrlMatchOperator.Equal, "/")
        ]);

        Assert.That(UrlMatcher.GetPrefilterLiterals(ruleSet), Is.EqualTo(new[]
        {
            "2026-spring",
            // Normalized value: default port dropped, path decoded – "é" ends the literal.
            "www.example.com",
            "utm_campaign",
            "gclid"
        }));
    }


    [TestCase("https://www.example.com:443/pricing", UrlPart.Url, UrlMatchOperator.Equal, "https://www.example.com/pricing")]
    [TestCase("https://WWW.EXAMPLE.COM/Pricing/#x", UrlPart.Url, UrlMatchOperator.StartsWith, "https://www.example.com/pricing")]
    [TestCase("https://www.example.com/caf%C3%A9-menu", UrlPart.Path, UrlMatchOperator.Equal, "/café-menu")]
    [TestCase("https://www.example.com/blog/a-long-post", UrlPart.Path, UrlMatchOperator.Matches, "/blog/*-post")]
    [TestCase("https://www.example.com/?utm_campaign=spring+sale", UrlPart.QueryParameter, UrlMatchOperator.Equal, "spring sale")]
    [TestCase("https://www.example.com/?utm_campaign=spring%20sale", UrlPart.QueryParameter, UrlMatchOperator.Contains, "spring sale")]
    public void GetPrefilterLiterals_MatchingStoredUrlContainsLiteral(string storedUrl, UrlPart part, UrlMatchOperator matchOperator, string value)
    {
        // The prefilter must never exclude a stored URL the .NET matcher would count.
        var rule = new UrlMatchRule(part, matchOperator, value, part == UrlPart.QueryParameter ? "utm_campaign" : null);
        var ruleSet = new UrlMatchRuleSet([rule]);

        Assert.Multiple(() =>
        {
            Assert.That(UrlMatcher.Create(ruleSet).Evaluate(storedUrl).IsMatch, Is.True);
            Assert.That(UrlMatcher.GetPrefilterLiterals(ruleSet), Is.All.Matches<string>(literal => storedUrl.Contains(literal, StringComparison.OrdinalIgnoreCase)));
        });
    }


    private static void AssertMatch(string? url, UrlMatchRule rule, bool expected)
    {
        var ruleSet = new UrlMatchRuleSet([rule]);
        Assert.That(ruleSet.Validate(), Is.Null, "Test rule must be valid.");

        Assert.That(UrlMatcher.Create(ruleSet).Evaluate(url).IsMatch, Is.EqualTo(expected), $"{url} | {rule}");
    }
}
