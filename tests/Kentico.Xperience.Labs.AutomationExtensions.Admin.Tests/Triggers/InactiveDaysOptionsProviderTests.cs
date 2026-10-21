using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class InactiveDaysOptionsProviderTests
{
    [Test]
    public async Task GetOptionItems_ListsEffectiveValuesWithLocalizedText()
    {
        var options = new AutomationExtensionsOptions { ContactInactive = { InactiveDays = [90, 7, 0, 7] } };
        var provider = new InactiveDaysOptionsProvider(new TestOptionsMonitor<AutomationExtensionsOptions>(options), new ResourceLocalizationService());

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(new[] { "7", "90" }));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[] { "7 days", "90 days" }));
        });
    }


    [Test]
    public async Task GetOptionItems_NothingConfigured_ListsDefault()
    {
        var provider = new InactiveDaysOptionsProvider(
            new TestOptionsMonitor<AutomationExtensionsOptions>(new AutomationExtensionsOptions()), new ResourceLocalizationService());

        var items = (await provider.GetOptionItems()).ToList();

        Assert.That(items.Select(item => (item.Value, item.Text)), Is.EqualTo(new[] { ("90", "90 days") }));
    }
}
