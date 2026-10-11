namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Operators of <see cref="StartedByTriggerCondition"/>. Persisted in process properties by name – never rename members.
/// </summary>
public enum TriggerDataOperator
{
    /// <summary>Value equals the entered value (numbers and dates by value, text ignoring case).</summary>
    Equal,

    /// <summary>Value differs from the entered value.</summary>
    NotEqual,

    /// <summary>Value text contains the entered text (ignoring case).</summary>
    Contains,

    /// <summary>Value text doesn't contain the entered text (ignoring case).</summary>
    DoesNotContain,

    /// <summary>Value text starts with the entered text (ignoring case).</summary>
    StartsWith,

    /// <summary>Value text ends with the entered text (ignoring case).</summary>
    EndsWith,

    /// <summary>Number or date is greater (later) than the entered value.</summary>
    GreaterThan,

    /// <summary>Number or date is greater (later) than or equal to the entered value.</summary>
    GreaterThanOrEqual,

    /// <summary>Number or date is less (earlier) than the entered value.</summary>
    LessThan,

    /// <summary>Number or date is less (earlier) than or equal to the entered value.</summary>
    LessThanOrEqual,

    /// <summary>Value is missing or empty text.</summary>
    IsEmpty,

    /// <summary>Value is present and isn't empty text.</summary>
    IsNotEmpty
}
