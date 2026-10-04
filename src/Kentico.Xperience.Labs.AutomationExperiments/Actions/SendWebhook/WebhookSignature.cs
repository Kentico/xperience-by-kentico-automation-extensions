using System.Security.Cryptography;
using System.Text;

namespace Kentico.Xperience.Labs.AutomationExperiments.Actions.SendWebhook;

/// <summary>
/// Computes webhook request signatures that receivers use to verify the sender.
/// </summary>
public static class WebhookSignature
{
    /// <summary>
    /// Returns <c>sha256=&lt;lowercase hex HMAC-SHA256 of body&gt;</c>.
    /// </summary>
    public static string Compute(string secret, string body)
    {
        byte[] hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body));

        return "sha256=" + Convert.ToHexStringLower(hash);
    }
}
