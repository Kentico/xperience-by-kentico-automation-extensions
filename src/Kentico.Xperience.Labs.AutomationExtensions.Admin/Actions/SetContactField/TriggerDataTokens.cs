using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Reads public properties of known trigger data types for <c>{triggerData.&lt;Property&gt;}</c> tokens.
/// </summary>
internal static class TriggerDataTokens
{
    /// <summary>
    /// Trigger data types whose properties can be used in tokens: built-in form submission, registration, and custom
    /// activity triggers, and the library's date anniversary, inactivity, and contact group triggers (<see cref="KnownTriggers"/>).
    /// </summary>
    public static IReadOnlyList<Type> KnownTypes => KnownTriggers.DataTypes;


    /// <summary>
    /// Returns the property names available for the given trigger data type (empty for unknown types).
    /// </summary>
    public static IReadOnlyList<string> GetPropertyNames(Type triggerDataType) =>
        KnownTriggers.GetDataProperties(triggerDataType).Select(property => property.Name).ToList();


    /// <summary>
    /// Returns <c>true</c> and the formatted value when <paramref name="triggerData"/> is a known type with a readable public
    /// property named <paramref name="propertyName"/> (case-insensitive). Empty property values are returned as empty text.
    /// </summary>
    public static bool TryGetValue(object? triggerData, string propertyName, out string value)
    {
        value = string.Empty;

        if (triggerData is null)
        {
            return false;
        }

        var property = KnownTriggers.GetDataProperties(triggerData.GetType())
            .FirstOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (property is null)
        {
            return false;
        }

        value = ContactFieldValueConverter.Format(property.GetValue(triggerData));
        return true;
    }
}
