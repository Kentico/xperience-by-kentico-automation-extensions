using System.Globalization;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;
using CMS.Scheduler;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(RecipientListMemberTask.IDENTIFIER, typeof(RecipientListMemberTask))]

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Fires <see cref="RecipientListMemberTrigger"/> for every subscribed contact of each configured recipient list.
/// Requires a task configuration in the Scheduled tasks application – run it once per day.
/// </summary>
/// <remarks>
/// <para>
/// A contact is subscribed when it is a contact member of the list and its latest subscription confirmation
/// (<see cref="EmailSubscriptionConfirmationInfo"/>) for the list is approved – the platform's own rule (verified in 31.9.3:
/// <c>IEmailSubscriptionConfirmationService.IsConfirmed</c>, the <i>Contact is receiving in recipient list</i> macro rule).
/// Double opt-in adds the member and an approved confirmation only after the link is clicked; unsubscribing keeps the member
/// and adds a not-approved confirmation; members added without a confirmation (e.g. by API) count as not subscribed.
/// Bounced contacts are excluded like in the macro rule: a hard bounce, or at least
/// <see cref="BouncedEmailsGlobalOptions.SoftBounceLimit"/> soft bounces for the contact's email address
/// (<see cref="EmailBounceInfo"/>). The limit is read once per run.
/// </para>
/// <para>
/// No state is stored: every run fires for every subscribed contact, not only for new subscribers. Process recurrence
/// (<i>If not already running</i> / <i>Only once</i>) prevents repeated starts. A contact in several configured lists fires
/// once per list. Fired triggers enter the shared in-memory trigger queue. Contacts are loaded in batches.
/// </para>
/// </remarks>
internal sealed class RecipientListMemberTask(
    IAutomationTriggerDispatcher triggerDispatcher,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberInfoProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationInfoProvider,
    IInfoProvider<ContactInfo> contactInfoProvider,
    IInfoProvider<EmailBounceInfo> emailBounceInfoProvider,
    IOptionsMonitor<BouncedEmailsGlobalOptions> bouncedEmailsOptions,
    RecipientListResolver recipientListResolver,
    IAutomationComponentStatus componentStatus,
    IOptionsMonitor<AutomationExtensionsOptions> options,
    ILoggerFactory loggerFactory) : IScheduledTask
{
    /// <summary>
    /// Task identifier selected in the Scheduled tasks application. Persisted in task configurations – never change.
    /// </summary>
    public const string IDENTIFIER = RecipientListMemberTrigger.IDENTIFIER + "Task";


    private const string ROW_NUMBER_COLUMN = "LabsConfirmationRowNumber";


    private readonly IAutomationTriggerDispatcher triggerDispatcher = triggerDispatcher;
    private readonly IInfoProvider<ContactGroupMemberInfo> contactGroupMemberInfoProvider = contactGroupMemberInfoProvider;
    private readonly IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationInfoProvider = subscriptionConfirmationInfoProvider;
    private readonly IInfoProvider<ContactInfo> contactInfoProvider = contactInfoProvider;
    private readonly IInfoProvider<EmailBounceInfo> emailBounceInfoProvider = emailBounceInfoProvider;
    private readonly IOptionsMonitor<BouncedEmailsGlobalOptions> bouncedEmailsOptions = bouncedEmailsOptions;
    private readonly RecipientListResolver recipientListResolver = recipientListResolver;
    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.RecipientListMember));


    /// <inheritdoc />
    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        var component = AutomationComponentCatalog.RecipientListMember;
        if (!componentStatus.IsEnabled(component))
        {
            return new ScheduledTaskExecutionResult($"Disabled via configuration ('{component.ConfigurationPath}'). No triggers fired.");
        }

        if (recipientListResolver.GetConfiguredNames().Count == 0)
        {
            return new ScheduledTaskExecutionResult(
                $"No recipient lists configured in '{RecipientListMemberOptions.RECIPIENT_LISTS_CONFIGURATION_PATH}'. No triggers fired.");
        }

        var resolution = await recipientListResolver.Resolve(cancellationToken);
        LogResolutionIssues(resolution);

        if (resolution.Lists.Count == 0)
        {
            return new ScheduledTaskExecutionResult(
                $"None of the recipient lists configured in '{RecipientListMemberOptions.RECIPIENT_LISTS_CONFIGURATION_PATH}' exist as recipient lists (contact groups aren't supported). No triggers fired.");
        }

        int batchSize = Math.Max(1, options.CurrentValue.RecipientListMember.BatchSize);
        int softBounceLimit = bouncedEmailsOptions.CurrentValue.SoftBounceLimit;
        var listCounts = new List<string>();
        int firedCount = 0;

        foreach (var list in resolution.Lists)
        {
            int listCount = await FireForList(list, batchSize, softBounceLimit, cancellationToken);
            firedCount += listCount;
            listCounts.Add(string.Create(CultureInfo.InvariantCulture, $"{list.Name}: {listCount}"));
        }

        logger.LogInformation(
            LogEvents.RecipientListMemberTaskCompleted,
            "Recipient list member task fired {FiredCount} trigger(s) ({ListCounts}).",
            firedCount,
            string.Join(", ", listCounts));

        return ScheduledTaskExecutionResult.Success;
    }


    /// <summary>
    /// Selects the IDs of contacts whose latest subscription confirmation for the recipient list is approved (latest by
    /// date, then by ID). Mirrors the subscription part of the platform's <i>Contact is receiving in recipient list</i> macro
    /// rule. Used as a sub-query of the member query. Generic so the shape can be tested on a plain <see cref="DataQuery"/>.
    /// </summary>
    internal static TQuery SelectApprovedContactIds<TQuery>(TQuery query, int recipientListId)
        where TQuery : DataQueryBase<TQuery>, new() => query
        .Columns(
            nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID),
            nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationIsApproved))
        .AddColumn(new RowNumberColumn(
            ROW_NUMBER_COLUMN,
            $"{nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate)} DESC, {nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationID)} DESC")
        {
            PartitionBy = nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID)
        })
        .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), recipientListId)
        .AsNested<TQuery>()
        .WhereEquals(ROW_NUMBER_COLUMN, 1)
        .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationIsApproved), true)
        .Column(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID));


    /// <summary>
    /// Selects the bounced email addresses: a hard bounce, or a soft bounce count of at least <paramref name="softBounceLimit"/>.
    /// Same condition as the platform's <i>Contact is receiving in recipient list</i> macro rule and the recipient list's
    /// <i>Bounced</i> status (no special meaning for a limit of 0 or less). Contacts with these addresses are excluded by the
    /// member query (the macro rule joins the same rows by <c>ContactEmail</c>). Generic so the shape can be tested on a plain
    /// <see cref="DataQuery"/>.
    /// </summary>
    internal static TQuery SelectBouncedEmailAddresses<TQuery>(TQuery query, int softBounceLimit)
        where TQuery : DataQueryBase<TQuery>, new() => query
        .Where(new WhereCondition()
            .WhereEquals(nameof(EmailBounceInfo.EmailBounceIsHardBounce), true)
            .Or()
            .WhereGreaterOrEquals(nameof(EmailBounceInfo.EmailBounceSoftBounceCount), softBounceLimit))
        .Column(nameof(EmailBounceInfo.EmailBounceEmailAddress));


    private void LogResolutionIssues(RecipientListResolution resolution)
    {
        foreach (string codeName in resolution.NotFound)
        {
            logger.LogWarning(LogEvents.RecipientListNotFound,
                "Configured recipient list '{RecipientListName}' does not exist and is ignored.", codeName);
        }

        foreach (string codeName in resolution.NotRecipientLists)
        {
            logger.LogWarning(LogEvents.RecipientListIsContactGroup,
                "Configured recipient list '{RecipientListName}' is not a recipient list (e.g. a contact group) and is ignored. Use the 'Contact in contact group (daily)' trigger for contact groups.", codeName);
        }
    }


    private async Task<int> FireForList(ResolvedContactGroup list, int batchSize, int softBounceLimit, CancellationToken cancellationToken)
    {
        int firedCount = 0;
        int lastContactId = 0;

        while (true)
        {
            // Contact members of the list (keyset batch, same shape as for contact groups) that are subscribed and not bounced.
            // Both filters are part of the batch query, so TopN counts only fired contacts and paging always progresses.
            var subscribed = SelectApprovedContactIds(subscriptionConfirmationInfoProvider.Get(), list.Id);
            var bounced = contactInfoProvider.Get()
                .WhereIn(nameof(ContactInfo.ContactEmail), SelectBouncedEmailAddresses(emailBounceInfoProvider.Get(), softBounceLimit))
                .Column(nameof(ContactInfo.ContactID));
            var rows = await ContactGroupMemberTask.SelectContactIds(contactGroupMemberInfoProvider.Get(), list.Id, lastContactId, batchSize)
                .WhereIn(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), subscribed)
                .WhereNotIn(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), bounced)
                .GetDataContainerResultAsync(cancellationToken: cancellationToken);
            var contactIds = rows
                .Select(row => Convert.ToInt32(row[nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID)], CultureInfo.InvariantCulture))
                .ToList();

            if (contactIds.Count > 0)
            {
                var contacts = await contactInfoProvider.Get()
                    .WhereIn(nameof(ContactInfo.ContactID), contactIds)
                    .OrderBy(nameof(ContactInfo.ContactID))
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

                foreach (var contact in contacts)
                {
                    var triggerData = new RecipientListMemberTriggerData
                    {
                        RecipientListName = list.Name,
                        RecipientListDisplayName = list.DisplayName
                    };

                    await triggerDispatcher.FireTrigger<RecipientListMemberTrigger>(new AutomationTriggerDispatch(contact, triggerData), cancellationToken);
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
