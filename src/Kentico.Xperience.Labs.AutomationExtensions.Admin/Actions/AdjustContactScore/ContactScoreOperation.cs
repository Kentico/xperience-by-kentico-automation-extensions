namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Operations of <see cref="AdjustContactScoreAction"/>. Persisted in process properties by name – never rename members.
/// </summary>
public enum ContactScoreOperation
{
    /// <summary>Adds the amount to the current value.</summary>
    Add,

    /// <summary>Subtracts the amount from the current value.</summary>
    Subtract,

    /// <summary>Replaces the current value with the amount.</summary>
    Set
}
