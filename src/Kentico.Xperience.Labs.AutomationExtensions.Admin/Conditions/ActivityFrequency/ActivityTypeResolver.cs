using CMS.Activities;
using CMS.Core;
using CMS.DataEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Lists activity types for the activity type drop-down and validates stored activity type code names.
/// </summary>
/// <remarks>
/// Activity type display names may be plain text or localization macros, so they are resolved via <see cref="ILocalizationService.LocalizeString"/>.
/// </remarks>
public sealed class ActivityTypeResolver
{
    private readonly ILocalizationService localizationService;
    private readonly Func<CancellationToken, Task<IEnumerable<ActivityTypeDefinition>>> getTypes;
    private readonly Func<string, CancellationToken, Task<ActivityTypeDefinition?>> findType;


    public ActivityTypeResolver(ILocalizationService localizationService, IInfoProvider<ActivityTypeInfo> activityTypeProvider)
        : this(
            localizationService,
            async cancellationToken => (await activityTypeProvider.Get()
                .Columns(nameof(ActivityTypeInfo.ActivityTypeName), nameof(ActivityTypeInfo.ActivityTypeDisplayName), nameof(ActivityTypeInfo.ActivityTypeEnabled))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                .Select(ToDefinition),
            // Activity types are cached by code name (InfoCache), so the per-contact lookup doesn't query the database.
            async (name, cancellationToken) => await activityTypeProvider.GetAsync(name, cancellationToken) is { } info ? ToDefinition(info) : null)
    {
    }


    /// <summary>
    /// Uses the given activity type sources (tests run without an Xperience container).
    /// </summary>
    internal ActivityTypeResolver(
        ILocalizationService localizationService,
        Func<CancellationToken, Task<IEnumerable<ActivityTypeDefinition>>> getTypes,
        Func<string, CancellationToken, Task<ActivityTypeDefinition?>> findType)
    {
        this.localizationService = localizationService;
        this.getTypes = getTypes;
        this.findType = findType;
    }


    /// <summary>
    /// Returns the enabled activity types with localized display names, sorted by display name.
    /// </summary>
    public async Task<IReadOnlyList<ActivityTypeDefinition>> GetEnabledTypes(CancellationToken cancellationToken) =>
        (await getTypes(cancellationToken))
            .Where(type => type.Enabled)
            .Select(type => type with { DisplayName = GetDisplayName(type) })
            .OrderBy(type => type.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();


    /// <summary>
    /// Returns the defined code name of the activity type matching <paramref name="activityType"/>, or <c>null</c> when it doesn't exist.
    /// Disabled types resolve too – contacts may have activities logged before the type was disabled.
    /// </summary>
    public async Task<string?> ResolveTypeName(string? activityType, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(activityType)
            ? null
            : (await findType(activityType, cancellationToken))?.Name;


    private static ActivityTypeDefinition ToDefinition(ActivityTypeInfo info) =>
        new(info.ActivityTypeName, info.ActivityTypeDisplayName, info.ActivityTypeEnabled);


    private string GetDisplayName(ActivityTypeDefinition type) =>
        string.IsNullOrEmpty(type.DisplayName) ? type.Name : localizationService.LocalizeString(type.DisplayName);
}
