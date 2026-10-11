using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Settings of the <see cref="ContactGroupMemberTrigger"/> and its scheduled task.
/// </summary>
/// <remarks>
/// Collections default to empty because configuration binding appends to (instead of replacing) pre-filled collections.
/// Use <see cref="GetContactGroups"/> to read effective values.
/// </remarks>
public sealed class ContactGroupMemberOptions
{
    /// <summary>
    /// Configuration path of <see cref="ContactGroups"/>, shown in task results when no groups are configured.
    /// </summary>
    public const string CONTACT_GROUPS_CONFIGURATION_PATH =
        $"{AutomationExtensionsOptions.SECTION_NAME}:{nameof(AutomationExtensionsOptions.ContactGroupMember)}:{nameof(ContactGroups)}";


    /// <summary>
    /// Code names of the contact groups scanned by the scheduled task. Defaults to none (the task fires nothing).
    /// Each group adds one set of queries to every task run. Marketers can select only these groups.
    /// Recipient lists are not supported and are ignored.
    /// </summary>
    public List<string> ContactGroups { get; set; } = [];


    /// <summary>
    /// Number of contacts loaded per database query.
    /// </summary>
    public int BatchSize { get; set; } = 500;


    /// <summary>
    /// Returns configured contact group code names – trimmed, without empty entries and case-insensitive duplicates, in
    /// configuration order.
    /// </summary>
    public IReadOnlyList<string> GetContactGroups() => ContactGroups
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(name => name.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
