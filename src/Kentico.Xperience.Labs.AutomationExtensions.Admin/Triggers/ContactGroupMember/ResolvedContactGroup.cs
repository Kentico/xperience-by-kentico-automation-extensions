namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Configured contact group that exists and can be scanned by <see cref="ContactGroupMemberTask"/>.
/// </summary>
/// <param name="Id">Contact group ID.</param>
/// <param name="Name">Code name.</param>
/// <param name="DisplayName">Display name.</param>
internal sealed record ResolvedContactGroup(int Id, string Name, string DisplayName);
