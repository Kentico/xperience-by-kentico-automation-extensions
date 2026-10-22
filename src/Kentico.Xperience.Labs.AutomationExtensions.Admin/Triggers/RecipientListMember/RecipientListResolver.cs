using CMS.ContactManagement;
using CMS.DataEngine;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Resolves the configured recipient list code names (<see cref="RecipientListMemberOptions.GetRecipientLists"/>) to
/// existing recipient lists. Single source of selectable/scanned lists for the drop-down and the scheduled task.
/// </summary>
/// <remarks>
/// Recipient lists are rows of the contact group table with <see cref="ContactGroupInfo.ContactGroupIsRecipientList"/> set.
/// Rows are loaded like for the contact group trigger (<see cref="ContactGroupResolver.LoadCandidates"/>), so contact groups
/// and other object types (segments) are told apart from recipient lists and unknown names.
/// </remarks>
internal sealed class RecipientListResolver
{
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options;
    private readonly Func<IReadOnlyList<string>, CancellationToken, Task<IEnumerable<ContactGroupCandidate>>> loadCandidates;


    public RecipientListResolver(IInfoProvider<ContactGroupInfo> contactGroupInfoProvider, IOptionsMonitor<AutomationExtensionsOptions> options)
        : this(options, (codeNames, cancellationToken) => ContactGroupResolver.LoadCandidates(contactGroupInfoProvider, codeNames, cancellationToken))
    {
    }


    /// <summary>
    /// Creates a resolver with a custom candidate source (tests).
    /// </summary>
    internal RecipientListResolver(
        IOptionsMonitor<AutomationExtensionsOptions> options,
        Func<IReadOnlyList<string>, CancellationToken, Task<IEnumerable<ContactGroupCandidate>>> loadCandidates)
    {
        this.options = options;
        this.loadCandidates = loadCandidates;
    }


    /// <summary>
    /// Returns the configured code names (trimmed, distinct). Empty when nothing is configured.
    /// </summary>
    public IReadOnlyList<string> GetConfiguredNames() => options.CurrentValue.RecipientListMember.GetRecipientLists();


    /// <summary>
    /// Loads and filters the configured recipient lists. Doesn't query the database when nothing is configured.
    /// </summary>
    public async Task<RecipientListResolution> Resolve(CancellationToken cancellationToken)
    {
        var codeNames = GetConfiguredNames();
        if (codeNames.Count == 0)
        {
            return RecipientListResolution.Empty;
        }

        var candidates = await loadCandidates(codeNames, cancellationToken);

        return Resolve(codeNames, candidates);
    }


    /// <summary>
    /// Matches configured code names (case-insensitive) to loaded candidates and sorts out unknown names and rows that aren't
    /// recipient lists. Keeps configuration order. A recipient list wins over other rows with the same code name.
    /// </summary>
    internal static RecipientListResolution Resolve(IReadOnlyList<string> codeNames, IEnumerable<ContactGroupCandidate> candidates)
    {
        var byName = candidates
            .GroupBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(candidate => candidate.Kind == ContactGroupCandidateKind.RecipientList ? 0 : 1).First(),
                StringComparer.OrdinalIgnoreCase);

        var lists = new List<ResolvedContactGroup>();
        var notFound = new List<string>();
        var notRecipientLists = new List<string>();

        foreach (string codeName in codeNames)
        {
            if (!byName.TryGetValue(codeName, out var candidate))
            {
                notFound.Add(codeName);
            }
            else if (candidate.Kind == ContactGroupCandidateKind.RecipientList)
            {
                string displayName = string.IsNullOrWhiteSpace(candidate.DisplayName) ? candidate.Name : candidate.DisplayName;
                lists.Add(new ResolvedContactGroup(candidate.Id, candidate.Name, displayName));
            }
            else
            {
                notRecipientLists.Add(codeName);
            }
        }

        return new RecipientListResolution(lists, notFound, notRecipientLists);
    }
}
