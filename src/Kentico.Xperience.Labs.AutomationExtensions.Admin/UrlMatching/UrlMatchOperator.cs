namespace Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

/// <summary>
/// Operators of a <see cref="UrlMatchRule"/>. Persisted in rule JSON by name – never rename members.
/// All comparisons are case-insensitive. Negated operators are the exact negation of their positive counterpart, so they
/// also match URLs without the part (no host, missing query parameter, unrecognized URL).
/// </summary>
public enum UrlMatchOperator
{
    /// <summary>Part equals the value.</summary>
    Equal,

    /// <summary>Part differs from the value.</summary>
    NotEqual,

    /// <summary>Part contains the value.</summary>
    Contains,

    /// <summary>Part doesn't contain the value.</summary>
    DoesNotContain,

    /// <summary>Part starts with the value.</summary>
    StartsWith,

    /// <summary>Part ends with the value.</summary>
    EndsWith,

    /// <summary>Part matches the value as a whole, where <c>*</c> stands for any characters (including <c>/</c>).</summary>
    Matches,

    /// <summary>Query parameter is present (with any or an empty value). <see cref="UrlPart.QueryParameter"/> only.</summary>
    Exists,

    /// <summary>Query parameter isn't present. <see cref="UrlPart.QueryParameter"/> only.</summary>
    DoesNotExist
}
