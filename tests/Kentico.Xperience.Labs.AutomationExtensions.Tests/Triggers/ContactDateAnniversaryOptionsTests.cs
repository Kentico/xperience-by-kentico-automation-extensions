using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

using Microsoft.Extensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class ContactDateAnniversaryOptionsTests
{
    [Test]
    public void Getters_NothingConfigured_ReturnDefaults()
    {
        var options = new ContactDateAnniversaryOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.GetDateFields(), Is.EqualTo(new[] { "ContactBirthday" }));
            Assert.That(options.GetDaysBefore(), Is.EqualTo(new[] { 0 }));
        });
    }


    [Test]
    public void GetDaysBefore_InvalidAndDuplicateValues_ReturnsValidSortedDistinct()
    {
        var options = new ContactDateAnniversaryOptions { DaysBefore = [30, -1, 7, 0, 7, 400] };

        Assert.That(options.GetDaysBefore(), Is.EqualTo(new[] { 0, 7, 30 }));
    }


    [Test]
    public void GetDateFields_BlankAndDuplicateValues_ReturnsTrimmedDistinct()
    {
        var options = new ContactDateAnniversaryOptions { DateFields = [" ContactBirthday ", "", "contactbirthday", "SignupDate"] };

        Assert.That(options.GetDateFields(), Is.EqualTo(new[] { "ContactBirthday", "SignupDate" }));
    }


    [Test]
    public void Bind_ConfiguredValues_ReplaceDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AutomationExtensions:ContactDateAnniversary:DateFields:0"] = "SignupDate",
                ["AutomationExtensions:ContactDateAnniversary:DaysBefore:0"] = "7"
            })
            .Build();

        var options = configuration.GetSection(AutomationExtensionsOptions.SECTION_NAME).Get<AutomationExtensionsOptions>()!;

        Assert.Multiple(() =>
        {
            Assert.That(options.ContactDateAnniversary.GetDateFields(), Is.EqualTo(new[] { "SignupDate" }));
            Assert.That(options.ContactDateAnniversary.GetDaysBefore(), Is.EqualTo(new[] { 7 }));
        });
    }
}
