namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Object type of a <see cref="ContactGroupCandidate"/>. The contact group table also stores other object types.
/// </summary>
internal enum ContactGroupCandidateKind
{
    /// <summary>
    /// Contact group (<c>om.contactgroup</c>) – supported.
    /// </summary>
    ContactGroup,

    /// <summary>
    /// Recipient list (<c>om.recipientlist</c>) – not supported.
    /// </summary>
    RecipientList,

    /// <summary>
    /// Any other object type sharing the table (e.g. segments in Xperience 31.9) – not supported.
    /// </summary>
    Other
}
