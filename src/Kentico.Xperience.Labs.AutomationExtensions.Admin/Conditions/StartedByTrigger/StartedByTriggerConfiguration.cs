using System.Reflection;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Validated settings of <see cref="StartedByTriggerCondition"/>.
/// </summary>
/// <param name="TriggerIdentifier">Selected trigger identifier.</param>
/// <param name="DataType">Trigger data type when trigger data is checked.</param>
/// <param name="Property">Trigger data property to compare, or <c>null</c> when trigger data isn't checked.</param>
/// <param name="Operator">Comparison operator (when <paramref name="Property"/> is set).</param>
/// <param name="Value">Entered value (when <paramref name="Property"/> is set).</param>
internal sealed record StartedByTriggerConfiguration(
    string TriggerIdentifier,
    Type? DataType,
    PropertyInfo? Property,
    TriggerDataOperator Operator,
    string Value)
{
    /// <summary>
    /// How the property value is compared.
    /// </summary>
    public TriggerDataValueKind Kind => Property is null ? TriggerDataValueKind.Text : TriggerDataComparison.GetKind(Property.PropertyType);


    /// <summary>
    /// Returns whether the process was started by the configured trigger (exact identifier, like the platform's
    /// <c>TriggeredBy…</c> checks).
    /// </summary>
    public bool Matches(string? triggerIdentifier) => string.Equals(triggerIdentifier, TriggerIdentifier, StringComparison.Ordinal);


    /// <summary>
    /// Returns why the properties can't be evaluated, or <c>null</c> when they are valid. Reasons never contain the entered value.
    /// </summary>
    /// <param name="properties">Step properties.</param>
    /// <param name="trigger">Registered trigger with the selected identifier, or <c>null</c> when it isn't registered.
    /// Unregistered triggers still match by identifier, but their data can't be checked.</param>
    /// <param name="configuration">Validated settings when valid.</param>
    public static string? GetConfigurationError(StartedByTriggerProperties properties, RegisteredTrigger? trigger, out StartedByTriggerConfiguration? configuration)
    {
        configuration = null;

        if (string.IsNullOrWhiteSpace(properties.Trigger))
        {
            return "no trigger is selected";
        }

        if (!properties.CheckTriggerData)
        {
            configuration = new(properties.Trigger, null, null, default, string.Empty);
            return null;
        }

        if (trigger is null)
        {
            return $"trigger '{properties.Trigger}' isn't registered, so its data can't be checked";
        }

        if (trigger.DataType is null || trigger.DataProperties.Count == 0)
        {
            return $"trigger '{trigger.Identifier}' provides no trigger data";
        }

        if (string.IsNullOrWhiteSpace(properties.Field))
        {
            return "no trigger data field is selected";
        }

        var property = trigger.FindDataProperty(properties.Field);
        if (property is null)
        {
            return $"field '{properties.Field}' isn't a data property of trigger '{trigger.Identifier}'";
        }

        if (!TriggerDataComparison.TryParseOperator(properties.Operator, out var comparisonOperator))
        {
            return $"operator '{properties.Operator}' is unknown";
        }

        string? valueError = TriggerDataComparison.GetValueError(TriggerDataComparison.GetKind(property.PropertyType), comparisonOperator, properties.Value);
        if (valueError is not null)
        {
            return $"field '{property.Name}': {valueError}";
        }

        configuration = new(trigger.Identifier, trigger.DataType, property, comparisonOperator, properties.Value ?? string.Empty);
        return null;
    }
}
