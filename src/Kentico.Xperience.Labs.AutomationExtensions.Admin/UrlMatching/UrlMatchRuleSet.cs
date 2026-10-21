using System.Text;
using System.Text.Json;

namespace Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

/// <summary>
/// URL rules that must all match (AND). Stored as a versioned JSON string, e.g.
/// <c>{"version":1,"rules":[{"part":"Path","operator":"StartsWith","value":"/blog/","parameter":null}]}</c>.
/// An empty string or an empty rule list means "no filter".
/// </summary>
/// <remarks>
/// Independent of activities, so other steps can reuse it. Part and operator names are persisted – never rename
/// <see cref="UrlPart"/> or <see cref="UrlMatchOperator"/> members; change the shape only with a new <see cref="CURRENT_VERSION"/>.
/// </remarks>
public sealed class UrlMatchRuleSet
{
    public const int CURRENT_VERSION = 1;
    public const int MAX_RULES = 5;
    public const int MAX_VALUE_LENGTH = 400;
    public const int MAX_PARAMETER_LENGTH = 100;


    /// <summary>
    /// Rule set without rules (no filter).
    /// </summary>
    public static UrlMatchRuleSet Empty { get; } = new([]);


    /// <summary>
    /// Rules; all must match.
    /// </summary>
    public IReadOnlyList<UrlMatchRule> Rules { get; }


    /// <summary>
    /// Creates a rule set. Doesn't validate – use <see cref="Validate"/> or <see cref="TryParse"/>.
    /// </summary>
    public UrlMatchRuleSet(IReadOnlyList<UrlMatchRule> rules) => Rules = rules;


    /// <summary>
    /// Returns whether <paramref name="matchOperator"/> can be used with <paramref name="part"/>:
    /// <see cref="UrlMatchOperator.Exists"/> and <see cref="UrlMatchOperator.DoesNotExist"/> only with
    /// <see cref="UrlPart.QueryParameter"/>, all other operators with all parts.
    /// </summary>
    public static bool IsOperatorAllowed(UrlPart part, UrlMatchOperator matchOperator) =>
        matchOperator is not (UrlMatchOperator.Exists or UrlMatchOperator.DoesNotExist) || part == UrlPart.QueryParameter;


    /// <summary>
    /// Returns whether <paramref name="matchOperator"/> compares a value (all except <see cref="UrlMatchOperator.Exists"/>
    /// and <see cref="UrlMatchOperator.DoesNotExist"/>).
    /// </summary>
    public static bool RequiresValue(UrlMatchOperator matchOperator) =>
        matchOperator is not (UrlMatchOperator.Exists or UrlMatchOperator.DoesNotExist);


    /// <summary>
    /// Parses stored rules. Empty or whitespace <paramref name="json"/> is a valid empty rule set. Part and operator names
    /// must match exactly (case-sensitive, no numbers). Values and parameter names are trimmed.
    /// </summary>
    /// <returns><c>true</c> with <paramref name="ruleSet"/> when the rules are valid; otherwise <c>false</c> with <paramref name="error"/>.</returns>
    public static bool TryParse(string? json, out UrlMatchRuleSet ruleSet, out UrlMatchRuleSetError? error)
    {
        ruleSet = Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        List<UrlMatchRule> rules;
        try
        {
            using var document = JsonDocument.Parse(json);
            error = ReadRules(document.RootElement, out rules);
        }
        catch (JsonException)
        {
            error = new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.InvalidJson);
            return false;
        }

        if (error is not null)
        {
            return false;
        }

        var parsed = new UrlMatchRuleSet(rules);
        error = parsed.Validate();
        if (error is not null)
        {
            return false;
        }

        ruleSet = parsed;
        return true;
    }


    /// <summary>
    /// Returns the first validation error (rule count, operator per part, parameter name, value), or <c>null</c> when valid.
    /// </summary>
    public UrlMatchRuleSetError? Validate()
    {
        if (Rules.Count > MAX_RULES)
        {
            return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.TooManyRules);
        }

        for (int index = 0; index < Rules.Count; index++)
        {
            var code = GetRuleError(Rules[index]);
            if (code is not null)
            {
                return new UrlMatchRuleSetError(code.Value, index + 1);
            }
        }

        return null;
    }


    /// <summary>
    /// Serializes the rules in the stored format (current version). An empty rule set serializes to an empty string.
    /// </summary>
    public string ToJson()
    {
        if (Rules.Count == 0)
        {
            return string.Empty;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", CURRENT_VERSION);
            writer.WriteStartArray("rules");
            foreach (var rule in Rules)
            {
                writer.WriteStartObject();
                writer.WriteString("part", rule.Part.ToString());
                writer.WriteString("operator", rule.Operator.ToString());
                writer.WriteString("value", rule.Value);
                writer.WriteString("parameter", rule.Parameter);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }


    private static UrlMatchRuleSetErrorCode? GetRuleError(UrlMatchRule rule)
    {
        if (!Enum.IsDefined(rule.Part))
        {
            return UrlMatchRuleSetErrorCode.UnknownPart;
        }

        if (!Enum.IsDefined(rule.Operator))
        {
            return UrlMatchRuleSetErrorCode.UnknownOperator;
        }

        if (!IsOperatorAllowed(rule.Part, rule.Operator))
        {
            return UrlMatchRuleSetErrorCode.OperatorNotAllowed;
        }

        if (rule.Part == UrlPart.QueryParameter)
        {
            if (string.IsNullOrWhiteSpace(rule.Parameter))
            {
                return UrlMatchRuleSetErrorCode.MissingParameter;
            }

            if (rule.Parameter.Length > MAX_PARAMETER_LENGTH)
            {
                return UrlMatchRuleSetErrorCode.ParameterTooLong;
            }
        }

        if (RequiresValue(rule.Operator))
        {
            if (string.IsNullOrWhiteSpace(rule.Value))
            {
                return UrlMatchRuleSetErrorCode.MissingValue;
            }

            if (rule.Value.Length > MAX_VALUE_LENGTH)
            {
                return UrlMatchRuleSetErrorCode.ValueTooLong;
            }
        }

        return null;
    }


    private static UrlMatchRuleSetError? ReadRules(JsonElement root, out List<UrlMatchRule> rules)
    {
        rules = [];

        if (root.ValueKind != JsonValueKind.Object)
        {
            return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.InvalidJson);
        }

        if (!root.TryGetProperty("version", out var version)
            || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out int versionNumber)
            || versionNumber != CURRENT_VERSION)
        {
            return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.UnsupportedVersion);
        }

        if (!root.TryGetProperty("rules", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.InvalidJson);
        }

        if (items.GetArrayLength() > MAX_RULES)
        {
            return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.TooManyRules);
        }

        int number = 0;
        foreach (var item in items.EnumerateArray())
        {
            number++;
            if (item.ValueKind != JsonValueKind.Object
                || !TryGetOptionalString(item, "part", out string? partName)
                || !TryGetOptionalString(item, "operator", out string? operatorName)
                || !TryGetOptionalString(item, "value", out string? value)
                || !TryGetOptionalString(item, "parameter", out string? parameter))
            {
                return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.InvalidJson, number);
            }

            if (!TryParseName(partName, out UrlPart part))
            {
                return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.UnknownPart, number);
            }

            if (!TryParseName(operatorName, out UrlMatchOperator matchOperator))
            {
                return new UrlMatchRuleSetError(UrlMatchRuleSetErrorCode.UnknownOperator, number);
            }

            rules.Add(new UrlMatchRule(
                part,
                matchOperator,
                RequiresValue(matchOperator) ? value?.Trim() : null,
                part == UrlPart.QueryParameter ? parameter?.Trim() : null));
        }

        return null;
    }


    /// <summary>
    /// Reads a string property; a missing or <c>null</c> property gives <c>null</c>, any other kind fails.
    /// </summary>
    private static bool TryGetOptionalString(JsonElement item, string name, out string? value)
    {
        value = null;
        if (!item.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return true;
    }


    /// <summary>
    /// Parses an enum persisted by name (exact, case-sensitive). Numeric values are rejected.
    /// </summary>
    internal static bool TryParseName<TEnum>(string? name, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;

        return name is not null
            && Enum.GetNames<TEnum>().Contains(name, StringComparer.Ordinal)
            && Enum.TryParse(name, out value);
    }
}
