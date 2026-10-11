using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Payload of <see cref="ContactGroupMemberTrigger"/>. Available to later steps via <c>GetTriggerData</c>.
/// Property names are persisted with process data – never rename them.
/// </summary>
public sealed class ContactGroupMemberTriggerData : IAutomationTriggerData
{
    /// <inheritdoc />
    public string Identifier => "Labs.AutomationExtensions.ContactGroupMemberTriggerData";


    /// <summary>
    /// Code name of the contact group the trigger fired for.
    /// </summary>
    public string ContactGroupName { get; init; } = string.Empty;


    /// <summary>
    /// Display name of the contact group at the time the trigger fired.
    /// </summary>
    public string ContactGroupDisplayName { get; init; } = string.Empty;
}
