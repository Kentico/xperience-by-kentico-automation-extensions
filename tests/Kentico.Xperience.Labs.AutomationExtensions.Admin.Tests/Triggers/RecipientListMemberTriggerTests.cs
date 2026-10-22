using Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class RecipientListMemberTriggerTests
{
    [TestCase("Newsletter", "Newsletter", ExpectedResult = true)]
    [TestCase("newsletter", "Newsletter", ExpectedResult = true)]
    [TestCase("Newsletter", "Events", ExpectedResult = false)]
    [TestCase("", "Newsletter", ExpectedResult = false)]
    [TestCase("", "", ExpectedResult = false)]
    [TestCase("Newsletter", "", ExpectedResult = false)]
    public bool Matches_ComparesRecipientListCodeNames(string configuredList, string firedList)
    {
        var properties = new RecipientListMemberTriggerProperties { RecipientList = configuredList };
        var triggerData = new RecipientListMemberTriggerData { RecipientListName = firedList };

        return RecipientListMemberTrigger.Matches(properties, triggerData);
    }


    [Test]
    public void Properties_HaveNoDefaultList() => Assert.That(new RecipientListMemberTriggerProperties().RecipientList, Is.Empty);


    [Test]
    public void Identifiers_AreStable()
    {
        string[] identifiers = [RecipientListMemberTrigger.IDENTIFIER, RecipientListMemberTask.IDENTIFIER, new RecipientListMemberTriggerData().Identifier];

        Assert.That(identifiers, Is.EqualTo(new[]
        {
            "Labs.AutomationExtensions.RecipientListMember",
            "Labs.AutomationExtensions.RecipientListMemberTask",
            "Labs.AutomationExtensions.RecipientListMemberTriggerData"
        }));
    }


    [Test]
    public void TriggerData_PropertyNamesAreStable() =>
        Assert.That(typeof(RecipientListMemberTriggerData).GetProperties().Select(property => property.Name),
            Is.EquivalentTo(new[] { "Identifier", "RecipientListName", "RecipientListDisplayName" }));


    [Test]
    public void TriggerData_DescribesRecipientList() =>
        Assert.That(new RecipientListMemberTriggerData { RecipientListName = "Newsletter" }.Describe(), Is.EqualTo("recipient list Newsletter"));
}
