namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Text match operators of the <see cref="ActivityFrequencyCondition"/> detail filters. Persisted in process properties by
/// name – never rename members.
/// </summary>
public enum TextMatchOperator
{
    /// <summary>Column contains the text.</summary>
    Contains,

    /// <summary>Column doesn't contain the text; empty (NULL) columns match.</summary>
    DoesNotContain,

    /// <summary>Column equals the text.</summary>
    Equal,

    /// <summary>Column differs from the text; empty (NULL) columns match.</summary>
    NotEqual,

    /// <summary>Column starts with the text.</summary>
    StartsWith,

    /// <summary>Column ends with the text.</summary>
    EndsWith
}
