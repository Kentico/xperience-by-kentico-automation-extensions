using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Resolves the configured contact group code names (<see cref="ContactGroupMemberOptions.GetContactGroups"/>) to existing
/// contact groups. Single source of selectable/scanned groups for the drop-down and the scheduled task – replace the
/// configuration source here (e.g. with a settings object managed in the admin UI) without changing its callers.
/// </summary>
/// <remarks>
/// The contact group table also stores recipient lists (<c>om.recipientlist</c>) and, in Xperience 31.9, segments
/// (<c>om.segment</c>). The <c>om.contactgroup</c> object type condition of <see cref="IInfoProvider{ContactGroupInfo}"/>
/// excludes both, so only contact groups are used. Code names not found that way are looked up once more without the type
/// condition to report recipient lists and other object types separately from unknown names.
/// </remarks>
internal sealed class ContactGroupResolver
{
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options;
    private readonly Func<IReadOnlyList<string>, CancellationToken, Task<IEnumerable<ContactGroupCandidate>>> loadCandidates;


    public ContactGroupResolver(IInfoProvider<ContactGroupInfo> contactGroupInfoProvider, IOptionsMonitor<AutomationExtensionsOptions> options)
        : this(options, (codeNames, cancellationToken) => LoadCandidates(contactGroupInfoProvider, codeNames, cancellationToken))
    {
    }


    /// <summary>
    /// Creates a resolver with a custom candidate source (tests).
    /// </summary>
    internal ContactGroupResolver(
        IOptionsMonitor<AutomationExtensionsOptions> options,
        Func<IReadOnlyList<string>, CancellationToken, Task<IEnumerable<ContactGroupCandidate>>> loadCandidates)
    {
        this.options = options;
        this.loadCandidates = loadCandidates;
    }


    /// <summary>
    /// Returns the configured code names (trimmed, distinct). Empty when nothing is configured.
    /// </summary>
    public IReadOnlyList<string> GetConfiguredNames() => options.CurrentValue.ContactGroupMember.GetContactGroups();


    /// <summary>
    /// Loads and filters the configured contact groups. Doesn't query the database when nothing is configured.
    /// </summary>
    public async Task<ContactGroupResolution> Resolve(CancellationToken cancellationToken)
    {
        var codeNames = GetConfiguredNames();
        if (codeNames.Count == 0)
        {
            return ContactGroupResolution.Empty;
        }

        var candidates = await loadCandidates(codeNames, cancellationToken);

        return Resolve(codeNames, candidates);
    }


    /// <summary>
    /// Matches configured code names (case-insensitive) to loaded candidates and sorts out unknown names, recipient lists,
    /// and other object types. Keeps configuration order. A contact group wins over other rows with the same code name.
    /// </summary>
    internal static ContactGroupResolution Resolve(IReadOnlyList<string> codeNames, IEnumerable<ContactGroupCandidate> candidates)
    {
        var byName = candidates
            .GroupBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderBy(candidate => candidate.Kind).First(), StringComparer.OrdinalIgnoreCase);

        var groups = new List<ResolvedContactGroup>();
        var notFound = new List<string>();
        var recipientLists = new List<string>();
        var otherTypes = new List<string>();

        foreach (string codeName in codeNames)
        {
            if (!byName.TryGetValue(codeName, out var candidate))
            {
                notFound.Add(codeName);
                continue;
            }

            if (candidate.Kind == ContactGroupCandidateKind.ContactGroup)
            {
                string displayName = string.IsNullOrWhiteSpace(candidate.DisplayName) ? candidate.Name : candidate.DisplayName;
                groups.Add(new ResolvedContactGroup(candidate.Id, candidate.Name, displayName));
            }
            else if (candidate.Kind == ContactGroupCandidateKind.RecipientList)
            {
                recipientLists.Add(codeName);
            }
            else
            {
                otherTypes.Add(codeName);
            }
        }

        return new ContactGroupResolution(groups, notFound, recipientLists, otherTypes);
    }


    /// <summary>
    /// Selects the contact groups (object type condition applied by the provider query) with the given code names. The
    /// recipient list flag is selected too, so a recipient list is never scanned even if the type condition changes.
    /// </summary>
    internal static ObjectQuery<ContactGroupInfo> SelectContactGroups(ObjectQuery<ContactGroupInfo> query, IReadOnlyList<string> codeNames) => query
        .Columns(
            nameof(ContactGroupInfo.ContactGroupID),
            nameof(ContactGroupInfo.ContactGroupName),
            nameof(ContactGroupInfo.ContactGroupDisplayName),
            nameof(ContactGroupInfo.ContactGroupIsRecipientList))
        .WhereIn(nameof(ContactGroupInfo.ContactGroupName), codeNames.ToList());


    /// <summary>
    /// Selects rows of any object type stored in the contact group table with the given code names (object type condition
    /// turned off), to tell recipient lists from other object types.
    /// </summary>
    internal static ObjectQuery<ContactGroupInfo> SelectOtherTypes(ObjectQuery<ContactGroupInfo> query, IReadOnlyList<string> codeNames)
    {
        query.UseObjectTypeCondition = false;

        return query
            .Columns(
                nameof(ContactGroupInfo.ContactGroupID),
                nameof(ContactGroupInfo.ContactGroupName),
                nameof(ContactGroupInfo.ContactGroupDisplayName),
                nameof(ContactGroupInfo.ContactGroupIsRecipientList))
            .WhereIn(nameof(ContactGroupInfo.ContactGroupName), codeNames.ToList());
    }


    private static async Task<IEnumerable<ContactGroupCandidate>> LoadCandidates(
        IInfoProvider<ContactGroupInfo> contactGroupInfoProvider,
        IReadOnlyList<string> codeNames,
        CancellationToken cancellationToken)
    {
        // Plain rows instead of typed infos: rows of other object types aren't materialized as contact group objects.
        var groupRows = await SelectContactGroups(contactGroupInfoProvider.Get(), codeNames)
            .GetDataContainerResultAsync(cancellationToken: cancellationToken);
        var candidates = groupRows
            .Select(row => CreateCandidate(row, ContactGroupCandidateKind.ContactGroup))
            .ToList();

        var missing = codeNames
            .Where(codeName => !candidates.Exists(candidate => string.Equals(candidate.Name, codeName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (missing.Count == 0)
        {
            return candidates;
        }

        var otherRows = await SelectOtherTypes(contactGroupInfoProvider.Get(), missing)
            .GetDataContainerResultAsync(cancellationToken: cancellationToken);
        candidates.AddRange(otherRows.Select(row => CreateCandidate(row, ContactGroupCandidateKind.Other)));

        return candidates;
    }


    /// <summary>
    /// Creates a candidate of <paramref name="kind"/>, or a recipient list when the row's recipient list flag is set.
    /// </summary>
    private static ContactGroupCandidate CreateCandidate(IDataContainer row, ContactGroupCandidateKind kind) => new(
        ValidationHelper.GetInteger(row[nameof(ContactGroupInfo.ContactGroupID)], 0),
        ValidationHelper.GetString(row[nameof(ContactGroupInfo.ContactGroupName)], string.Empty),
        ValidationHelper.GetString(row[nameof(ContactGroupInfo.ContactGroupDisplayName)], string.Empty),
        ValidationHelper.GetBoolean(row[nameof(ContactGroupInfo.ContactGroupIsRecipientList)], false) ? ContactGroupCandidateKind.RecipientList : kind);
}
