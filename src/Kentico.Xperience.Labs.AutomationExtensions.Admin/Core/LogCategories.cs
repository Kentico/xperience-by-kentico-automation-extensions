using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Logger categories used by the library. Create loggers via <c>ILoggerFactory.CreateLogger(LogCategories.For(component))</c>
/// instead of <c>ILogger&lt;T&gt;</c>.
/// </summary>
/// <remarks>
/// The Xperience event log stores the category in a 100-character Source column and silently drops entries with longer
/// categories. Full type names in this library (namespace per component kind and component) easily exceed that, so
/// categories are built from the component name instead. The <c>Kentico</c> prefix keeps Information entries in the
/// event log with the default <c>XperienceEventLog</c> configuration.
/// </remarks>
public static class LogCategories
{
    /// <summary>
    /// Prefix of all library categories.
    /// </summary>
    public const string PREFIX = "Kentico.Xperience.Labs.AutomationExtensions";

    /// <summary>
    /// Category of library-wide infrastructure (configuration, component status).
    /// </summary>
    public const string CONFIGURATION = PREFIX + ".Configuration";


    /// <summary>
    /// Returns the category of the given component, e.g. <c>Kentico.Xperience.Labs.AutomationExtensions.SendWebhook</c>.
    /// </summary>
    public static string For(AutomationComponentDescriptor component) => $"{PREFIX}.{component.Name}";
}
