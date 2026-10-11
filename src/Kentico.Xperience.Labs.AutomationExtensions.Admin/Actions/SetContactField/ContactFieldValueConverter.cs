using System.Globalization;

using CMS.DataEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Converts resolved text to contact field values and field values to token text. All formats are culture-invariant.
/// Error messages never contain the value (it may be personal data).
/// </summary>
internal static class ContactFieldValueConverter
{
    /// <summary>
    /// Format of date values (<c>2026-10-11</c>).
    /// </summary>
    public const string DATE_FORMAT = "yyyy-MM-dd";

    /// <summary>
    /// Format of date and time values (<c>2026-10-11T14:30:00</c>).
    /// </summary>
    public const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";


    private static readonly string[] dateFormats =
    [
        DATE_FORMAT,
        "yyyy-MM-ddTHH:mm",
        DATE_TIME_FORMAT,
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss"
    ];


    /// <summary>
    /// Returns whether the stored value counts as empty (<c>null</c>, <see cref="DBNull"/>, or empty text).
    /// </summary>
    public static bool IsEmpty(object? value) => value is null or DBNull || (value is string text && text.Length == 0);


    /// <summary>
    /// Formats a value for use in a template: numbers invariant, booleans <c>true</c>/<c>false</c>, dates ISO
    /// (<see cref="DATE_FORMAT"/> when <paramref name="dateOnly"/>, otherwise <see cref="DATE_TIME_FORMAT"/>), empty values as empty text.
    /// </summary>
    public static string Format(object? value, bool dateOnly = false) => value switch
    {
        null or DBNull => string.Empty,
        string text => text,
        bool flag => flag ? "true" : "false",
        DateTime dateTime => dateTime.ToString(dateOnly ? DATE_FORMAT : DATE_TIME_FORMAT, CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(DATE_TIME_FORMAT + "zzz", CultureInfo.InvariantCulture),
        Guid guid => guid.ToString("D"),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };


    /// <summary>
    /// Converts <paramref name="text"/> to a value of the field's data type. Empty text clears the field (<c>null</c>),
    /// or sets empty text for text fields that don't allow empty values. Text longer than the field size is truncated.
    /// </summary>
    public static ConversionResult Convert(string text, ContactField field)
    {
        if (field.IsText)
        {
            return ConvertText(text, field);
        }

        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return field.AllowEmpty
                ? ConversionResult.Success(null)
                : ConversionResult.Failure("the field requires a value but the resolved value is empty");
        }

        return field.DataType.ToLowerInvariant() switch
        {
            FieldDataType.Integer => int.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int integer)
                ? ConversionResult.Success(integer)
                : NotConvertible(field, "a whole number in the field's range"),
            FieldDataType.LongInteger => long.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long longInteger)
                ? ConversionResult.Success(longInteger)
                : NotConvertible(field, "a whole number in the field's range"),
            FieldDataType.Decimal => decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalNumber)
                ? ConversionResult.Success(decimalNumber)
                : NotConvertible(field, "a number"),
            FieldDataType.Double => double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleNumber) && double.IsFinite(doubleNumber)
                ? ConversionResult.Success(doubleNumber)
                : NotConvertible(field, "a number"),
            FieldDataType.Boolean => TryParseBoolean(trimmed, out bool flag)
                ? ConversionResult.Success(flag)
                : NotConvertible(field, "true, false, 1, or 0"),
            FieldDataType.Date => TryParseDate(trimmed, out var date)
                ? ConversionResult.Success(date.Date)
                : NotConvertible(field, "an ISO date (yyyy-MM-dd)"),
            FieldDataType.DateTime => TryParseDate(trimmed, out var dateTime)
                ? ConversionResult.Success(dateTime)
                : NotConvertible(field, "an ISO date and time (yyyy-MM-ddTHH:mm:ss)"),
            _ => ConversionResult.Failure($"data type '{field.DataType}' isn't supported")
        };
    }


    /// <summary>
    /// Parses <c>true</c>/<c>false</c> (case-insensitive) and <c>1</c>/<c>0</c>.
    /// </summary>
    public static bool TryParseBoolean(string text, out bool value)
    {
        value = false;

        if (text == "1" || string.Equals(text, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        return text == "0" || string.Equals(text, bool.FalseString, StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Parses ISO dates with optional time (<c>T</c> or space separator), without time zone offset.
    /// </summary>
    public static bool TryParseDate(string text, out DateTime value) =>
        DateTime.TryParseExact(text, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);


    private static ConversionResult ConvertText(string text, ContactField field)
    {
        if (text.Length == 0)
        {
            return ConversionResult.Success(field.AllowEmpty ? null : string.Empty);
        }

        if (field.IsDataType(FieldDataType.Text) && field.Size > 0 && text.Length > field.Size)
        {
            int length = char.IsHighSurrogate(text[field.Size - 1]) ? field.Size - 1 : field.Size;

            return ConversionResult.Success(text[..length], truncated: true);
        }

        return ConversionResult.Success(text);
    }


    private static ConversionResult NotConvertible(ContactField field, string expected) =>
        ConversionResult.Failure($"the resolved value can't be converted to the field type '{field.DataType}' (expected {expected})");
}


/// <summary>
/// Result of <see cref="ContactFieldValueConverter.Convert"/>.
/// </summary>
/// <param name="Succeeded">Whether the text was converted.</param>
/// <param name="Value">Converted value (<c>null</c> clears the field).</param>
/// <param name="Truncated">Whether text was shortened to the field size.</param>
/// <param name="Error">Reason the conversion failed, without the value.</param>
internal readonly record struct ConversionResult(bool Succeeded, object? Value, bool Truncated, string? Error)
{
    public static ConversionResult Success(object? value, bool truncated = false) => new(true, value, truncated, null);


    public static ConversionResult Failure(string error) => new(false, null, false, error);
}
