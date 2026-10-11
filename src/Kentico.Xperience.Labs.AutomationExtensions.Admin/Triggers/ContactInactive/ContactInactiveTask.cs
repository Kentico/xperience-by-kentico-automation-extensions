using System.Globalization;

using CMS.Activities;
using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Scheduler;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(ContactInactiveTask.IDENTIFIER, typeof(ContactInactiveTask))]

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Fires <see cref="ContactInactiveTrigger"/> for contacts whose last activity was exactly each configured number of days
/// before today (server local time). Requires a task configuration in the Scheduled tasks application – run it once per day.
/// </summary>
/// <remarks>
/// <para>
/// No state is stored: a contact matches only on the day its last activity reaches the threshold. Contacts not reached that
/// day (skipped or aborted run, lost in-memory trigger queue) are not fired later. Contacts without activities never match.
/// </para>
/// <para>
/// Fired triggers enter the shared in-memory trigger queue. Contacts are loaded in batches; processes decide via
/// <see cref="ContactInactiveTrigger.Evaluate"/> whether they start.
/// </para>
/// </remarks>
internal sealed class ContactInactiveTask(
    IAutomationTriggerDispatcher triggerDispatcher,
    IInfoProvider<ActivityInfo> activityInfoProvider,
    IInfoProvider<ContactInfo> contactInfoProvider,
    IAutomationComponentStatus componentStatus,
    IOptionsMonitor<AutomationExtensionsOptions> options,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : IScheduledTask
{
    /// <summary>
    /// Task identifier selected in the Scheduled tasks application. Persisted in task configurations – never change.
    /// </summary>
    public const string IDENTIFIER = ContactInactiveTrigger.IDENTIFIER + "Task";


    private readonly IAutomationTriggerDispatcher triggerDispatcher = triggerDispatcher;
    private readonly IInfoProvider<ActivityInfo> activityInfoProvider = activityInfoProvider;
    private readonly IInfoProvider<ContactInfo> contactInfoProvider = contactInfoProvider;
    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.ContactInactive));


    /// <inheritdoc />
    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        var component = AutomationComponentCatalog.ContactInactive;
        if (!componentStatus.IsEnabled(component))
        {
            return new ScheduledTaskExecutionResult($"Disabled via configuration ('{component.ConfigurationPath}'). No triggers fired.");
        }

        var inactiveOptions = options.CurrentValue.ContactInactive;
        int batchSize = Math.Max(1, inactiveOptions.BatchSize);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        int firedCount = 0;

        foreach (int inactiveDays in inactiveOptions.GetInactiveDays())
        {
            firedCount += await FireForRange(inactiveDays, InactivityCalendar.GetRange(today, inactiveDays), batchSize, cancellationToken);
        }

        logger.LogInformation(LogEvents.ContactInactiveTaskCompleted, "Contact inactive task fired {FiredCount} trigger(s) for {Today}.", firedCount, today);

        return ScheduledTaskExecutionResult.Success;
    }


    /// <summary>
    /// Selects one batch of contact IDs whose last activity falls into <paramref name="range"/>, ordered by contact ID after
    /// <paramref name="lastContactId"/> (keyset paging).
    /// </summary>
    /// <remarks>
    /// <c>WHERE ActivityCreated &gt;= @start</c> limits the aggregation to activities since the range start; a contact matches
    /// when the latest of those is before the range end (i.e. at least one activity on that day and none after it).
    /// All values are query parameters.
    /// </remarks>
    internal static ObjectQuery<ActivityInfo> SelectContactIds(ObjectQuery<ActivityInfo> query, InactivityRange range, int lastContactId, int batchSize) => query
        .Columns(nameof(ActivityInfo.ActivityContactID))
        .WhereGreaterOrEquals(nameof(ActivityInfo.ActivityCreated), range.Start)
        .WhereGreaterThan(nameof(ActivityInfo.ActivityContactID), lastContactId)
        .GroupBy(nameof(ActivityInfo.ActivityContactID))
        .Having(having => having.WhereLessThan(new AggregatedColumn(AggregationType.Max, nameof(ActivityInfo.ActivityCreated)), range.End))
        .OrderBy(nameof(ActivityInfo.ActivityContactID))
        .TopN(batchSize);


    private async Task<int> FireForRange(int inactiveDays, InactivityRange range, int batchSize, CancellationToken cancellationToken)
    {
        int firedCount = 0;
        int lastContactId = 0;
        var lastActivityDate = range.LastActivityDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        while (true)
        {
            var rows = await SelectContactIds(activityInfoProvider.Get(), range, lastContactId, batchSize)
                .GetDataContainerResultAsync(cancellationToken: cancellationToken);
            var contactIds = rows
                .Select(row => Convert.ToInt32(row[nameof(ActivityInfo.ActivityContactID)], CultureInfo.InvariantCulture))
                .ToList();

            if (contactIds.Count > 0)
            {
                var contacts = await contactInfoProvider.Get()
                    .WhereIn(nameof(ContactInfo.ContactID), contactIds)
                    .OrderBy(nameof(ContactInfo.ContactID))
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

                foreach (var contact in contacts)
                {
                    var triggerData = new ContactInactiveTriggerData
                    {
                        InactiveDays = inactiveDays,
                        LastActivityDate = lastActivityDate
                    };

                    await triggerDispatcher.FireTrigger<ContactInactiveTrigger>(new AutomationTriggerDispatch(contact, triggerData), cancellationToken);
                    firedCount++;
                }
            }

            if (contactIds.Count < batchSize)
            {
                return firedCount;
            }

            lastContactId = contactIds[^1];
        }
    }
}
