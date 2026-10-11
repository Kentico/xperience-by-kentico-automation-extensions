namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Settings of the <see cref="ContactInactiveTrigger"/> and its scheduled task.
/// </summary>
/// <remarks>
/// Collections default to empty because configuration binding appends to (instead of replacing) pre-filled collections.
/// Use <see cref="GetInactiveDays"/> to read effective values.
/// </remarks>
public sealed class ContactInactiveOptions
{
    public const int MINIMUM_INACTIVE_DAYS = 1;
    public const int MAXIMUM_INACTIVE_DAYS = 3650;
    public const int DEFAULT_INACTIVE_DAYS = 90;


    /// <summary>
    /// Numbers of days without activity the scheduled task fires the trigger for. Defaults to <c>[90]</c>.
    /// Each value adds one set of queries to every task run. Marketers can select only these values.
    /// </summary>
    public List<int> InactiveDays { get; set; } = [];


    /// <summary>
    /// Number of contacts loaded per database query.
    /// </summary>
    public int BatchSize { get; set; } = 500;


    /// <summary>
    /// Returns configured, valid (<see cref="MINIMUM_INACTIVE_DAYS"/> – <see cref="MAXIMUM_INACTIVE_DAYS"/>) day counts in
    /// ascending order, or <c>[90]</c> when none are configured.
    /// </summary>
    public IReadOnlyList<int> GetInactiveDays()
    {
        var days = InactiveDays
            .Where(day => day is >= MINIMUM_INACTIVE_DAYS and <= MAXIMUM_INACTIVE_DAYS)
            .Distinct()
            .Order()
            .ToList();

        return days.Count > 0 ? days : [DEFAULT_INACTIVE_DAYS];
    }
}
