using System.Globalization;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// How a trigger data value is compared.
/// </summary>
internal enum TriggerDataValueKind
{
    /// <summary>Formatted text, ignoring case (text, GUIDs, booleans, other types).</summary>
    Text,

    /// <summary>Numbers (<see cref="int"/>, <see cref="long"/>, <see cref="decimal"/>, <see cref="double"/>, …).</summary>
    Number,

    /// <summary><see cref="DateTime"/> values compared by date (time ignored).</summary>
    Date
}


/// <summary>
/// Validates and compares trigger data values of <see cref="StartedByTriggerCondition"/>. Error messages never contain the
/// entered value (it may be personal data).
/// </summary>
internal static class TriggerDataComparison
{
    /// <summary>
    /// Format of entered date values (<c>2026-10-11</c>).
    /// </summary>
    public const string DATE_FORMAT = ContactFieldValueConverter.DATE_FORMAT;


    private static readonly Type[] numberTypes =
    [
        typeof(byte),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal)
    ];


    /// <summary>
    /// Returns how values of the given property type are compared. Nullable types use their underlying type.
    /// </summary>
    public static TriggerDataValueKind GetKind(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(DateTime))
        {
            return TriggerDataValueKind.Date;
        }

        return numberTypes.Contains(underlying) ? TriggerDataValueKind.Number : TriggerDataValueKind.Text;
    }


    /// <summary>
    /// Parses an operator persisted by name (exact, case-sensitive). Numeric values are rejected.
    /// </summary>
    public static bool TryParseOperator(string? value, out TriggerDataOperator comparisonOperator)
    {
        comparisonOperator = default;

        return value is not null
            && Enum.GetNames<TriggerDataOperator>().Contains(value, StringComparer.Ordinal)
            && Enum.TryParse(value, out comparisonOperator);
    }


    /// <summary>
    /// Returns whether the operator checks for an empty value and ignores the entered value.
    /// </summary>
    public static bool IsEmptyCheck(TriggerDataOperator comparisonOperator) =>
        comparisonOperator is TriggerDataOperator.IsEmpty or TriggerDataOperator.IsNotEmpty;


    /// <summary>
    /// Returns why <paramref name="value"/> can't be compared with values of <paramref name="kind"/> using
    /// <paramref name="comparisonOperator"/>, or <c>null</c> when it can.
    /// </summary>
    public static string? GetValueError(TriggerDataValueKind kind, TriggerDataOperator comparisonOperator, string? value)
    {
        if (IsEmptyCheck(comparisonOperator))
        {
            return null;
        }

        string trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return "no value is entered";
        }

        bool byValue = IsOrderOperator(comparisonOperator) || comparisonOperator is TriggerDataOperator.Equal or TriggerDataOperator.NotEqual;
        if (!byValue)
        {
            return null;
        }

        return kind switch
        {
            TriggerDataValueKind.Number when !TryParseNumber(trimmed, out _) => "the value isn't a number (use a dot as decimal separator, e.g. 12.5)",
            TriggerDataValueKind.Date when !TryParseDate(trimmed, out _) => $"the value isn't a date in the format {DATE_FORMAT}",
            TriggerDataValueKind.Text when IsOrderOperator(comparisonOperator) => $"operator '{comparisonOperator}' can only compare numbers and dates",
            _ => null
        };
    }


    /// <summary>
    /// Compares <paramref name="actual"/> with <paramref name="value"/>. Call only when <see cref="GetValueError"/> returns
    /// <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Equality and order operators compare numbers and dates by value; text operators compare formatted text
    /// (<see cref="ContactFieldValueConverter.Format"/>), ignoring case. A missing value is never equal to, greater, or less
    /// than the entered value.
    /// </remarks>
    public static bool Compare(object? actual, TriggerDataValueKind kind, TriggerDataOperator comparisonOperator, string? value)
    {
        string text = ContactFieldValueConverter.Format(actual);
        string expected = value?.Trim() ?? string.Empty;

        return comparisonOperator switch
        {
            TriggerDataOperator.IsEmpty => string.IsNullOrWhiteSpace(text),
            TriggerDataOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(text),
            TriggerDataOperator.Contains => text.Contains(expected, StringComparison.OrdinalIgnoreCase),
            TriggerDataOperator.DoesNotContain => !text.Contains(expected, StringComparison.OrdinalIgnoreCase),
            TriggerDataOperator.StartsWith => text.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
            TriggerDataOperator.EndsWith => text.EndsWith(expected, StringComparison.OrdinalIgnoreCase),
            TriggerDataOperator.Equal => CompareValues(actual, kind, text, expected) == 0,
            TriggerDataOperator.NotEqual => CompareValues(actual, kind, text, expected) != 0,
            TriggerDataOperator.GreaterThan => CompareValues(actual, kind, text, expected) > 0,
            TriggerDataOperator.GreaterThanOrEqual => CompareValues(actual, kind, text, expected) >= 0,
            TriggerDataOperator.LessThan => CompareValues(actual, kind, text, expected) < 0,
            TriggerDataOperator.LessThanOrEqual => CompareValues(actual, kind, text, expected) <= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(comparisonOperator), comparisonOperator, "Unknown operator.")
        };
    }


    private static bool IsOrderOperator(TriggerDataOperator comparisonOperator) => comparisonOperator
        is TriggerDataOperator.GreaterThan
        or TriggerDataOperator.GreaterThanOrEqual
        or TriggerDataOperator.LessThan
        or TriggerDataOperator.LessThanOrEqual;


    /// <summary>
    /// Returns the sign of <c>actual - expected</c>, or <c>null</c> when the values can't be compared (missing value; text
    /// that differs, which only supports equality).
    /// </summary>
    private static int? CompareValues(object? actual, TriggerDataValueKind kind, string text, string expected) => kind switch
    {
        TriggerDataValueKind.Number => CompareNumber(actual, expected),
        TriggerDataValueKind.Date => CompareDate(actual, expected),
        TriggerDataValueKind.Text => string.Equals(text, expected, StringComparison.OrdinalIgnoreCase) ? 0 : null,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown value kind.")
    };


    private static int? CompareNumber(object? actual, string expected)
    {
        if (actual is null || !TryParseNumber(expected, out double number))
        {
            return null;
        }

        // Exact comparison where possible: decimals and long values can't be represented exactly as double.
        if (actual is decimal decimalValue && decimal.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal expectedDecimal))
        {
            return decimalValue.CompareTo(expectedDecimal);
        }

        if (actual is long longValue && long.TryParse(expected, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long expectedLong))
        {
            return longValue.CompareTo(expectedLong);
        }

        return Convert.ToDouble(actual, CultureInfo.InvariantCulture).CompareTo(number);
    }


    private static int? CompareDate(object? actual, string expected)
    {
        if (actual is not DateTime dateTime || !TryParseDate(expected, out var date))
        {
            return null;
        }

        return dateTime.Date.CompareTo(date.Date);
    }


    private static bool TryParseNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);


    private static bool TryParseDate(string text, out DateTime value) =>
        DateTime.TryParseExact(text, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
}
