namespace Kentico.Xperience.Labs.AutomationExtensions.Configuration;

/// <summary>
/// Describes an automation component provided by the library.
/// </summary>
/// <param name="Name">Component name used as the key in <see cref="AutomationExtensionsOptions.Components"/>.</param>
/// <param name="Kind">Component kind.</param>
public sealed record AutomationComponentDescriptor(string Name, AutomationComponentKind Kind)
{
    /// <summary>
    /// Registration identifier of the component. Never changes once released.
    /// </summary>
    public string Identifier => AutomationComponentCatalog.IDENTIFIER_PREFIX + Name;


    /// <summary>
    /// Configuration path that controls the enabled state of the component.
    /// </summary>
    public string ConfigurationPath => $"{AutomationExtensionsOptions.SECTION_NAME}:{nameof(AutomationExtensionsOptions.Components)}:{Name}";
}
