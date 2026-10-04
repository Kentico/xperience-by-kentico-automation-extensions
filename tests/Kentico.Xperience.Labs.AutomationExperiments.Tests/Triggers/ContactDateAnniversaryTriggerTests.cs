using Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Triggers;

public class ContactDateAnniversaryTriggerTests
{
    [TestCase("ContactBirthday", "7", "ContactBirthday", 7, ExpectedResult = true)]
    [TestCase("contactbirthday", "0", "ContactBirthday", 0, ExpectedResult = true)]
    [TestCase("ContactBirthday", "0", "ContactBirthday", 7, ExpectedResult = false)]
    [TestCase("SignupDate", "7", "ContactBirthday", 7, ExpectedResult = false)]
    [TestCase("ContactBirthday", "", "ContactBirthday", 0, ExpectedResult = false)]
    public bool Matches_ComparesFieldAndDaysBefore(string configuredField, string configuredDaysBefore, string firedField, int firedDaysBefore)
    {
        var properties = new ContactDateAnniversaryTriggerProperties { DateField = configuredField, DaysBefore = configuredDaysBefore };
        var triggerData = new ContactDateAnniversaryTriggerData { FieldName = firedField, DaysBefore = firedDaysBefore };

        return ContactDateAnniversaryTrigger.Matches(properties, triggerData);
    }
}
