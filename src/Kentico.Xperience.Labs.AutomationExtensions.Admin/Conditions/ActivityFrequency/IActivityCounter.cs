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
    /// <param name="criteria">"Column matches text" filters, all must match; empty for no filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<int> Count(int contactId, string activityType, DateTime since, IReadOnlyList<ActivityDetailCriterion> criteria, CancellationToken cancellationToken);
}
