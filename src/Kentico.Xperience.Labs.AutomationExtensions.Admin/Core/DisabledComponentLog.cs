using CMS.Core;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Logs that a disabled component was reached in a process, at most once per hour per component and process.
/// </summary>
internal static class DisabledComponentLog
{
    private static readonly TimeSpan logInterval = TimeSpan.FromHours(1);


    public static void Write(ILogger logger, AutomationComponentDescriptor component, string processName, string disabledBehavior = "does nothing")
    {
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{component.Identifier}|Disabled|{processName}", logInterval),
            l => l.LogWarning(
                LogEvents.ComponentDisabled,
                "Automation component '{ComponentName}' used in process '{ProcessName}' is disabled and {DisabledBehavior}. Enable it with '{ConfigurationPath}': true.",
                component.Name,
                processName,
                disabledBehavior,
                component.ConfigurationPath));
    }
}
