using CMS.Base;
using CMS.Localization;

using Kentico.Xperience.Labs.AutomationExtensions.Resources;

[assembly: RegisterLocalizationResource(typeof(AutomationExtensionsCommerceResources), LocalizationTarget.Server, SystemContext.SYSTEM_CULTURE_NAME)]
[assembly: RegisterLocalizationResource(typeof(AutomationExtensionsCommerceResources), LocalizationTarget.Builder, SystemContext.SYSTEM_CULTURE_NAME)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Resources;

/// <summary>
/// Encapsulates the admin UI localization resources (<c>AutomationExtensionsCommerceResources.resx</c>) of the commerce components.
/// </summary>
/// <remarks>
/// Registered for both <see cref="LocalizationTarget.Server"/> and <see cref="LocalizationTarget.Builder"/>, same as
/// <see cref="AutomationExtensionsResources"/>.
/// </remarks>
#pragma warning disable S2094 // Empty class encapsulates the .resx file, required by RegisterLocalizationResource
public sealed class AutomationExtensionsCommerceResources
{
}
#pragma warning restore S2094
