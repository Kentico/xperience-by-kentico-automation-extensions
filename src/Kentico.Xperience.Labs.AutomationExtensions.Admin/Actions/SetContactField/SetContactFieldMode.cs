namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Modes of <see cref="SetContactFieldAction"/>. Persisted in process properties by name – never rename members.
/// </summary>
public enum SetContactFieldMode
{
    /// <summary>Replaces the field value with the resolved value template.</summary>
    SetValue,

    /// <summary>Calculates a new number from the current value, an operator, and an operand.</summary>
    Calculate
}
