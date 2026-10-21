using System.Text;
using System.Text.RegularExpressions;

namespace Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

/// <summary>
/// Matches URLs against a validated <see cref="UrlMatchRuleSet"/>. Prepare once, then match many URLs (e.g. all distinct
/// URLs of a contact) – values are normalized and wildcard patterns compiled only once.
/// </summary>
/// <remarks>
/// Rule values are normalized like the URL parts they're compared with (<see cref="NormalizedUrl"/>):
/// <list type="bullet">
/// <item>Path: percent-decoded; a missing leading <c>/</c> is added for <see cref="UrlMatchOperator.Equal"/>, <see cref="UrlMatchOperator.NotEqual"/>,
/// <see cref="UrlMatchOperator.StartsWith"/>, <see cref="UrlMatchOperator.Matches"/>; a trailing slash is ignored for
/// <see cref="UrlMatchOperator.Equal"/>, <see cref="UrlMatchOperator.NotEqual"/>, <see cref="UrlMatchOperator.Matches"/>.</item>
/// <item>Query parameter name and value: percent-decoded.</item>
/// <item>Url with <see cref="UrlMatchOperator.Equal"/> / <see cref="UrlMatchOperator.NotEqual"/>: normalized as a URL when it is one.</item>
/// </list>
/// </remarks>
internal sealed class UrlMatcher
{
    /// <summary>
    /// Maximum time one wildcard comparison may take; longer comparisons count as "no match".
    /// </summary>
    public static readonly TimeSpan DefaultMatchTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Minimum length of a literal used for the database prefilter (<see cref="GetPrefilterLiterals"/>).
    /// </summary>
    public const int MIN_PREFILTER_LITERAL_LENGTH = 3;


    private readonly IReadOnlyList<PreparedRule> rules;


    private UrlMatcher(IReadOnlyList<PreparedRule> rules) => this.rules = rules;


    /// <summary>
    /// Prepares <paramref name="ruleSet"/> (must be valid, see <see cref="UrlMatchRuleSet.Validate"/>) for matching.
    /// </summary>
    public static UrlMatcher Create(UrlMatchRuleSet ruleSet, TimeSpan? matchTimeout = null)
    {
        var timeout = matchTimeout ?? DefaultMatchTimeout;

        return new UrlMatcher(ruleSet.Rules.Select(rule => PreparedRule.Prepare(rule, timeout)).ToList());
    }


    /// <summary>
    /// Matches <paramref name="url"/> against all rules (AND). No rules = every URL matches.
    /// </summary>
    public UrlMatchEvaluation Evaluate(string? url) => Evaluate(NormalizedUrl.Parse(url));


    /// <inheritdoc cref="Evaluate(string?)"/>
    public UrlMatchEvaluation Evaluate(NormalizedUrl url)
    {
        bool timedOut = false;
        bool[] results = new bool[rules.Count];
        for (int index = 0; index < rules.Count; index++)
        {
            results[index] = rules[index].IsMatch(url, ref timedOut);
        }

        return new UrlMatchEvaluation(results.All(result => result), results, timedOut);
    }


    /// <summary>
    /// Returns, per positive rule, its longest literal that every matching stored URL contains verbatim, for a
    /// case-insensitive database <c>LIKE '%literal%'</c> prefilter. Negated rules and rules without a literal of at least
    /// <see cref="MIN_PREFILTER_LITERAL_LENGTH"/> characters give none.
    /// </summary>
    /// <remarks>
    /// Literals contain only unreserved URL characters (letters, digits, <c>- . _ ~</c>). Normalization never changes
    /// them – it only removes characters between them (fragment, default port, trailing slash), decodes escapes (which
    /// end a literal), and lowercases the host – so a stored URL whose normalized part matches the rule still contains
    /// the literal. Assumes a case-insensitive collation (Xperience default) and that clients don't percent-encode unreserved
    /// characters (RFC 3986 says they shouldn't).
    /// </remarks>
    public static IReadOnlyList<string> GetPrefilterLiterals(UrlMatchRuleSet ruleSet) => ruleSet.Rules
        .Where(rule => rule.IsPositive)
        .Select(rule => GetLongestLiteral(PreparedRule.Prepare(rule, DefaultMatchTimeout).Texts))
        .Where(literal => literal is not null)
        .Select(literal => literal!)
        .ToList();


    private static string? GetLongestLiteral(IEnumerable<string> texts)
    {
        string? longest = null;
        foreach (string text in texts)
        {
            int start = -1;
            for (int index = 0; index <= text.Length; index++)
            {
                bool literalChar = index < text.Length && IsUnreserved(text[index]);
                if (literalChar && start < 0)
                {
                    start = index;
                }
                else if (!literalChar && start >= 0)
                {
                    int length = index - start;
                    if (length >= MIN_PREFILTER_LITERAL_LENGTH && length > (longest?.Length ?? 0))
                    {
                        longest = text[start..index];
                    }

                    start = -1;
                }
            }
        }

        return longest;
    }


    private static bool IsUnreserved(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~';


    /// <summary>
    /// Rule with its value normalized for comparison.
    /// </summary>
    private sealed class PreparedRule
    {
        private readonly UrlMatchRule rule;
        private readonly string value;
        private readonly string? parameter;
        private readonly Regex? pattern;


        /// <summary>Normalized value and parameter name, for prefilter literals.</summary>
        public IEnumerable<string> Texts => parameter is null ? [value] : [value, parameter];


        private PreparedRule(UrlMatchRule rule, string value, string? parameter, Regex? pattern)
        {
            this.rule = rule;
            this.value = value;
            this.parameter = parameter;
            this.pattern = pattern;
        }


        public static PreparedRule Prepare(UrlMatchRule rule, TimeSpan timeout)
        {
            string value = NormalizeValue(rule);
            string? parameter = rule.Part == UrlPart.QueryParameter && rule.Parameter is not null
                ? Uri.UnescapeDataString(rule.Parameter.Trim())
                : null;
            var pattern = rule.Operator == UrlMatchOperator.Matches ? CreateWildcardPattern(value, timeout) : null;

            return new PreparedRule(rule, value, parameter, pattern);
        }


        public bool IsMatch(NormalizedUrl url, ref bool timedOut) => rule.Operator switch
        {
            UrlMatchOperator.Exists => GetValues(url).Any(),
            UrlMatchOperator.DoesNotExist => !GetValues(url).Any(),
            UrlMatchOperator.NotEqual or UrlMatchOperator.DoesNotContain => !AnyMatches(url, ref timedOut),
            UrlMatchOperator.Equal
                or UrlMatchOperator.Contains
                or UrlMatchOperator.StartsWith
                or UrlMatchOperator.EndsWith
                or UrlMatchOperator.Matches => AnyMatches(url, ref timedOut),
            _ => false
        };


        /// <summary>
        /// Returns whether any value of the part satisfies the positive form of the operator. A missing part has no values.
        /// </summary>
        private bool AnyMatches(NormalizedUrl url, ref bool timedOut)
        {
            foreach (string candidate in GetValues(url))
            {
                if (IsPositiveMatch(candidate, ref timedOut))
                {
                    return true;
                }
            }

            return false;
        }


        private bool IsPositiveMatch(string candidate, ref bool timedOut)
        {
            switch (rule.Operator)
            {
                case UrlMatchOperator.Equal:
                case UrlMatchOperator.NotEqual:
                    return string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase);
                case UrlMatchOperator.Contains:
                case UrlMatchOperator.DoesNotContain:
                    return candidate.Contains(value, StringComparison.OrdinalIgnoreCase);
                case UrlMatchOperator.StartsWith:
                    return candidate.StartsWith(value, StringComparison.OrdinalIgnoreCase);
                case UrlMatchOperator.EndsWith:
                    return candidate.EndsWith(value, StringComparison.OrdinalIgnoreCase);
                case UrlMatchOperator.Matches:
                    try
                    {
                        return pattern!.IsMatch(candidate);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        timedOut = true;
                        return false;
                    }

                case UrlMatchOperator.Exists:
                case UrlMatchOperator.DoesNotExist:
                default:
                    return false;
            }
        }


        private IEnumerable<string> GetValues(NormalizedUrl url)
        {
            if (!url.IsRecognized)
            {
                return [];
            }

            string? single = rule.Part switch
            {
                UrlPart.Url => url.Url,
                UrlPart.Host => url.Host,
                UrlPart.Path => url.Path,
                UrlPart.QueryParameter => null,
                _ => null
            };

            if (rule.Part != UrlPart.QueryParameter)
            {
                return single is null ? [] : [single];
            }

            string name = parameter ?? string.Empty;

            return url.QueryParameters
                .Where(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Value);
        }


        private static string NormalizeValue(UrlMatchRule rule)
        {
            string value = rule.Value?.Trim() ?? string.Empty;

            switch (rule.Part)
            {
                case UrlPart.Path:
                    value = Uri.UnescapeDataString(value);
                    if (rule.Operator is UrlMatchOperator.Equal or UrlMatchOperator.NotEqual or UrlMatchOperator.StartsWith or UrlMatchOperator.Matches
                        && !value.StartsWith('/') && !value.StartsWith('*'))
                    {
                        value = "/" + value;
                    }

                    return rule.Operator is UrlMatchOperator.Equal or UrlMatchOperator.NotEqual or UrlMatchOperator.Matches
                        ? NormalizedUrl.NormalizePath(value)
                        : value;
                case UrlPart.QueryParameter:
                    return Uri.UnescapeDataString(value);
                case UrlPart.Url when rule.Operator is UrlMatchOperator.Equal or UrlMatchOperator.NotEqual:
                    var url = NormalizedUrl.Parse(value);
                    return url.IsRecognized ? url.Url! : value;
                case UrlPart.Url:
                case UrlPart.Host:
                default:
                    return value;
            }
        }


        /// <summary>
        /// Builds an anchored regular expression from a wildcard value: <c>*</c> = any characters (including <c>/</c>),
        /// everything else literal.
        /// </summary>
        private static Regex CreateWildcardPattern(string value, TimeSpan timeout)
        {
            var builder = new StringBuilder("^");
            builder.AppendJoin(".*", value.Split('*').Select(Regex.Escape));
            builder.Append('$');

            return new Regex(builder.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline, timeout);
        }
    }
}


/// <summary>
/// Result of matching one URL.
/// </summary>
/// <param name="IsMatch">Whether all rules match.</param>
/// <param name="RuleResults">Result per rule, in rule order.</param>
/// <param name="TimedOut">Whether a wildcard comparison exceeded its time limit (and counted as "no match").</param>
internal sealed record UrlMatchEvaluation(bool IsMatch, IReadOnlyList<bool> RuleResults, bool TimedOut);
