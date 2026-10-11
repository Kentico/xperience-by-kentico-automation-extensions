using System.Reflection;

using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Reads public properties of known trigger data types for <c>{triggerData.&lt;Property&gt;}</c> tokens.
/// </summary>
internal static class TriggerDataTokens
{
    /// <summary>
    /// Trigger data types whose properties can be used in tokens: built-in form submission, registration, and custom
    /// activity triggers, and the library's date anniversary, inactivity, and contact group triggers.
    /// </summary>
    public static readonly IReadOnlyList<Type> KnownTypes =
    [
        typeof(FormSubmissionTriggerData),
        typeof(RegistrationTriggerData),
        typeof(CustomActivityTriggerData),
        typeof(ContactDateAnniversaryTriggerData),
        typeof(ContactInactiveTriggerData),
        typeof(ContactGroupMemberTriggerData)
    ];


    /// <summary>
    /// Returns the property names available for the given trigger data type (empty for unknown types).
    /// </summary>
    public static IReadOnlyList<string> GetPropertyNames(Type triggerDataType) =>
        GetProperties(triggerDataType).Select(property => property.Name).ToList();


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

        var property = GetProperties(triggerData.GetType())
            .FirstOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (property is null)
        {
            return false;
        }

        value = ContactFieldValueConverter.Format(property.GetValue(triggerData));
        return true;
    }


    private static IEnumerable<PropertyInfo> GetProperties(Type triggerDataType)
    {
        if (!KnownTypes.Contains(triggerDataType))
        {
            return [];
        }

        return triggerDataType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead
                && property.GetIndexParameters().Length == 0
                && property.Name != nameof(IAutomationTriggerData.Identifier));
    }
}
