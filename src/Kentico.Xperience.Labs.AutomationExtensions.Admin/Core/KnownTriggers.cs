using System.Reflection;

using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Trigger whose data is read by <c>{triggerData.X}</c> tokens and the Write to log trigger details.
/// </summary>
/// <param name="Identifier">Identifier of the trigger (<see cref="AutomationProcessContext.GetTriggerIdentifier"/>).</param>
/// <param name="DataType">Type of the trigger data.</param>
internal sealed record KnownTrigger(string Identifier, Type DataType);


/// <summary>
/// Triggers whose data the library reads in <c>{triggerData.X}</c> tokens and Write to log trigger details: built-in form
/// submission, registration, and custom activity triggers, and the library's own triggers.
/// </summary>
internal static class KnownTriggers
{
    public static readonly KnownTrigger FormSubmission = new(DefaultTriggerIdentifiers.FORM_SUBMISSION, typeof(FormSubmissionTriggerData));
    public static readonly KnownTrigger Registration = new(DefaultTriggerIdentifiers.REGISTRATION, typeof(RegistrationTriggerData));
    public static readonly KnownTrigger CustomActivity = new(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, typeof(CustomActivityTriggerData));
    public static readonly KnownTrigger ContactDateAnniversary = new(ContactDateAnniversaryTrigger.IDENTIFIER, typeof(ContactDateAnniversaryTriggerData));
    public static readonly KnownTrigger ContactInactive = new(ContactInactiveTrigger.IDENTIFIER, typeof(ContactInactiveTriggerData));
    public static readonly KnownTrigger ContactGroupMember = new(ContactGroupMemberTrigger.IDENTIFIER, typeof(ContactGroupMemberTriggerData));


    /// <summary>
    /// All known triggers.
    /// </summary>
    public static IReadOnlyList<KnownTrigger> All { get; } =
    [
        FormSubmission,
        Registration,
        CustomActivity,
        ContactDateAnniversary,
        ContactInactive,
        ContactGroupMember
    ];


    /// <summary>
    /// Trigger data types of <see cref="All"/>.
    /// </summary>
    public static IReadOnlyList<Type> DataTypes { get; } = All.Select(trigger => trigger.DataType).ToList();


    /// <summary>
    /// Returns the trigger with the given identifier (exact, case-sensitive like the platform's <c>TriggeredBy…</c>
    /// checks), or <c>null</c>.
    /// </summary>
    public static KnownTrigger? FindByIdentifier(string? identifier) =>
        All.FirstOrDefault(trigger => string.Equals(trigger.Identifier, identifier, StringComparison.Ordinal));


    /// <summary>
    /// Returns the data properties (<see cref="TriggerDataProperties.Get"/>) of a known trigger data type. Unknown types
    /// have no properties.
    /// </summary>
    public static IReadOnlyList<PropertyInfo> GetDataProperties(Type triggerDataType) =>
        DataTypes.Contains(triggerDataType) ? TriggerDataProperties.Get(triggerDataType) : [];
}


/// <summary>
/// Readable data properties of trigger data types.
/// </summary>
internal static class TriggerDataProperties
{
    /// <summary>
    /// Returns the public readable instance properties of <paramref name="triggerDataType"/> without indexers and
    /// <see cref="IAutomationTriggerData.Identifier"/>.
    /// </summary>
    public static IReadOnlyList<PropertyInfo> Get(Type triggerDataType) => triggerDataType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.CanRead
            && property.GetIndexParameters().Length == 0
            && property.Name != nameof(IAutomationTriggerData.Identifier))
        .ToList();
}
