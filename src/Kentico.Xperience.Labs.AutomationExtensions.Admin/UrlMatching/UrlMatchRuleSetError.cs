namespace Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

/// <summary>
/// Why stored URL rules can't be used.
/// </summary>
public enum UrlMatchRuleSetErrorCode
{
    /// <summary>Not a JSON object of the expected shape.</summary>
    InvalidJson,

    /// <summary>Missing or unknown <c>version</c>.</summary>
    UnsupportedVersion,

    /// <summary>More than <see cref="UrlMatchRuleSet.MAX_RULES"/> rules.</summary>
    TooManyRules,

    /// <summary>Unknown <c>part</c> name.</summary>
    UnknownPart,

    /// <summary>Unknown <c>operator</c> name.</summary>
    UnknownOperator,

    /// <summary>Operator not available for the part (<see cref="UrlMatchRuleSet.IsOperatorAllowed"/>).</summary>
    OperatorNotAllowed,

    /// <summary>Query parameter rule without a parameter name.</summary>
    MissingParameter,

    /// <summary>Operator needs a value, but it's empty.</summary>
    MissingValue,

    /// <summary>Value longer than <see cref="UrlMatchRuleSet.MAX_VALUE_LENGTH"/>.</summary>
    ValueTooLong,

    /// <summary>Parameter name longer than <see cref="UrlMatchRuleSet.MAX_PARAMETER_LENGTH"/>.</summary>
    ParameterTooLong
}


/// <summary>
/// Validation error of stored URL rules.
/// </summary>
/// <param name="Code">Error kind.</param>
/// <param name="RuleNumber">1-based number of the invalid rule; <c>null</c> for errors of the whole rule set.</param>
public sealed record UrlMatchRuleSetError(UrlMatchRuleSetErrorCode Code, int? RuleNumber = null)
{
    /// <summary>
    /// Returns an English description for logs (never contains rule values).
    /// </summary>
    public string Describe()
    {
        string reason = Code switch
        {
            UrlMatchRuleSetErrorCode.InvalidJson => "the stored rules aren't valid JSON of the expected shape",
            UrlMatchRuleSetErrorCode.UnsupportedVersion => "the rules version is missing or unsupported",
            UrlMatchRuleSetErrorCode.TooManyRules => $"more than {UrlMatchRuleSet.MAX_RULES} rules",
            UrlMatchRuleSetErrorCode.UnknownPart => "unknown URL part",
            UrlMatchRuleSetErrorCode.UnknownOperator => "unknown operator",
            UrlMatchRuleSetErrorCode.OperatorNotAllowed => "operator not available for the URL part",
            UrlMatchRuleSetErrorCode.MissingParameter => "query parameter name is empty",
            UrlMatchRuleSetErrorCode.MissingValue => "value is empty",
            UrlMatchRuleSetErrorCode.ValueTooLong => $"value is longer than {UrlMatchRuleSet.MAX_VALUE_LENGTH} characters",
            UrlMatchRuleSetErrorCode.ParameterTooLong => $"query parameter name is longer than {UrlMatchRuleSet.MAX_PARAMETER_LENGTH} characters",
            _ => Code.ToString()
        };

        return RuleNumber is { } number ? $"rule {number}: {reason}" : reason;
    }
}
