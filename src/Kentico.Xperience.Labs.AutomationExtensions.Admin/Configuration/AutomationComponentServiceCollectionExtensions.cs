using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers library components that live outside this assembly (e.g. the commerce components).
/// </summary>
public static class AutomationComponentServiceCollectionExtensions
{
    /// <summary>
    /// Adds a component to the startup status log and the step registry tab, next to <see cref="AutomationComponentCatalog.All"/>.
    /// Its enabled state is read from configuration like any other component. Adding the same component again does nothing.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a different component with the same name is already known.</exception>
    public static IServiceCollection AddAutomationComponent(this IServiceCollection services, AutomationComponentDescriptor component)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentException.ThrowIfNullOrWhiteSpace(component.Name);

        if (AutomationComponentCatalog.All.Any(existing => existing.Name.Equals(component.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Automation component '{component.Name}' is part of the built-in catalog.", nameof(component));
        }

        var registered = services
            .Select(descriptor => descriptor.ServiceType == typeof(AutomationComponentDescriptor) ? descriptor.ImplementationInstance as AutomationComponentDescriptor : null)
            .FirstOrDefault(existing => existing?.Name.Equals(component.Name, StringComparison.OrdinalIgnoreCase) == true);

        if (registered is not null)
        {
            return registered == component
                ? services
                : throw new ArgumentException($"Automation component '{component.Name}' is already registered as {registered.Kind}.", nameof(component));
        }

        services.AddSingleton(component);

        return services;
    }
}
