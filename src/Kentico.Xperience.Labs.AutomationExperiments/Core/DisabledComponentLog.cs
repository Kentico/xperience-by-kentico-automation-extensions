using CMS.Core;

using Kentico.Xperience.Labs.AutomationExperiments.Configuration;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Logs that a disabled component was reached in a process, at most once per hour per component and process.
/// </summary>
internal static class DisabledComponentLog
{
    private static readonly TimeSpan logInterval = TimeSpan.FromHours(1);


    public static void Write(ILogger logger, AutomationComponentDescriptor component, string processName)
    {
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{component.Identifier}|Disabled|{processName}", logInterval),
            l => l.LogWarning(
                LogEvents.ComponentDisabled,
                "Automation component '{ComponentName}' used in process '{ProcessName}' is disabled and does nothing. Enable it with '{ConfigurationPath}': true.",
                component.Name,
                processName,
                component.ConfigurationPath));
    }
}
