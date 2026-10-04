using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Core;

public class StepResultKeysTests
{
    [TestCase("SendWebhook", "1", ExpectedResult = "SendWebhook.1")]
    [TestCase("Acme.SyncToCrm", "5", ExpectedResult = "Acme.SyncToCrm.5")]
    [TestCase("SendWebhook", "0", ExpectedResult = null)]
    [TestCase("SendWebhook", "6", ExpectedResult = null)]
    [TestCase("SendWebhook", "", ExpectedResult = null)]
    [TestCase("SendWebhook", "one", ExpectedResult = null)]
    [TestCase("", "1", ExpectedResult = null)]
    [TestCase(null, "1", ExpectedResult = null)]
    public string? Create_ReturnsKeyOnlyForValidInput(string? sourceName, string? resultNumber) =>
        StepResultKeys.Create(sourceName, resultNumber);


    [Test]
    public void SendWebhook_NameMatchesCatalog()
    {
        Assert.That(StepResultKeys.SendWebhook.Name, Is.EqualTo(AutomationComponentCatalog.SendWebhook.Name));
    }
}
