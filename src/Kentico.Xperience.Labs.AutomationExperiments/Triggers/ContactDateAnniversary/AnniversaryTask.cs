using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using CMS.Scheduler;

using Kentico.Xperience.Labs.AutomationExperiments.Configuration;
using Kentico.Xperience.Labs.AutomationExperiments.Core;
using Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(AnniversaryTask.IDENTIFIER, typeof(AnniversaryTask))]

namespace Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

/// <summary>
/// Fires <see cref="ContactDateAnniversaryTrigger"/> for contacts whose date field anniversary falls on today plus each configured
/// day offset. Requires a task configuration in the Scheduled tasks application – run it once per day.
/// </summary>
/// <remarks>
/// <para>
/// Fired triggers enter the shared in-memory trigger queue. Contacts are loaded in batches; processes decide via
/// <see cref="ContactDateAnniversaryTrigger.Evaluate"/> whether they start.
/// </para>
/// </remarks>
internal sealed class AnniversaryTask(
    IAutomationTriggerDispatcher triggerDispatcher,
    IInfoProvider<ContactInfo> contactInfoProvider,
    IAutomationComponentStatus componentStatus,
    DateFieldResolver fieldResolver,
    IOptionsMonitor<AutomationExperimentsOptions> options,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : IScheduledTask
{
    /// <summary>
    /// Task identifier selected in the Scheduled tasks application. Persisted in task configurations – never change.
    /// </summary>
    public const string IDENTIFIER = ContactDateAnniversaryTrigger.IDENTIFIER + "Task";


    private readonly IAutomationTriggerDispatcher triggerDispatcher = triggerDispatcher;
    private readonly IInfoProvider<ContactInfo> contactInfoProvider = contactInfoProvider;
    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly DateFieldResolver fieldResolver = fieldResolver;
    private readonly IOptionsMonitor<AutomationExperimentsOptions> options = options;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.ContactDateAnniversary));


    /// <inheritdoc />
    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        var component = AutomationComponentCatalog.ContactDateAnniversary;
        if (!componentStatus.IsEnabled(component))
        {
            return new ScheduledTaskExecutionResult($"Disabled via configuration ('{component.ConfigurationPath}'). No triggers fired.");
        }

        var fields = fieldResolver.GetFields();
        if (fields.Count == 0)
        {
            return new ScheduledTaskExecutionResult(
                $"No valid contact date fields configured in '{AutomationExperimentsOptions.SECTION_NAME}:{nameof(AutomationExperimentsOptions.ContactDateAnniversary)}:{nameof(ContactDateAnniversaryOptions.DateFields)}'.");
        }

        var anniversaryOptions = options.CurrentValue.ContactDateAnniversary;
        int batchSize = Math.Max(1, anniversaryOptions.BatchSize);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        int firedCount = 0;

        foreach (var field in fields)
        {
            foreach (int daysBefore in anniversaryOptions.GetDaysBefore())
            {
                var anniversaryDate = today.AddDays(daysBefore);
                foreach (var (month, day) in AnniversaryCalendar.GetMatchingMonthDays(anniversaryDate))
                {
                    firedCount += await FireForMonthDay(field.Name, month, day, daysBefore, anniversaryDate, batchSize, cancellationToken);
                }
            }
        }

        logger.LogInformation(LogEvents.AnniversaryTaskCompleted, "Contact date anniversary task fired {FiredCount} trigger(s) for {Today}.", firedCount, today);

        return ScheduledTaskExecutionResult.Success;
    }


    private async Task<int> FireForMonthDay(
        string fieldName,
        int month,
        int day,
        int daysBefore,
        DateOnly anniversaryDate,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int firedCount = 0;
        int lastContactId = 0;

        while (true)
        {
            var contacts = (await contactInfoProvider.Get()
                .WhereNotNull(fieldName)
                .WhereEquals($"MONTH([{fieldName}])".AsExpression(), month)
                .WhereEquals($"DAY([{fieldName}])".AsExpression(), day)
                .WhereGreaterThan(nameof(ContactInfo.ContactID), lastContactId)
                .OrderBy(nameof(ContactInfo.ContactID))
                .TopN(batchSize)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                .ToList();

            foreach (var contact in contacts)
            {
                var originalDate = DateOnly.FromDateTime(contact.GetDateTimeValue(fieldName, DateTime.MinValue));
                int? years = AnniversaryCalendar.GetYears(originalDate, anniversaryDate);
                if (years is null)
                {
                    continue;
                }

                var triggerData = new ContactDateAnniversaryTriggerData
                {
                    FieldName = fieldName,
                    DaysBefore = daysBefore,
                    AnniversaryDate = anniversaryDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    Years = years.Value
                };

                await triggerDispatcher.FireTrigger<ContactDateAnniversaryTrigger>(new AutomationTriggerDispatch(contact, triggerData), cancellationToken);
                firedCount++;
            }

            if (contacts.Count < batchSize)
            {
                return firedCount;
            }

            lastContactId = contacts[^1].ContactID;
        }
    }
}
