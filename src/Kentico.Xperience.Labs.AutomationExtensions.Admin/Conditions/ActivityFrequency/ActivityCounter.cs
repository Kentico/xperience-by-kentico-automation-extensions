using CMS.Activities;
using CMS.DataEngine;
using CMS.DataEngine.Query;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Counts activities with a single parameterized <c>COUNT</c> query – no activity rows are loaded.
/// </summary>
/// <remarks>
/// Each criterion adds a parameterized condition (<see cref="ActivityDetailFilters.ToWhereCondition"/>), combined with AND. Column names come
/// from <see cref="ActivityDetailFilters.GetColumn"/>, never from marketer input.
/// </remarks>
internal sealed class ActivityCounter(IInfoProvider<ActivityInfo> activityProvider) : IActivityCounter
{
    private readonly IInfoProvider<ActivityInfo> activityProvider = activityProvider;


    /// <inheritdoc />
    public Task<int> Count(int contactId, string activityType, DateTime since, IReadOnlyList<ActivityDetailCriterion> criteria, CancellationToken cancellationToken)
    {
        var query = activityProvider.Get()
            .WhereEquals(nameof(ActivityInfo.ActivityContactID), contactId)
            .WhereEquals(nameof(ActivityInfo.ActivityType), activityType)
            .WhereGreaterOrEquals(nameof(ActivityInfo.ActivityCreated), since);

        foreach (var criterion in criteria)
        {
            query = query.Where(ActivityDetailFilters.ToWhereCondition(criterion));
        }

        return query.GetCountAsync(cancellationToken);
    }
}
