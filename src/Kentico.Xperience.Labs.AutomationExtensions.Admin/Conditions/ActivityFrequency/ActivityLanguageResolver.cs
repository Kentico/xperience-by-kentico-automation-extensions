using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Lists content languages for the language drop-down and resolves stored language code names to IDs.
/// </summary>
/// <remarks>
/// Display names may be plain text or localization macros, so they are resolved via <see cref="ILocalizationService.LocalizeString"/>.
/// </remarks>
public sealed class ActivityLanguageResolver
{
    private readonly ILocalizationService localizationService;
    private readonly Func<CancellationToken, Task<IEnumerable<ActivityLanguageDefinition>>> getLanguages;
    private readonly Func<string, CancellationToken, Task<ActivityLanguageDefinition?>> findLanguage;


    public ActivityLanguageResolver(ILocalizationService localizationService, IInfoProvider<ContentLanguageInfo> contentLanguageProvider)
        : this(
            localizationService,
            async cancellationToken => (await contentLanguageProvider.Get()
                .Columns(nameof(ContentLanguageInfo.ContentLanguageID), nameof(ContentLanguageInfo.ContentLanguageName), nameof(ContentLanguageInfo.ContentLanguageDisplayName))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                .Select(ToDefinition),
            // Content languages are cached by code name (InfoCache), so the per-contact lookup doesn't query the database.
            async (name, cancellationToken) => await contentLanguageProvider.GetAsync(name, cancellationToken) is { } info ? ToDefinition(info) : null)
    {
    }


    /// <summary>
    /// Uses the given language sources (tests run without an Xperience container).
    /// </summary>
    internal ActivityLanguageResolver(
        ILocalizationService localizationService,
        Func<CancellationToken, Task<IEnumerable<ActivityLanguageDefinition>>> getLanguages,
        Func<string, CancellationToken, Task<ActivityLanguageDefinition?>> findLanguage)
    {
        this.localizationService = localizationService;
        this.getLanguages = getLanguages;
        this.findLanguage = findLanguage;
    }


    /// <summary>
    /// Returns the content languages with localized display names, sorted by display name.
    /// </summary>
    public async Task<IReadOnlyList<ActivityLanguageDefinition>> GetLanguages(CancellationToken cancellationToken) =>
        (await getLanguages(cancellationToken))
            .Select(language => language with { DisplayName = GetDisplayName(language) })
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();


    /// <summary>
    /// Returns the ID of the content language with code name <paramref name="languageName"/>, or <c>null</c> when it
    /// doesn't exist (or no name is given).
    /// </summary>
    public async Task<int?> ResolveLanguageId(string? languageName, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(languageName)
            ? null
            : (await findLanguage(languageName, cancellationToken))?.Id;


    private static ActivityLanguageDefinition ToDefinition(ContentLanguageInfo info) =>
        new(info.ContentLanguageName, info.ContentLanguageDisplayName, info.ContentLanguageID);


    private string GetDisplayName(ActivityLanguageDefinition language) =>
        string.IsNullOrEmpty(language.DisplayName) ? language.Name : localizationService.LocalizeString(language.DisplayName);
}
