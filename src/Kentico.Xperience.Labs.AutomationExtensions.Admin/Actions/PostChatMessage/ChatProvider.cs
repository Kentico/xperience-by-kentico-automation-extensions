namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Chat service a channel posts to. Selects the payload format.
/// </summary>
public enum ChatProvider
{
    /// <summary>
    /// Microsoft Teams Workflows webhook ("When a Teams webhook request is received"). Receives an Adaptive Card.
    /// </summary>
    Teams,

    /// <summary>
    /// Slack incoming webhook (<c>https://hooks.slack.com/services/...</c>). Receives Block Kit blocks.
    /// </summary>
    Slack
}
