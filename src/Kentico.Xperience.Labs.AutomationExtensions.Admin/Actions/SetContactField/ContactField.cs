using CMS.DataEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Contact field available to the <see cref="SetContactFieldAction"/>.
/// </summary>
/// <param name="Name">Field (column) name.</param>
/// <param name="Caption">Localized field caption, or the name when the field has no caption.</param>
/// <param name="DataType">Field data type (<see cref="FieldDataType"/> constant).</param>
/// <param name="Size">Maximum text length (text fields only, 0 = unlimited).</param>
/// <param name="AllowEmpty">Whether the field accepts empty values.</param>
/// <param name="IsSystem">Whether the field is a built-in contact field.</param>
public sealed record ContactField(string Name, string Caption, string DataType, int Size, bool AllowEmpty, bool IsSystem)
{
    /// <summary>
    /// Whether the field holds a number (integer, long integer, decimal, or floating-point number).
    /// </summary>
    public bool IsNumber => IsDataType(FieldDataType.Integer)
        || IsDataType(FieldDataType.LongInteger)
        || IsDataType(FieldDataType.Decimal)
        || IsDataType(FieldDataType.Double);


    /// <summary>
    /// Whether the field holds text (text or long text).
    /// </summary>
    public bool IsText => IsDataType(FieldDataType.Text) || IsDataType(FieldDataType.LongText);


    /// <summary>
    /// Returns whether the field has the given <see cref="FieldDataType"/> (case-insensitive).
    /// </summary>
    public bool IsDataType(string dataType) => string.Equals(DataType, dataType, StringComparison.OrdinalIgnoreCase);
}
