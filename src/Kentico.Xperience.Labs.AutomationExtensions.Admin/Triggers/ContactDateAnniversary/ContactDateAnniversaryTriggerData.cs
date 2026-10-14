using System.Globalization;

using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

/// <summary>
/// Payload of <see cref="ContactDateAnniversaryTrigger"/>. Available to later steps via <c>GetTriggerData</c>.
/// </summary>
public sealed class ContactDateAnniversaryTriggerData : IAutomationTriggerData, ITriggerDataDescription
{
    /// <inheritdoc />
    public string Identifier => "Labs.AutomationExtensions.ContactDateAnniversaryTriggerData";


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


    /// <inheritdoc />
    public string Describe() => string.Format(
        CultureInfo.InvariantCulture,
        "field {0}, {1} day(s) before, {2:yyyy-MM-dd}, {3} year(s)",
        FieldName,
        DaysBefore,
        AnniversaryDate,
        Years);
}
