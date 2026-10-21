namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Posts chat messages to named channels from configuration.
/// </summary>
public interface IChatMessageSender
{
    /// <summary>
    /// Posts the message to the named channel. Never throws for configuration, HTTP, or timeout failures – returns a failed
    /// result instead. Cancellation of <paramref name="cancellationToken"/> propagates.
    /// </summary>
    public Task<ChatSendResult> Send(string channelName, ChatMessage message, CancellationToken cancellationToken);
}
