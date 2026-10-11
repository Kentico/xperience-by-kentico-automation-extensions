using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

using Microsoft.Extensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class ContactInactiveOptionsTests
{
    [Test]
    public void Defaults_NothingConfigured_Return90DaysAndBatchSize500()
    {
        var options = new ContactInactiveOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.InactiveDays, Is.Empty);
            Assert.That(options.GetInactiveDays(), Is.EqualTo(new[] { 90 }));
            Assert.That(options.BatchSize, Is.EqualTo(500));
        });
    }


    [Test]
    public void GetInactiveDays_InvalidAndDuplicateValues_ReturnsValidSortedDistinct()
    {
        var options = new ContactInactiveOptions { InactiveDays = [90, 0, -5, 7, 3650, 7, 3651, 1] };

        Assert.That(options.GetInactiveDays(), Is.EqualTo(new[] { 1, 7, 90, 3650 }));
    }


    [Test]
    public void GetInactiveDays_OnlyInvalidValues_ReturnsDefault()
    {
        var options = new ContactInactiveOptions { InactiveDays = [0, 4000] };

        Assert.That(options.GetInactiveDays(), Is.EqualTo(new[] { 90 }));
    }


    [Test]
    public void Bind_ConfiguredValues_ReplaceDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AutomationExtensions:ContactInactive:InactiveDays:0"] = "30",
                ["AutomationExtensions:ContactInactive:InactiveDays:1"] = "7",
                ["AutomationExtensions:ContactInactive:BatchSize"] = "100"
            })
            .Build();

        var options = configuration.GetSection(AutomationExtensionsOptions.SECTION_NAME).Get<AutomationExtensionsOptions>()!;

        Assert.Multiple(() =>
        {
            Assert.That(options.ContactInactive.GetInactiveDays(), Is.EqualTo(new[] { 7, 30 }));
            Assert.That(options.ContactInactive.BatchSize, Is.EqualTo(100));
        });
    }


    [Test]
    public void Bind_SectionMissing_KeepsDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AutomationExtensions:EnabledByDefault"] = "true" })
            .Build();

        var options = configuration.GetSection(AutomationExtensionsOptions.SECTION_NAME).Get<AutomationExtensionsOptions>()!;

        Assert.That(options.ContactInactive.GetInactiveDays(), Is.EqualTo(new[] { 90 }));
    }
}
