using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Configuration;

/// <summary>
/// Resolves the enabled state of components from <see cref="AutomationExtensionsOptions"/>. Reflects configuration reloads.
/// </summary>
internal sealed class AutomationComponentStatus(IOptionsMonitor<AutomationExtensionsOptions> options) : IAutomationComponentStatus
{
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;


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
