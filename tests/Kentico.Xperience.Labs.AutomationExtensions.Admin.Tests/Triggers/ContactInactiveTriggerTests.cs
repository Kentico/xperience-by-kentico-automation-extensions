using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class ContactInactiveTriggerTests
{
    [TestCase("90", 90, ExpectedResult = true)]
    [TestCase("7", 7, ExpectedResult = true)]
    [TestCase("7", 90, ExpectedResult = false)]
    [TestCase("", 90, ExpectedResult = false)]
    [TestCase("ninety", 90, ExpectedResult = false)]
    [TestCase("9 0", 90, ExpectedResult = false)]
    public bool Matches_ComparesInactiveDays(string configuredDays, int firedDays)
    {
        var properties = new ContactInactiveTriggerProperties { InactiveDays = configuredDays };
        var triggerData = new ContactInactiveTriggerData { InactiveDays = firedDays };

        return ContactInactiveTrigger.Matches(properties, triggerData);
    }


    [Test]
    public void Properties_DefaultTo90Days() => Assert.That(new ContactInactiveTriggerProperties().InactiveDays, Is.EqualTo("90"));


    [Test]
    public void Identifiers_AreStable()
    {
        string[] identifiers = [ContactInactiveTrigger.IDENTIFIER, ContactInactiveTask.IDENTIFIER, new ContactInactiveTriggerData().Identifier];

        Assert.That(identifiers, Is.EqualTo(new[]
        {
            "Labs.AutomationExtensions.ContactInactive",
            "Labs.AutomationExtensions.ContactInactiveTask",
            "Labs.AutomationExtensions.ContactInactiveTriggerData"
        }));
    }
}
