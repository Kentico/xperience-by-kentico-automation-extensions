using System.Reflection;

using CMS.Automation;
using CMS.Automation.Internal;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Trigger known to the application (built-in or registered with <c>RegisterAutomationTrigger</c>).
/// </summary>
/// <param name="Identifier">Trigger identifier (<see cref="AutomationProcessContext.GetTriggerIdentifier"/>).</param>
/// <param name="DisplayName">Display name or resource key.</param>
/// <param name="DataType">Trigger data type, or <c>null</c> when the trigger provides no data.</param>
internal sealed record RegisteredTrigger(string Identifier, string DisplayName, Type? DataType)
{
    /// <summary>
    /// Readable data properties (<see cref="TriggerDataProperties.Get"/>); empty without <see cref="DataType"/>.
    /// </summary>
    public IReadOnlyList<PropertyInfo> DataProperties => DataType is null ? [] : TriggerDataProperties.Get(DataType);


    /// <summary>
    /// Returns the data property with the given name (exact, case-sensitive), or <c>null</c>.
    /// </summary>
    public PropertyInfo? FindDataProperty(string? name) =>
        DataProperties.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.Ordinal));


    /// <summary>
    /// Returns <c>true</c> when <paramref name="triggerData"/> is an instance of <see cref="DataType"/>.
    /// </summary>
    public bool IsDataOfTrigger(object? triggerData) => DataType is not null && DataType.IsInstanceOfType(triggerData);
}


/// <summary>
/// Triggers of the application: built-in triggers (not in the custom trigger store) and every trigger registered with
/// <c>RegisterAutomationTrigger</c> (library, project, and other packages). Used by <c>Started by trigger</c>,
/// <c>{triggerData.X}</c> tokens of <c>Set contact field</c>, and Write to log trigger details.
/// </summary>
internal static class RegisteredTriggers
{
    /// <summary>
    /// Built-in triggers. They aren't registered in the custom trigger store (verified in 31.9.1).
    /// </summary>
    public static IReadOnlyList<RegisteredTrigger> BuiltIn { get; } =
    [
        new(DefaultTriggerIdentifiers.FORM_SUBMISSION, "labs.automationextensions.startedbytrigger.trigger.formsubmission", typeof(FormSubmissionTriggerData)),
        new(DefaultTriggerIdentifiers.REGISTRATION, "labs.automationextensions.startedbytrigger.trigger.registration", typeof(RegistrationTriggerData)),
        new(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, "labs.automationextensions.startedbytrigger.trigger.customactivity", typeof(CustomActivityTriggerData))
    ];


    /// <summary>
    /// Returns the built-in triggers followed by the registered custom triggers (built-in identifiers win on duplicates).
    /// </summary>
    public static IReadOnlyList<RegisteredTrigger> GetAll(ICustomAutomationTriggerProvider triggerProvider) => BuiltIn
        .Concat(triggerProvider.GetAll()
            .Where(definition => !BuiltIn.Any(builtIn => string.Equals(builtIn.Identifier, definition.Identifier, StringComparison.Ordinal)))
            .Select(definition => new RegisteredTrigger(definition.Identifier, definition.DisplayName, definition.TriggerDataType)))
        .ToList();


    /// <summary>
    /// Returns the trigger with the given identifier (exact, case-sensitive like the platform's <c>TriggeredBy…</c>
    /// checks), or <c>null</c> when it isn't registered.
    /// </summary>
    public static RegisteredTrigger? Find(ICustomAutomationTriggerProvider triggerProvider, string? identifier) =>
        string.IsNullOrEmpty(identifier)
            ? null
            : GetAll(triggerProvider).FirstOrDefault(trigger => string.Equals(trigger.Identifier, identifier, StringComparison.Ordinal));
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
