using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Payload of <see cref="ContactInactiveTrigger"/>. Available to later steps via <c>GetTriggerData</c>.
/// </summary>
public sealed class ContactInactiveTriggerData : IAutomationTriggerData
{
    /// <inheritdoc />
    public string Identifier => "Labs.AutomationExtensions.ContactInactiveTriggerData";


    /// <summary>
    /// Number of days without activity the trigger fired for.
    /// </summary>
    public int InactiveDays { get; init; }


    /// <summary>
    /// Date of the contact's last activity (UTC midnight of the server-local date).
    /// </summary>
    public DateTime LastActivityDate { get; init; }
}
