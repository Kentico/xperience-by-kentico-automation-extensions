namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;

/// <summary>
/// Comparison and property validation of <see cref="ContactNumberFieldCompareCondition"/>.
/// </summary>
internal static class NumberComparison
{
    /// <summary>
    /// Returns whether <paramref name="current"/> relates to <paramref name="value"/> as <paramref name="comparisonOperator"/> says.
    /// </summary>
    public static bool Compare(int current, NumberComparisonOperator comparisonOperator, int value) => comparisonOperator switch
    {
        NumberComparisonOperator.Equal => current == value,
        NumberComparisonOperator.NotEqual => current != value,
        NumberComparisonOperator.GreaterThan => current > value,
        NumberComparisonOperator.GreaterThanOrEqual => current >= value,
        NumberComparisonOperator.LessThan => current < value,
        NumberComparisonOperator.LessThanOrEqual => current <= value,
        _ => throw new ArgumentOutOfRangeException(nameof(comparisonOperator), comparisonOperator, "Unknown operator.")
    };


    /// <summary>
    /// Parses an operator persisted by name (exact, case-sensitive). Numeric values are rejected.
    /// </summary>
    public static bool TryParseOperator(string? value, out NumberComparisonOperator comparisonOperator)
    {
        comparisonOperator = default;

        return value is not null
            && Enum.GetNames<NumberComparisonOperator>().Contains(value, StringComparer.Ordinal)
            && Enum.TryParse(value, out comparisonOperator);
    }


    /// <summary>
    /// Returns why the properties can't be evaluated, or <c>null</c> when they are valid.
    /// </summary>
    /// <param name="properties">Step properties.</param>
    /// <param name="fieldIsEligible">Whether <see cref="ContactNumberFieldCompareProperties.Field"/> is an existing eligible integer field.</param>
    /// <param name="comparisonOperator">Parsed operator when valid.</param>
    public static string? GetConfigurationError(ContactNumberFieldCompareProperties properties, bool fieldIsEligible, out NumberComparisonOperator comparisonOperator)
    {
        comparisonOperator = default;

        if (string.IsNullOrWhiteSpace(properties.Field))
        {
            return "no field is selected";
        }

        if (!fieldIsEligible)
        {
            return $"field '{properties.Field}' doesn't exist or isn't an integer custom contact field";
        }

        if (!TryParseOperator(properties.Operator, out comparisonOperator))
        {
            return $"operator '{properties.Operator}' is unknown";
        }

        return null;
    }
}
