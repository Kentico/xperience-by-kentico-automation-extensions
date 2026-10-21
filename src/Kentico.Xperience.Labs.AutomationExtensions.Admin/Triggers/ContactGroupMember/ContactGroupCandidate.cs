namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Row of the contact group table loaded for a configured code name, before filtering.
/// </summary>
internal sealed record ContactGroupCandidate(int Id, string Name, string DisplayName, ContactGroupCandidateKind Kind);
