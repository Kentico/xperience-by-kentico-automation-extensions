using System.Globalization;

using CMS.Activities;
using CMS.DataEngine;
using CMS.DataEngine.Query;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Counts activities with parameterized queries: a single <c>COUNT</c>, or a <c>COUNT</c> grouped by one column for URL
/// rules. No activity rows are loaded.
/// </summary>
/// <remarks>
/// Each criterion adds a parameterized condition (<see cref="ActivityDetailFilters.ToWhereCondition"/>), combined with AND. Column names come
/// from <see cref="ActivityDetailFilters"/>, never from marketer input. The page filter is a grouped <c>OR</c> with one parameter per page GUID.
/// </remarks>
internal sealed class ActivityCounter(IInfoProvider<ActivityInfo> activityProvider) : IActivityCounter
{
    /// <summary>
    /// Alias of the count column of <see cref="CountByValue"/>.
    /// </summary>
    internal const string COUNT_COLUMN = "ActivityValueCount";


    private readonly IInfoProvider<ActivityInfo> activityProvider = activityProvider;


    /// <inheritdoc />
    public Task<int> Count(int contactId, string activityType, DateTime since, IReadOnlyList<ActivityCriterion> criteria, CancellationToken cancellationToken) =>
        Filter(activityProvider.Get(), contactId, activityType, since, criteria).GetCountAsync(cancellationToken);


    /// <inheritdoc />
    public async Task<IReadOnlyList<ActivityValueCount>> CountByValue(
        int contactId,
        string activityType,
        DateTime since,
        IReadOnlyList<ActivityCriterion> criteria,
        string column,
        int maxValues,
        CancellationToken cancellationToken)
    {
        var query = GroupByValue(Filter(activityProvider.Get(), contactId, activityType, since, criteria), column, maxValues);
        var rows = await query.GetDataContainerResultAsync(cancellationToken: cancellationToken);

        return rows
            .Select(row => new ActivityValueCount(row[column] as string, Convert.ToInt32(row[COUNT_COLUMN], CultureInfo.InvariantCulture)))
            .ToList();
    }


    /// <summary>
    /// Applies the contact, type, window, and criteria conditions. Empty conditions (URL rules without a prefilter) are skipped.
    /// </summary>
    internal static ObjectQuery<ActivityInfo> Filter(ObjectQuery<ActivityInfo> query, int contactId, string activityType, DateTime since, IReadOnlyList<ActivityCriterion> criteria)
    {
        query = query
            .WhereEquals(nameof(ActivityInfo.ActivityContactID), contactId)
            .WhereEquals(nameof(ActivityInfo.ActivityType), activityType)
            .WhereGreaterOrEquals(nameof(ActivityInfo.ActivityCreated), since);

        foreach (var criterion in criteria)
        {
            var condition = ActivityDetailFilters.ToWhereCondition(criterion);
            if (!condition.WhereIsEmpty)
            {
                query = query.Where(condition);
            }
        }

        return query;
    }


    /// <summary>
    /// Projects <c>column, COUNT(*)</c> grouped (and ordered, replacing any default order) by <paramref name="column"/>, top <paramref name="maxValues"/> groups.
    /// </summary>
    internal static ObjectQuery<ActivityInfo> GroupByValue(ObjectQuery<ActivityInfo> query, string column, int maxValues) => query
        .Columns(new QueryColumn(column), new CountColumn(string.Empty).As(COUNT_COLUMN))
        .GroupBy(column)
        .OrderBy(column)
        .TopN(maxValues);
}
