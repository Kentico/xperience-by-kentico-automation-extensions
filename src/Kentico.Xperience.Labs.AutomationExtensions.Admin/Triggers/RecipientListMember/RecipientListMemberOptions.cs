using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Settings of the <see cref="RecipientListMemberTrigger"/> and its scheduled task.
/// </summary>
/// <remarks>
/// Collections default to empty because configuration binding appends to (instead of replacing) pre-filled collections.
/// Use <see cref="GetRecipientLists"/> to read effective values.
/// </remarks>
public sealed class RecipientListMemberOptions
{
    /// <summary>
    /// Configuration path of <see cref="RecipientLists"/>, shown in task results when no lists are configured.
    /// </summary>
    public const string RECIPIENT_LISTS_CONFIGURATION_PATH =
        $"{AutomationExtensionsOptions.SECTION_NAME}:{nameof(AutomationExtensionsOptions.RecipientListMember)}:{nameof(RecipientLists)}";


    /// <summary>
    /// Code names of the recipient lists scanned by the scheduled task. Defaults to none (the task fires nothing).
    /// Each list adds one set of queries to every task run. Marketers can select only these lists.
    /// Contact groups are not supported and are ignored.
    /// </summary>
    public List<string> RecipientLists { get; set; } = [];


    /// <summary>
    /// Number of contacts loaded per database query.
    /// </summary>
    public int BatchSize { get; set; } = 500;


    /// <summary>
    /// Returns configured recipient list code names – trimmed, without empty entries and case-insensitive duplicates, in
    /// configuration order.
    /// </summary>
    public IReadOnlyList<string> GetRecipientLists() => RecipientLists
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(name => name.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
