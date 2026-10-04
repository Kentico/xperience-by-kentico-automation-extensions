using Kentico.Xperience.Labs.AutomationExperiments.Core;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers step types whose saved results can be checked by the "Previous step succeeded" condition.
/// </summary>
public static class StepResultSourceServiceCollectionExtensions
{
    /// <summary>
    /// Adds a step type to the step drop-down of the "Previous step succeeded" condition.
    /// The step saves results with <see cref="StepResultRecorder"/> using the same <paramref name="name"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="name">Stable, unique name used in persisted result keys. Prefix it (e.g. <c>Acme.SyncToCrm</c>) and never change it once used.</param>
    /// <param name="displayName">Text shown to marketers – an admin localization resource key or plain text.</param>
    /// <exception cref="ArgumentException">Thrown when the name is empty or already registered with a different display name.</exception>
    public static IServiceCollection AddStepResultSource(this IServiceCollection services, string name, string displayName) =>
        services.AddStepResultSource(new StepResultSource(name, displayName));


    /// <inheritdoc cref="AddStepResultSource(IServiceCollection, string, string)"/>
    public static IServiceCollection AddStepResultSource(this IServiceCollection services, StepResultSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.DisplayName);

        var existing = services
            .Select(descriptor => descriptor.ServiceType == typeof(StepResultSource) ? descriptor.ImplementationInstance as StepResultSource : null)
            .FirstOrDefault(registered => string.Equals(registered?.Name, source.Name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            if (existing == source)
            {
                return services;
            }

            throw new ArgumentException($"Step result source '{source.Name}' is already registered with display name '{existing.DisplayName}'.", nameof(source));
        }

        services.AddSingleton(source);

        return services;
    }
}
