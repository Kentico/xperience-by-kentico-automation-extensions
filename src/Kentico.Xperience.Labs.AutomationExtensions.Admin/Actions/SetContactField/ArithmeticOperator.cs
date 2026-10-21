namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Operators of the <see cref="SetContactFieldMode.Calculate"/> mode. Persisted in process properties by name – never rename members.
/// </summary>
public enum ArithmeticOperator
{
    /// <summary>Current value plus operand.</summary>
    Add,

    /// <summary>Current value minus operand.</summary>
    Subtract,

    /// <summary>Current value times operand.</summary>
    Multiply,

    /// <summary>Current value divided by operand. Integer fields truncate toward zero.</summary>
    Divide,

    /// <summary>Remainder of the current value divided by operand.</summary>
    Modulo
}
