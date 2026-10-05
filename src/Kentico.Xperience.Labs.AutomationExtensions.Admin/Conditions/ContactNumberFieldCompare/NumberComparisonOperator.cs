namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;

/// <summary>
/// Operators of <see cref="ContactNumberFieldCompareCondition"/>. Persisted in process properties by name – never rename members.
/// </summary>
public enum NumberComparisonOperator
{
    /// <summary>Field value equals the compared value.</summary>
    Equal,

    /// <summary>Field value differs from the compared value.</summary>
    NotEqual,

    /// <summary>Field value is greater than the compared value.</summary>
    GreaterThan,

    /// <summary>Field value is greater than or equal to the compared value.</summary>
    GreaterThanOrEqual,

    /// <summary>Field value is less than the compared value.</summary>
    LessThan,

    /// <summary>Field value is less than or equal to the compared value.</summary>
    LessThanOrEqual
}
