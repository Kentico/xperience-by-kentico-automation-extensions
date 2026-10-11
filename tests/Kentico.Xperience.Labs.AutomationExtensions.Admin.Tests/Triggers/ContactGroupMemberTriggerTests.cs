using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class ContactGroupMemberTriggerTests
{
    [TestCase("HighIntent", "HighIntent", ExpectedResult = true)]
    [TestCase("highintent", "HighIntent", ExpectedResult = true)]
    [TestCase("HighIntent", "Customers", ExpectedResult = false)]
    [TestCase("", "HighIntent", ExpectedResult = false)]
    [TestCase("", "", ExpectedResult = false)]
    [TestCase("HighIntent", "", ExpectedResult = false)]
    public bool Matches_ComparesContactGroupCodeNames(string configuredGroup, string firedGroup)
    {
        var properties = new ContactGroupMemberTriggerProperties { ContactGroup = configuredGroup };
        var triggerData = new ContactGroupMemberTriggerData { ContactGroupName = firedGroup };

        return ContactGroupMemberTrigger.Matches(properties, triggerData);
    }


    [Test]
    public void Properties_HaveNoDefaultGroup() => Assert.That(new ContactGroupMemberTriggerProperties().ContactGroup, Is.Empty);


    [Test]
    public void Identifiers_AreStable()
    {
        string[] identifiers = [ContactGroupMemberTrigger.IDENTIFIER, ContactGroupMemberTask.IDENTIFIER, new ContactGroupMemberTriggerData().Identifier];

        Assert.That(identifiers, Is.EqualTo(new[]
        {
            "Labs.AutomationExtensions.ContactGroupMember",
            "Labs.AutomationExtensions.ContactGroupMemberTask",
            "Labs.AutomationExtensions.ContactGroupMemberTriggerData"
        }));
    }


    [Test]
    public void TriggerData_PropertyNamesAreStable() =>
        Assert.That(typeof(ContactGroupMemberTriggerData).GetProperties().Select(property => property.Name),
            Is.EquivalentTo(new[] { "Identifier", "ContactGroupName", "ContactGroupDisplayName" }));
}
