namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Counts logged activities of a contact.
/// </summary>
public interface IActivityCounter
{
    /// <summary>
    /// Returns the number of activities of <paramref name="activityType"/> the contact performed since <paramref name="since"/>
    /// (inclusive) that match all <paramref name="criteria"/>.
    /// </summary>
    /// <param name="contactId">Contact ID.</param>
    /// <param name="activityType">Activity type code name.</param>
    /// <param name="since">Start of the window in the time basis of stored activities (see <see cref="ActivityWindow"/>).</param>
    /// <param name="criteria">Detail and page filters, all must match; empty for no filter. Must not contain <see cref="ActivityUrlMatchCriterion"/> – use <see cref="CountByValue"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<int> Count(int contactId, string activityType, DateTime since, IReadOnlyList<ActivityCriterion> criteria, CancellationToken cancellationToken);


    /// <summary>
    /// Returns the matching activities grouped by the distinct values of <paramref name="column"/> (with their counts), at
    /// most <paramref name="maxValues"/> groups. Same filters as <see cref="Count"/>; an <see cref="ActivityUrlMatchCriterion"/>
    /// applies only its database prefilter – the caller matches the returned values.
    /// </summary>
    /// <param name="contactId">Contact ID.</param>
    /// <param name="activityType">Activity type code name.</param>
    /// <param name="since">Start of the window (see <see cref="Count"/>).</param>
    /// <param name="criteria">Filters, all must match.</param>
    /// <param name="column">Grouped activity column (fixed set, never marketer input).</param>
    /// <param name="maxValues">Maximum number of groups returned. Ask for one more than allowed to detect an exceeded limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IReadOnlyList<ActivityValueCount>> CountByValue(
        int contactId,
        string activityType,
        DateTime since,
        IReadOnlyList<ActivityCriterion> criteria,
        string column,
        int maxValues,
        CancellationToken cancellationToken);
}
