namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Settings of a single named chat channel.
/// </summary>
public sealed class ChatChannelEndpointOptions
{
    /// <summary>
    /// Chat service the <see cref="Url"/> belongs to.
    /// </summary>
    public ChatProvider Provider { get; set; }


    /// <summary>
    /// Absolute HTTPS webhook URL. Anyone with the URL can post to the channel, so treat it as a secret:
    /// store it in user secrets, environment variables, or a key vault – not in committed appsettings files.
    /// </summary>
    public string Url { get; set; } = string.Empty;


    /// <summary>
    /// Name displayed to marketers in the channel selector. Falls back to the channel name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;


    /// <summary>
    /// Request timeout in seconds (minimum 1). Kept well below the 2-minute automation step limit.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
