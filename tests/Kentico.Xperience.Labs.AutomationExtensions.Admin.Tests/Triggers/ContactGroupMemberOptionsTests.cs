using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

using Microsoft.Extensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class ContactGroupMemberOptionsTests
{
    [Test]
    public void Defaults_NothingConfigured_NoGroupsAndBatchSize500()
    {
        var options = new ContactGroupMemberOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.ContactGroups, Is.Empty);
            Assert.That(options.GetContactGroups(), Is.Empty);
            Assert.That(options.BatchSize, Is.EqualTo(500));
        });
    }


    [Test]
    public void GetContactGroups_TrimsAndRemovesEmptyAndDuplicateEntries()
    {
        var options = new ContactGroupMemberOptions { ContactGroups = [" HighIntent ", "", "   ", "Customers", "highintent", "Customers"] };

        Assert.That(options.GetContactGroups(), Is.EqualTo(new[] { "HighIntent", "Customers" }));
    }


    [Test]
    public void ContactGroupsConfigurationPath_PointsToSetting() =>
        Assert.That(ContactGroupMemberOptions.CONTACT_GROUPS_CONFIGURATION_PATH, Is.EqualTo("AutomationExtensions:ContactGroupMember:ContactGroups"));


    [Test]
    public void Bind_ConfiguredValues_ReplaceDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AutomationExtensions:ContactGroupMember:ContactGroups:0"] = "DancingGoat.BusinessCustomers",
                ["AutomationExtensions:ContactGroupMember:ContactGroups:1"] = "HighIntent",
                ["AutomationExtensions:ContactGroupMember:BatchSize"] = "100"
            })
            .Build();

        var options = configuration.GetSection(AutomationExtensionsOptions.SECTION_NAME).Get<AutomationExtensionsOptions>()!;

        Assert.Multiple(() =>
        {
            Assert.That(options.ContactGroupMember.GetContactGroups(), Is.EqualTo(new[] { "DancingGoat.BusinessCustomers", "HighIntent" }));
            Assert.That(options.ContactGroupMember.BatchSize, Is.EqualTo(100));
        });
    }


    [Test]
    public void Bind_SectionMissing_KeepsDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AutomationExtensions:EnabledByDefault"] = "true" })
            .Build();

        var options = configuration.GetSection(AutomationExtensionsOptions.SECTION_NAME).Get<AutomationExtensionsOptions>()!;

        Assert.Multiple(() =>
        {
            Assert.That(options.ContactGroupMember.GetContactGroups(), Is.Empty);
            Assert.That(options.ContactGroupMember.BatchSize, Is.EqualTo(500));
        });
    }
}
