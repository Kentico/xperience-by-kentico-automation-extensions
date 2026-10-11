using System.Reflection;

using CMS.Automation;
using CMS.Automation.Internal;

using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Trigger selectable in <see cref="StartedByTriggerProperties.Trigger"/>.
/// </summary>
/// <param name="Identifier">Trigger identifier – the persisted value.</param>
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
}


/// <summary>
/// Triggers of the application: built-in triggers (not in the custom trigger store) and every trigger registered with
/// <c>RegisterAutomationTrigger</c> (library, project, and other packages).
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
    /// Returns the trigger with the given identifier (exact, case-sensitive), or <c>null</c> when it isn't registered.
    /// </summary>
    public static RegisteredTrigger? Find(ICustomAutomationTriggerProvider triggerProvider, string? identifier) =>
        string.IsNullOrEmpty(identifier)
            ? null
            : GetAll(triggerProvider).FirstOrDefault(trigger => string.Equals(trigger.Identifier, identifier, StringComparison.Ordinal));
}
