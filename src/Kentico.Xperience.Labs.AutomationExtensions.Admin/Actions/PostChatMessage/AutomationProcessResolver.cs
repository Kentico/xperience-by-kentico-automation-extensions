using CMS.AutomationEngine.Internal;
using CMS.DataEngine;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <inheritdoc />
/// <remarks>
/// Automation processes are <see cref="WorkflowInfo"/> objects with <see cref="WorkflowTypeEnum.Automation"/> (verified in
/// 31.9.3). The admin requires a display name but doesn't keep it unique (only the code name is unique), so several
/// processes can share one – then no process is returned. The database compares with its collation (usually
/// case-insensitive); the result is then filtered by ordinal comparison, because the context holds the name as stored.
/// </remarks>
internal sealed class AutomationProcessResolver : IAutomationProcessResolver
{
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<(int Id, string DisplayName)>>> findCandidates;
    private readonly ILogger logger;


    public AutomationProcessResolver(IInfoProvider<WorkflowInfo> workflowProvider, ILoggerFactory loggerFactory)
        : this((displayName, cancellationToken) => FindCandidates(workflowProvider, displayName, cancellationToken), loggerFactory)
    {
    }


    /// <summary>
    /// Uses the given candidate source (tests run without an Xperience container).
    /// </summary>
    internal AutomationProcessResolver(
        Func<string, CancellationToken, Task<IReadOnlyList<(int Id, string DisplayName)>>> findCandidates,
        ILoggerFactory loggerFactory)
    {
        this.findCandidates = findCandidates;
        logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.PostChatMessage));
    }


    /// <inheritdoc />
    public async Task<int?> FindIdByDisplayName(string? displayName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var matches = (await findCandidates(displayName, cancellationToken))
            .Where(candidate => string.Equals(candidate.DisplayName, displayName, StringComparison.Ordinal))
            .Select(candidate => candidate.Id)
            .Distinct()
            .ToList();

        if (matches.Count == 1)
        {
            return matches[0];
        }

        logger.LogDebug(
            LogEvents.ChatMessageProcessLinkSkipped,
            "No link to automation process '{ProcessName}': {MatchCount} automation processes have this display name.",
            displayName,
            matches.Count);

        return null;
    }


    private static async Task<IReadOnlyList<(int Id, string DisplayName)>> FindCandidates(
        IInfoProvider<WorkflowInfo> workflowProvider,
        string displayName,
        CancellationToken cancellationToken)
    {
        var processes = await workflowProvider.Get()
            .WhereEquals(nameof(WorkflowInfo.WorkflowType), (int)WorkflowTypeEnum.Automation)
            .WhereEquals(nameof(WorkflowInfo.WorkflowDisplayName), displayName)
            .Columns(nameof(WorkflowInfo.WorkflowID), nameof(WorkflowInfo.WorkflowDisplayName))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return processes.Select(process => (process.WorkflowID, process.WorkflowDisplayName)).ToList();
    }
}
