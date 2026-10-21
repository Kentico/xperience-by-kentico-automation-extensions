using System.Globalization;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Scheduler;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(ContactGroupMemberTask.IDENTIFIER, typeof(ContactGroupMemberTask))]

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Fires <see cref="ContactGroupMemberTrigger"/> for every current contact member of each configured contact group.
/// Requires a task configuration in the Scheduled tasks application – run it once per day.
/// </summary>
/// <remarks>
/// <para>
/// No state is stored: every run fires for every member, not only for contacts that joined since the last run. Process
/// recurrence (<i>If not already running</i> / <i>Only once</i>) prevents repeated starts. A contact in several configured
/// groups fires once per group; processes decide via <see cref="ContactGroupMemberTrigger.Evaluate"/> whether they start.
/// </para>
/// <para>
/// Fired triggers enter the shared in-memory trigger queue. Members are loaded in batches.
/// </para>
/// </remarks>
internal sealed class ContactGroupMemberTask(
    IAutomationTriggerDispatcher triggerDispatcher,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberInfoProvider,
    IInfoProvider<ContactInfo> contactInfoProvider,
    ContactGroupResolver contactGroupResolver,
    IAutomationComponentStatus componentStatus,
    IOptionsMonitor<AutomationExtensionsOptions> options,
    ILoggerFactory loggerFactory) : IScheduledTask
{
    /// <summary>
    /// Task identifier selected in the Scheduled tasks application. Persisted in task configurations – never change.
    /// </summary>
    public const string IDENTIFIER = ContactGroupMemberTrigger.IDENTIFIER + "Task";


    private readonly IAutomationTriggerDispatcher triggerDispatcher = triggerDispatcher;
    private readonly IInfoProvider<ContactGroupMemberInfo> contactGroupMemberInfoProvider = contactGroupMemberInfoProvider;
    private readonly IInfoProvider<ContactInfo> contactInfoProvider = contactInfoProvider;
    private readonly ContactGroupResolver contactGroupResolver = contactGroupResolver;
    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.ContactGroupMember));


    /// <inheritdoc />
    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        var component = AutomationComponentCatalog.ContactGroupMember;
        if (!componentStatus.IsEnabled(component))
        {
            return new ScheduledTaskExecutionResult($"Disabled via configuration ('{component.ConfigurationPath}'). No triggers fired.");
        }

        if (contactGroupResolver.GetConfiguredNames().Count == 0)
        {
            return new ScheduledTaskExecutionResult(
                $"No contact groups configured in '{ContactGroupMemberOptions.CONTACT_GROUPS_CONFIGURATION_PATH}'. No triggers fired.");
        }

        var resolution = await contactGroupResolver.Resolve(cancellationToken);
        LogResolutionIssues(resolution);

        if (resolution.Groups.Count == 0)
        {
            return new ScheduledTaskExecutionResult(
                $"None of the contact groups configured in '{ContactGroupMemberOptions.CONTACT_GROUPS_CONFIGURATION_PATH}' exist as contact groups (recipient lists aren't supported). No triggers fired.");
        }

        int batchSize = Math.Max(1, options.CurrentValue.ContactGroupMember.BatchSize);
        var groupCounts = new List<string>();
        int firedCount = 0;

        foreach (var group in resolution.Groups)
        {
            int groupCount = await FireForGroup(group, batchSize, cancellationToken);
            firedCount += groupCount;
            groupCounts.Add(string.Create(CultureInfo.InvariantCulture, $"{group.Name}: {groupCount}"));
        }

        logger.LogInformation(
            LogEvents.ContactGroupMemberTaskCompleted,
            "Contact group member task fired {FiredCount} trigger(s) ({GroupCounts}).",
            firedCount,
            string.Join(", ", groupCounts));

        return ScheduledTaskExecutionResult.Success;
    }


    /// <summary>
    /// Selects one batch of contact member IDs of the group, ordered by contact ID after <paramref name="lastContactId"/>
    /// (keyset paging). All values are query parameters. Generic so the shape can be tested on a plain <see cref="DataQuery"/>
    /// (creating an <see cref="ObjectQuery{TObject}"/> of members needs a database).
    /// </summary>
    internal static TQuery SelectContactIds<TQuery>(TQuery query, int contactGroupId, int lastContactId, int batchSize)
        where TQuery : DataQueryBase<TQuery>, new() => query
        .Columns(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID))
        .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), contactGroupId)
        .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), (int)ContactGroupMemberTypeEnum.Contact)
        .WhereGreaterThan(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), lastContactId)
        .OrderBy(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID))
        .TopN(batchSize);


    private void LogResolutionIssues(ContactGroupResolution resolution)
    {
        foreach (string codeName in resolution.NotFound)
        {
            logger.LogWarning(LogEvents.ContactGroupNotFound,
                "Configured contact group '{ContactGroupName}' does not exist and is ignored.", codeName);
        }

        foreach (string codeName in resolution.RecipientLists)
        {
            logger.LogWarning(LogEvents.ContactGroupIsRecipientList,
                "Configured contact group '{ContactGroupName}' is a recipient list and is ignored. Only contact groups are supported.", codeName);
        }

        foreach (string codeName in resolution.OtherTypes)
        {
            logger.LogWarning(LogEvents.ContactGroupNotFound,
                "Configured contact group '{ContactGroupName}' is not a contact group (e.g. a segment) and is ignored. Only contact groups are supported.", codeName);
        }
    }


    private async Task<int> FireForGroup(ResolvedContactGroup group, int batchSize, CancellationToken cancellationToken)
    {
        int firedCount = 0;
        int lastContactId = 0;

        while (true)
        {
            var rows = await SelectContactIds(contactGroupMemberInfoProvider.Get(), group.Id, lastContactId, batchSize)
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
                    var triggerData = new ContactGroupMemberTriggerData
                    {
                        ContactGroupName = group.Name,
                        ContactGroupDisplayName = group.DisplayName
                    };

                    await triggerDispatcher.FireTrigger<ContactGroupMemberTrigger>(new AutomationTriggerDispatch(contact, triggerData), cancellationToken);
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
