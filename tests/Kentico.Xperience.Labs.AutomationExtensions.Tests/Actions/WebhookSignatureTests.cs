using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class WebhookSignatureTests
{
    [Test]
    public void Compute_KnownVector_ReturnsPrefixedLowercaseHex()
    {
        string signature = WebhookSignature.Compute("key", "The quick brown fox jumps over the lazy dog");

        Assert.That(signature, Is.EqualTo("sha256=f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8"));
    }
}
