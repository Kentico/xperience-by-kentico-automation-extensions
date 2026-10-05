namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// Settings of a single named webhook endpoint.
/// </summary>
public sealed class WebhookEndpointOptions
{
    /// <summary>
    /// Default name of the HTTP header carrying the request signature.
    /// </summary>
    public const string DEFAULT_SIGNATURE_HEADER_NAME = "X-Automation-Signature";


    /// <summary>
    /// Absolute URL the payload is posted to.
    /// </summary>
    public string Url { get; set; } = string.Empty;


    /// <summary>
    /// Name displayed to marketers in the endpoint selector. Falls back to the endpoint name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;


    /// <summary>
    /// Optional shared secret. When set, the request body is signed with HMAC-SHA256 and the signature is sent
    /// in the <see cref="SignatureHeaderName"/> header as <c>sha256=&lt;hex&gt;</c>.
    /// Store in user secrets, environment variables, or a key vault – not in committed appsettings files.
    /// </summary>
    public string Secret { get; set; } = string.Empty;


    /// <summary>
    /// Name of the HTTP header carrying the signature.
    /// </summary>
    public string SignatureHeaderName { get; set; } = DEFAULT_SIGNATURE_HEADER_NAME;


    /// <summary>
    /// Request timeout in seconds. Kept well below the 2-minute automation step limit.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
