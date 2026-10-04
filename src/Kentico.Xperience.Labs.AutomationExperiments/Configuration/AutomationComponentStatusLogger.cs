using Kentico.Xperience.Labs.AutomationExperiments.Core;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExperiments.Configuration;

/// <summary>
/// Logs the enabled state of all library components at application start and warns about unknown component names in configuration.
/// </summary>
internal sealed class AutomationComponentStatusLogger(
    IAutomationComponentStatus componentStatus,
    IOptions<AutomationExperimentsOptions> options,
    ILoggerFactory loggerFactory) : IHostedService
{
    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly IOptions<AutomationExperimentsOptions> options = options;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.CONFIGURATION);


    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var enabledNames = AutomationComponentCatalog.All
            .Where(componentStatus.IsEnabled)
            .Select(component => component.Name)
            .ToList();
        var disabledNames = AutomationComponentCatalog.All
            .Where(component => !componentStatus.IsEnabled(component))
            .Select(component => component.Name)
            .ToList();

        logger.LogInformation(
            LogEvents.ComponentStatus,
            "Automation experiments components enabled: {EnabledComponents}. Disabled: {DisabledComponents}. Configure in '{SectionName}'.",
            FormatNames(enabledNames),
            FormatNames(disabledNames),
            AutomationExperimentsOptions.SECTION_NAME);

        var knownNames = AutomationComponentCatalog.All
            .Select(component => component.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string unknownName in options.Value.Components.Keys.Where(name => !knownNames.Contains(name)))
        {
            logger.LogWarning(
                LogEvents.UnknownComponent,
                "Unknown automation experiments component '{ComponentName}' in configuration section '{SectionName}:{ComponentsKey}'. Known components: {KnownComponents}.",
                unknownName,
                AutomationExperimentsOptions.SECTION_NAME,
                nameof(AutomationExperimentsOptions.Components),
                string.Join(", ", knownNames));
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// Returns comma-separated names, or <c>none</c> when the list is empty.
    /// </summary>
    internal static string FormatNames(IReadOnlyCollection<string> names) =>
        names.Count == 0 ? "none" : string.Join(", ", names);


    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
