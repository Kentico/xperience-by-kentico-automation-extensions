using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExperiments.Configuration;

/// <summary>
/// Resolves the enabled state of components from <see cref="AutomationExperimentsOptions"/>. Reflects configuration reloads.
/// </summary>
internal sealed class AutomationComponentStatus(IOptionsMonitor<AutomationExperimentsOptions> options) : IAutomationComponentStatus
{
    private readonly IOptionsMonitor<AutomationExperimentsOptions> options = options;


    /// <inheritdoc />
    public bool IsEnabled(AutomationComponentDescriptor component)
    {
        var currentOptions = options.CurrentValue;

        if (currentOptions.Components.TryGetValue(component.Name, out bool enabled))
        {
            return enabled;
        }

        return currentOptions.EnabledByDefault;
    }
}
