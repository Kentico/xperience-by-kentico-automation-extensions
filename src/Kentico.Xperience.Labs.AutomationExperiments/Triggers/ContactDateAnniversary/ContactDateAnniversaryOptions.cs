using CMS.ContactManagement;

namespace Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

/// <summary>
/// Settings of the <see cref="ContactDateAnniversaryTrigger"/> and its scheduled task.
/// </summary>
/// <remarks>
/// Collections default to empty because configuration binding appends to (instead of replacing) pre-filled collections.
/// Use <see cref="GetDateFields"/> and <see cref="GetDaysBefore"/> to read effective values.
/// </remarks>
public sealed class ContactDateAnniversaryOptions
{
    public const int MAXIMUM_DAYS_BEFORE = 365;


    /// <summary>
    /// Contact date fields scanned by the scheduled task. Defaults to <c>ContactBirthday</c>.
    /// Custom contact fields of the date or date and time type are supported.
    /// </summary>
    public List<string> DateFields { get; set; } = [];


    /// <summary>
    /// Days before the anniversary the scheduled task fires the trigger for (0 = on the day). Defaults to <c>[0]</c>.
    /// Each value adds one query per date field to every task run. Marketers can select only these values.
    /// </summary>
    public List<int> DaysBefore { get; set; } = [];


    /// <summary>
    /// Number of contacts loaded per database query.
    /// </summary>
    public int BatchSize { get; set; } = 500;


    /// <summary>
    /// Returns configured date fields, or <c>ContactBirthday</c> when none are configured.
    /// </summary>
    public IReadOnlyList<string> GetDateFields()
    {
        var fields = DateFields
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Select(field => field.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return fields.Count > 0 ? fields : [nameof(ContactInfo.ContactBirthday)];
    }


    /// <summary>
    /// Returns configured, valid (0 – <see cref="MAXIMUM_DAYS_BEFORE"/>) day offsets in ascending order, or <c>[0]</c> when none are configured.
    /// </summary>
    public IReadOnlyList<int> GetDaysBefore()
    {
        var days = DaysBefore
            .Where(day => day is >= 0 and <= MAXIMUM_DAYS_BEFORE)
            .Distinct()
            .Order()
            .ToList();

        return days.Count > 0 ? days : [0];
    }
}
