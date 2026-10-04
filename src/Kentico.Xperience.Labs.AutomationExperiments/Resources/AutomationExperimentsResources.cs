using CMS.Base;
using CMS.Localization;

using Kentico.Xperience.Labs.AutomationExperiments.Resources;

[assembly: RegisterLocalizationResource(typeof(AutomationExperimentsResources), LocalizationTarget.Server, SystemContext.SYSTEM_CULTURE_NAME)]
[assembly: RegisterLocalizationResource(typeof(AutomationExperimentsResources), LocalizationTarget.Builder, SystemContext.SYSTEM_CULTURE_NAME)]

namespace Kentico.Xperience.Labs.AutomationExperiments.Resources;

/// <summary>
/// Encapsulates the admin UI localization resources (<c>AutomationExperimentsResources.resx</c>) of the library.
/// </summary>
/// <remarks>
/// Registered for both <see cref="LocalizationTarget.Server"/> and <see cref="LocalizationTarget.Builder"/> until it is
/// confirmed which target the Automation Builder resolves step texts from.
/// </remarks>
#pragma warning disable S2094 // Empty class encapsulates the .resx file, required by RegisterLocalizationResource
public sealed class AutomationExperimentsResources
{
}
#pragma warning restore S2094
