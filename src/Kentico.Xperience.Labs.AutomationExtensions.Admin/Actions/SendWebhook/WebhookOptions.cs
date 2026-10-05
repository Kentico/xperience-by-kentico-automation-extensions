namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// Settings of the <see cref="SendWebhookAction"/>.
/// </summary>
public sealed class WebhookOptions
{
    /// <summary>
    /// Named webhook endpoints marketers can select in the step configuration, keyed by endpoint name.
    /// Endpoint URLs and secrets stay in configuration and are never stored in automation step properties.
    /// </summary>
    public Dictionary<string, WebhookEndpointOptions> Endpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
