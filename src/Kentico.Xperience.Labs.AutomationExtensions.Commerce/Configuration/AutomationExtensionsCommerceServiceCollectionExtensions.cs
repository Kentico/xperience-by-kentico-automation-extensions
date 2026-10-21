using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the commerce automation components.
/// </summary>
public static class AutomationExtensionsCommerceServiceCollectionExtensions
{
    /// <summary>
    /// Adds services of the commerce automation components (e.g. <see cref="IOrderCompletedTriggerDispatcher"/>).
    /// Call after <c>AddAutomationExtensions</c>, which binds the shared configuration. Components are disabled unless
    /// enabled in configuration (<c>AutomationExtensions:Components:OrderCompleted</c>).
    /// </summary>
    public static IServiceCollection AddAutomationExtensionsCommerce(this IServiceCollection services)
    {
        foreach (var component in CommerceAutomationComponents.All)
        {
            services.AddAutomationComponent(component);
        }

        services.TryAddSingleton<IOrderCompletedDataSource, OrderCompletedDataSource>();
        services.TryAddSingleton<IOrderCompletedTriggerDispatcher, OrderCompletedTriggerDispatcher>();

        return services;
    }
}
