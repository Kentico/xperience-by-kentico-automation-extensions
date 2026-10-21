namespace Kentico.Xperience.Labs.AutomationExtensions.Configuration;

/// <summary>
/// Resolves whether a library component is enabled through configuration.
/// </summary>
/// <remarks>
/// All components are always registered (unregistering a component used by a process breaks the process).
/// Disabled components stay visible in the Automation Builder but do nothing at runtime.
/// </remarks>
public interface IAutomationComponentStatus
{
    /// <summary>
    /// Returns <c>true</c> when the component is enabled.
    /// </summary>
    public bool IsEnabled(AutomationComponentDescriptor component);
}
