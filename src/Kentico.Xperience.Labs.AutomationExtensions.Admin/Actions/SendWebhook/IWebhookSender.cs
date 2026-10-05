namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// Posts webhook payloads to named endpoints from configuration.
/// </summary>
public interface IWebhookSender
{
    /// <summary>
    /// Posts the payload to the named endpoint. Never throws for HTTP or timeout failures – returns a failed result instead.
    /// </summary>
    public Task<WebhookSendResult> Send(string endpointName, WebhookPayload payload, CancellationToken cancellationToken);
}
