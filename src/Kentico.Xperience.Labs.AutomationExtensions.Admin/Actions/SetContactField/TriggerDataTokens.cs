using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Reads trigger data properties for <c>{triggerData.&lt;Property&gt;}</c> tokens. Works for every trigger in
/// <see cref="RegisteredTriggers"/> (built-in and registered with <c>RegisterAutomationTrigger</c>) that provides data.
/// </summary>
internal static class TriggerDataTokens
{
    /// <summary>
    /// Returns <c>true</c> and the formatted value when <paramref name="triggerData"/> is data of the registered
    /// <paramref name="trigger"/> (instance of its data type) and the data type has a readable public property named
    /// <paramref name="propertyName"/> (case-insensitive, see <see cref="TriggerDataProperties.Get"/>). Empty property values
    /// are returned as empty text.
    /// </summary>
    /// <param name="trigger">Registered trigger that started the process, or <c>null</c> when it isn't registered.</param>
    /// <param name="triggerData">Trigger data of the process.</param>
    /// <param name="propertyName">Property name from the token.</param>
    /// <param name="value">Formatted value, or empty text.</param>
    public static bool TryGetValue(RegisteredTrigger? trigger, object? triggerData, string propertyName, out string value)
    {
        value = string.Empty;

        if (trigger is null || !trigger.IsDataOfTrigger(triggerData))
        {
            return false;
        }

        var property = trigger.DataProperties
            .FirstOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (property is null)
        {
            return false;
        }

        value = ContactFieldValueConverter.Format(property.GetValue(triggerData));
        return true;
    }
}
