using System.Globalization;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Score math and property validation of <see cref="AdjustContactScoreAction"/>.
/// </summary>
internal static class ContactScoreCalculator
{
    /// <summary>
    /// Returns the new score. Results outside the <see cref="int"/> range saturate instead of overflowing, then
    /// <paramref name="limitRange"/> clamps them to [<paramref name="minimum"/>, <paramref name="maximum"/>].
    /// </summary>
    public static int Calculate(int current, ContactScoreOperation operation, int amount, bool limitRange, int minimum, int maximum)
    {
        long value = operation switch
        {
            ContactScoreOperation.Add => (long)current + amount,
            ContactScoreOperation.Subtract => (long)current - amount,
            ContactScoreOperation.Set => amount,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation.")
        };

        value = Math.Clamp(value, int.MinValue, int.MaxValue);

        return limitRange ? (int)Math.Clamp(value, minimum, maximum) : (int)value;
    }


    /// <summary>
    /// Converts a stored field value to a score. <c>null</c> when the field is empty (callers treat it as 0).
    /// </summary>
    public static int? GetStoredValue(object? value) =>
        value is null or DBNull ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);


    /// <summary>
    /// Parses an operation persisted by name (exact, case-sensitive). Numeric values are rejected.
    /// </summary>
    public static bool TryParseOperation(string? value, out ContactScoreOperation operation)
    {
        operation = default;

        return value is not null
            && Enum.GetNames<ContactScoreOperation>().Contains(value, StringComparer.Ordinal)
            && Enum.TryParse(value, out operation);
    }


    /// <summary>
    /// Returns why the properties can't be applied, or <c>null</c> when they are valid.
    /// </summary>
    /// <param name="properties">Step properties.</param>
    /// <param name="fieldIsEligible">Whether <see cref="AdjustContactScoreActionProperties.Field"/> is an existing eligible integer field.</param>
    /// <param name="operation">Parsed operation when valid.</param>
    public static string? GetConfigurationError(AdjustContactScoreActionProperties properties, bool fieldIsEligible, out ContactScoreOperation operation)
    {
        operation = default;

        if (string.IsNullOrWhiteSpace(properties.Field))
        {
            return "no field is selected";
        }

        if (!fieldIsEligible)
        {
            return $"field '{properties.Field}' doesn't exist or isn't an integer custom contact field";
        }

        if (!TryParseOperation(properties.Operation, out operation))
        {
            return $"operation '{properties.Operation}' is unknown";
        }

        if (properties.Amount < 0)
        {
            return $"amount {properties.Amount} is negative";
        }

        if (properties.LimitRange && properties.Minimum > properties.Maximum)
        {
            return $"minimum {properties.Minimum} is greater than maximum {properties.Maximum}";
        }

        return null;
    }
}
