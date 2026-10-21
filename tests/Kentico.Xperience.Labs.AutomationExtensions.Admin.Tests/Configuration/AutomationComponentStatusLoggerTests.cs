using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Configuration;

public class AutomationComponentStatusLoggerTests
{
    [Test]
    public void FormatNames_Empty_ReturnsNone() => Assert.That(AutomationComponentStatusLogger.FormatNames([]), Is.EqualTo("none"));


    [Test]
    public void FormatNames_Names_ReturnsCommaSeparated() => Assert.That(AutomationComponentStatusLogger.FormatNames(["SendWebhook", "WriteToLog"]), Is.EqualTo("SendWebhook, WriteToLog"));
}
