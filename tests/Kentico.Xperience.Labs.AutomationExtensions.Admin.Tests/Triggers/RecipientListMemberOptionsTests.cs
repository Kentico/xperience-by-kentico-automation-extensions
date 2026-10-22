using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

using Microsoft.Extensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class RecipientListMemberOptionsTests
{
    [Test]
    public void Defaults_NothingConfigured_NoListsAndBatchSize500()
    {
        var options = new RecipientListMemberOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.RecipientLists, Is.Empty);
            Assert.That(options.GetRecipientLists(), Is.Empty);
            Assert.That(options.BatchSize, Is.EqualTo(500));
        });
    }


    [Test]
    public void GetRecipientLists_TrimsAndRemovesEmptyAndDuplicateEntries()
    {
        var options = new RecipientListMemberOptions { RecipientLists = [" Newsletter ", "", "   ", "Events", "newsletter", "Events"] };

        Assert.That(options.GetRecipientLists(), Is.EqualTo(new[] { "Newsletter", "Events" }));
    }


    [Test]
    public void RecipientListsConfigurationPath_PointsToSetting() =>
        Assert.That(RecipientListMemberOptions.RECIPIENT_LISTS_CONFIGURATION_PATH, Is.EqualTo("AutomationExtensions:RecipientListMember:RecipientLists"));


    [Test]
    public void Bind_ConfiguredValues_ReplaceDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AutomationExtensions:RecipientListMember:RecipientLists:0"] = "DancingGoatNewsletter",
                ["AutomationExtensions:RecipientListMember:RecipientLists:1"] = "Events",
                ["AutomationExtensions:RecipientListMember:BatchSize"] = "100"
            })
            .Build();

        var options = configuration.GetSection(AutomationExtensionsOptions.SECTION_NAME).Get<AutomationExtensionsOptions>()!;

        Assert.Multiple(() =>
        {
            Assert.That(options.RecipientListMember.GetRecipientLists(), Is.EqualTo(new[] { "DancingGoatNewsletter", "Events" }));
            Assert.That(options.RecipientListMember.BatchSize, Is.EqualTo(100));
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
            Assert.That(options.RecipientListMember.GetRecipientLists(), Is.Empty);
            Assert.That(options.RecipientListMember.BatchSize, Is.EqualTo(500));
        });
    }
}
