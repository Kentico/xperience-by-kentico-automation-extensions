using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Supplies "Any" (empty value = no filter) and the content languages to the language drop-down of <see cref="ActivityFrequencyProperties"/>.
/// </summary>
internal sealed class ActivityLanguageOptionsProvider(ActivityLanguageResolver languageResolver, ILocalizationService localizationService) : IDropDownOptionsProvider
{
    public const string ANY_RESOURCE_KEY = "labs.automationextensions.activityfrequency.language.any";


    private readonly ActivityLanguageResolver languageResolver = languageResolver;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        new[] { new DropDownOptionItem { Value = string.Empty, Text = localizationService.GetString(ANY_RESOURCE_KEY) } }
            .Concat((await languageResolver.GetLanguages(CancellationToken.None))
                .Select(language => new DropDownOptionItem
                {
                    Value = language.Name,
                    Text = language.DisplayName
                }))
            .ToList();
}
