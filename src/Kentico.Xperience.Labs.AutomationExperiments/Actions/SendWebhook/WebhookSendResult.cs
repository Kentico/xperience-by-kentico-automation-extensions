namespace Kentico.Xperience.Labs.AutomationExperiments.Actions.SendWebhook;

/// <summary>
/// Outcome of a webhook request.
/// </summary>
/// <param name="Succeeded">Indicates whether the endpoint returned a success status code.</param>
/// <param name="Message">Short, non-personal description of the outcome.</param>
/// <param name="Exception">Exception that caused the failure, if any. Logged by the caller.</param>
public sealed record WebhookSendResult(bool Succeeded, string Message, Exception? Exception = null);
