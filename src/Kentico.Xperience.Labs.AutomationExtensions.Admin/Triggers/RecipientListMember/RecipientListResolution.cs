using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Result of resolving the configured recipient list code names.
/// </summary>
/// <param name="Lists">Usable recipient lists, in configuration order.</param>
/// <param name="NotFound">Configured code names without a matching row.</param>
/// <param name="NotRecipientLists">
/// Configured code names that belong to contact groups or other object types, e.g. segments (not supported).
/// </param>
internal sealed record RecipientListResolution(
    IReadOnlyList<ResolvedContactGroup> Lists,
    IReadOnlyList<string> NotFound,
    IReadOnlyList<string> NotRecipientLists)
{
    public static RecipientListResolution Empty { get; } = new([], [], []);
}
