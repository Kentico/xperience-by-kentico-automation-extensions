namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Finds the ID of an automation process. The automation process context exposes only the process display name, so the
/// process is looked up by that name.
/// </summary>
public interface IAutomationProcessResolver
{
    /// <summary>
    /// Returns the ID of the only automation process whose display name equals <paramref name="displayName"/>
    /// (ordinal, as stored), or <c>null</c> when none or several processes match.
    /// </summary>
    public Task<int?> FindIdByDisplayName(string? displayName, CancellationToken cancellationToken);
}
