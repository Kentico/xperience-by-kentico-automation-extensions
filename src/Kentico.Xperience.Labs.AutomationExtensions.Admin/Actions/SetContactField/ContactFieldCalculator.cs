using System.Globalization;
using System.Numerics;

using CMS.DataEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Arithmetic of the <see cref="SetContactFieldMode.Calculate"/> mode: <c>current value &lt;operator&gt; operand</c>
/// in the field's number type. Empty current values count as 0. Integer fields use integer division (truncated toward
/// zero). Overflow and division by zero fail instead of saturating.
/// </summary>
internal static class ContactFieldCalculator
{
    public const string DIVISION_BY_ZERO = "division by zero";
    public const string OVERFLOW = "the result is outside the range of the field type";


    /// <summary>
    /// Calculates the new value. <paramref name="operandText"/> is parsed culture-invariant in the field's number type.
    /// </summary>
    public static CalculationResult Calculate(object? current, ArithmeticOperator arithmeticOperator, string operandText, ContactField field) =>
        field.DataType.ToLowerInvariant() switch
        {
            FieldDataType.Integer => Calculate<int>(current, arithmeticOperator, operandText, NumberStyles.Integer, "a whole number in the field's range"),
            FieldDataType.LongInteger => Calculate<long>(current, arithmeticOperator, operandText, NumberStyles.Integer, "a whole number in the field's range"),
            FieldDataType.Decimal => Calculate<decimal>(current, arithmeticOperator, operandText, NumberStyles.Float, "a number"),
            FieldDataType.Double => Calculate<double>(current, arithmeticOperator, operandText, NumberStyles.Float, "a number"),
            _ => CalculationResult.Failure($"field type '{field.DataType}' isn't a number type")
        };


    /// <summary>
    /// Applies the operator with checked arithmetic.
    /// </summary>
    internal static CalculationResult Apply<T>(T current, ArithmeticOperator arithmeticOperator, T operand)
        where T : INumber<T>
    {
        if (arithmeticOperator is ArithmeticOperator.Divide or ArithmeticOperator.Modulo && T.IsZero(operand))
        {
            return CalculationResult.Failure(DIVISION_BY_ZERO);
        }

        try
        {
            var result = arithmeticOperator switch
            {
                ArithmeticOperator.Add => checked(current + operand),
                ArithmeticOperator.Subtract => checked(current - operand),
                ArithmeticOperator.Multiply => checked(current * operand),
                ArithmeticOperator.Divide => checked(current / operand),
                ArithmeticOperator.Modulo => current % operand,
                _ => throw new ArgumentOutOfRangeException(nameof(arithmeticOperator), arithmeticOperator, "Unknown operator.")
            };

            return T.IsFinite(result) ? CalculationResult.Success(result) : CalculationResult.Failure(OVERFLOW);
        }
        catch (OverflowException)
        {
            return CalculationResult.Failure(OVERFLOW);
        }
    }


    private static CalculationResult Calculate<T>(object? current, ArithmeticOperator arithmeticOperator, string operandText, NumberStyles styles, string expected)
        where T : INumber<T>
    {
        if (!T.TryParse(operandText.Trim(), styles, CultureInfo.InvariantCulture, out var operand) || !T.IsFinite(operand))
        {
            return CalculationResult.Failure($"the operand isn't {expected}");
        }

        if (!TryGetCurrent(current, out T currentValue))
        {
            return CalculationResult.Failure("the current value can't be read as the field type");
        }

        return Apply(currentValue, arithmeticOperator, operand);
    }


    private static bool TryGetCurrent<T>(object? current, out T value)
        where T : INumber<T>
    {
        value = T.Zero;

        if (current is null or DBNull)
        {
            return true;
        }

        try
        {
            value = (T)Convert.ChangeType(current, typeof(T), CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        {
            return false;
        }
    }
}


/// <summary>
/// Result of <see cref="ContactFieldCalculator.Calculate"/>.
/// </summary>
/// <param name="Succeeded">Whether a value was calculated.</param>
/// <param name="Value">Calculated value.</param>
/// <param name="Error">Reason the calculation failed, without values.</param>
internal readonly record struct CalculationResult(bool Succeeded, object? Value, string? Error)
{
    public static CalculationResult Success(object value) => new(true, value, null);


    public static CalculationResult Failure(string error) => new(false, null, error);
}
