namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// JSON body posted by the <see cref="SendWebhookAction"/>.
/// </summary>
public sealed class WebhookPayload
{
    /// <summary>
    /// Marketer-defined event name (e.g. <c>lead-qualified</c>).
    /// </summary>
    public string EventName { get; init; } = string.Empty;


    /// <summary>
    /// Time the payload was created (UTC).
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }


    /// <summary>
    /// Display name of the automation process that sent the webhook.
    /// </summary>
    public string ProcessName { get; init; } = string.Empty;


    /// <summary>
    /// Identifier of the trigger that started the process.
    /// </summary>
    public string TriggerIdentifier { get; init; } = string.Empty;


    /// <summary>
    /// Contact the process runs for.
    /// </summary>
    public WebhookContact Contact { get; init; } = new();
}
