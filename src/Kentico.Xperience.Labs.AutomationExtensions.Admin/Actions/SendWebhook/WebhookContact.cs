namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// Contact section of the <see cref="WebhookPayload"/>. Personal fields are included only when enabled on the step.
/// </summary>
public sealed class WebhookContact
{
    /// <summary>
    /// Contact GUID. Always included.
    /// </summary>
    public Guid Guid { get; init; }


    public string? Email { get; init; }


    public string? FirstName { get; init; }


    public string? LastName { get; init; }


    public string? CompanyName { get; init; }
}
