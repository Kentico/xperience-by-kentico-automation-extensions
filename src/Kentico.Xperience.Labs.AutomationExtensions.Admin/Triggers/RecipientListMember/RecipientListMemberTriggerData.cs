using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Payload of <see cref="RecipientListMemberTrigger"/>. Available to later steps via <c>GetTriggerData</c>.
/// Property names are persisted with process data – never rename them.
/// </summary>
public sealed class RecipientListMemberTriggerData : IAutomationTriggerData, ITriggerDataDescription
{
    /// <inheritdoc />
    public string Identifier => "Labs.AutomationExtensions.RecipientListMemberTriggerData";


    /// <summary>
    /// Code name of the recipient list the trigger fired for.
    /// </summary>
    public string RecipientListName { get; init; } = string.Empty;


    /// <summary>
    /// Display name of the recipient list at the time the trigger fired.
    /// </summary>
    public string RecipientListDisplayName { get; init; } = string.Empty;


    /// <inheritdoc />
    public string Describe() => $"recipient list {RecipientListName}";
}
