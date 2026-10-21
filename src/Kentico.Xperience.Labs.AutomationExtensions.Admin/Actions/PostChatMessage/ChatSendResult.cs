namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Outcome of a chat message request.
/// </summary>
/// <param name="Succeeded">Indicates whether the webhook returned a success status code.</param>
/// <param name="Message">Short, non-personal description of the outcome. Never contains the webhook URL.</param>
/// <param name="Exception">Exception that caused the failure, if any. Logged by the caller.</param>
public sealed record ChatSendResult(bool Succeeded, string Message, Exception? Exception = null);
