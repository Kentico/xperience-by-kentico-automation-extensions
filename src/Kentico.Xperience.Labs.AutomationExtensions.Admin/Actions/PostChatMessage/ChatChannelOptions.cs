namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Settings of the <see cref="PostChatMessageAction"/>.
/// </summary>
public sealed class ChatChannelOptions
{
    /// <summary>
    /// Optional absolute URL of the Xperience administration root (e.g. <c>https://www.example.com/admin</c>), used for
    /// the link to the contact. Empty – messages contain no link.
    /// </summary>
    public string AdminBaseUrl { get; set; } = string.Empty;


    /// <summary>
    /// Named chat channels marketers can select in the step configuration, keyed by channel name.
    /// Webhook URLs stay in configuration and are never stored in automation step properties.
    /// </summary>
    public Dictionary<string, ChatChannelEndpointOptions> Channels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
