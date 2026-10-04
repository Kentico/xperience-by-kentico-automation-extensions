using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

/// <summary>
/// Payload of <see cref="ContactDateAnniversaryTrigger"/>. Available to later steps via <c>GetTriggerData</c>.
/// </summary>
public sealed class ContactDateAnniversaryTriggerData : IAutomationTriggerData
{
    /// <inheritdoc />
    public string Identifier => "Labs.AutomationExperiments.ContactDateAnniversaryTriggerData";


    /// <summary>
    /// Name of the contact date field that matched.
    /// </summary>
    public string FieldName { get; init; } = string.Empty;


    /// <summary>
    /// Number of days before the anniversary the trigger fired.
    /// </summary>
    public int DaysBefore { get; init; }


    /// <summary>
    /// Anniversary date (UTC midnight of the server-local date).
    /// </summary>
    public DateTime AnniversaryDate { get; init; }


    /// <summary>
    /// Whole years since the original date on the anniversary (e.g. age on a birthday).
    /// </summary>
    public int Years { get; init; }
}
