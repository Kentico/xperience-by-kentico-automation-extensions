using CMS.Base;
using CMS.Localization;

using Kentico.Xperience.Labs.AutomationExtensions.Resources;

[assembly: RegisterLocalizationResource(typeof(AutomationExtensionsResources), LocalizationTarget.Builder, SystemContext.SYSTEM_CULTURE_NAME)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Resources;

/// <summary>
/// Encapsulates the admin UI localization resources (<c>AutomationExtensionsResources.resx</c>) of the library.
/// </summary>
/// <remarks>
/// Registered for <see cref="LocalizationTarget.Builder"/> only. Trigger display names are plain text (see
/// <c>kentico-issue-trigger-display-name-resource-key.md</c>).
/// </remarks>
#pragma warning disable S2094 // Empty class encapsulates the .resx file, required by RegisterLocalizationResource
public sealed class AutomationExtensionsResources
{
}
#pragma warning restore S2094
