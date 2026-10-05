using CMS.Automation.Internal;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Admin;

internal sealed class StepRegistry
{
    private readonly IAutomationComponentStatus status;
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options;
    private readonly Func<IEnumerable<RegisteredAutomationComponent>> getRegistrations;


    public StepRegistry(
        IAutomationComponentStatus status,
        IOptionsMonitor<AutomationExtensionsOptions> options,
        ICustomAutomationTriggerProvider triggerProvider,
        ICustomAutomationActionProvider actionProvider,
        ICustomAutomationConditionProvider conditionProvider)
        : this(status, options, () => GetRegistrations(triggerProvider, actionProvider, conditionProvider))
    {
    }


    internal StepRegistry(
        IAutomationComponentStatus status,
        IOptionsMonitor<AutomationExtensionsOptions> options,
        Func<IEnumerable<RegisteredAutomationComponent>> getRegistrations)
    {
        this.status = status;
        this.options = options;
        this.getRegistrations = getRegistrations;
    }


    public IReadOnlyList<StepRegistryRow> GetRows() => BuildRows(getRegistrations(), options.CurrentValue, status);


    private static IEnumerable<RegisteredAutomationComponent> GetRegistrations(
        ICustomAutomationTriggerProvider triggerProvider,
        ICustomAutomationActionProvider actionProvider,
        ICustomAutomationConditionProvider conditionProvider) =>
        triggerProvider.GetAll()
            .Select(definition => new RegisteredAutomationComponent(definition.Identifier, definition.DisplayName, definition.Description, AutomationComponentKind.Trigger))
            .Concat(actionProvider.GetAll()
                .Select(definition => new RegisteredAutomationComponent(definition.Identifier, definition.DisplayName, definition.Description, AutomationComponentKind.Action)))
            .Concat(conditionProvider.GetAll()
                .Select(definition => new RegisteredAutomationComponent(definition.Identifier, definition.DisplayName, definition.Description, AutomationComponentKind.Condition)));


    internal static IReadOnlyList<StepRegistryRow> BuildRows(
        IEnumerable<RegisteredAutomationComponent> registrations,
        AutomationExtensionsOptions options,
        IAutomationComponentStatus status)
    {
        var registered = registrations.ToList();
        var rows = new List<StepRegistryRow>();

        foreach (var registration in registered)
        {
            var component = AutomationComponentCatalog.All.SingleOrDefault(component =>
                component.Identifier.Equals(registration.Identifier, StringComparison.OrdinalIgnoreCase)
                && component.Kind == registration.Kind);

            rows.Add(component is null
                ? new StepRegistryRow(
                    $"{registration.Kind}:{registration.Identifier}", registration.DisplayName, registration.Kind,
                    registration.Identifier, "notmanaged", registration.Description)
                : CreateLibraryRow(component, registration.DisplayName, registration.Description, true, status));
        }

        foreach (var component in AutomationComponentCatalog.All.Where(component =>
            !registered.Any(registration => registration.Identifier.Equals(component.Identifier, StringComparison.OrdinalIgnoreCase)
                && registration.Kind == component.Kind)))
        {
            rows.Add(CreateLibraryRow(component,
                $"labs.automationextensions.{component.Name.ToLowerInvariant()}.displayname", null, false, status));
        }

        foreach (string configuredKey in options.Components.Keys.Where(configuredKey =>
            !AutomationComponentCatalog.All.Any(component => component.Name.Equals(configuredKey, StringComparison.OrdinalIgnoreCase))))
        {
            rows.Add(new StepRegistryRow(
                $"configuration:{configuredKey}", configuredKey, null, null, "notapplicable", null));
        }

        return rows;
    }


    private static StepRegistryRow CreateLibraryRow(
        AutomationComponentDescriptor component,
        string displayName,
        string? description,
        bool registered,
        IAutomationComponentStatus status)
    {
        string componentStatus = "notapplicable";
        if (registered)
        {
            componentStatus = status.IsEnabled(component) ? "enabled" : "disabled";
        }

        return new StepRegistryRow(
            $"{component.Kind}:{component.Identifier}", displayName, component.Kind, component.Identifier,
            componentStatus, description);
    }
}
