namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Result of resolving the configured contact group code names.
/// </summary>
/// <param name="Groups">Usable contact groups, in configuration order.</param>
/// <param name="NotFound">Configured code names without a matching row.</param>
/// <param name="RecipientLists">Configured code names that belong to recipient lists (not supported).</param>
/// <param name="OtherTypes">Configured code names that belong to other object types, e.g. segments (not supported).</param>
internal sealed record ContactGroupResolution(
    IReadOnlyList<ResolvedContactGroup> Groups,
    IReadOnlyList<string> NotFound,
    IReadOnlyList<string> RecipientLists,
    IReadOnlyList<string> OtherTypes)
{
    public static ContactGroupResolution Empty { get; } = new([], [], [], []);
}
