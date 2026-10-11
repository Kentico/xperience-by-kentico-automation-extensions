using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class ContactGroupOptionsProviderTests
{
    [Test]
    public async Task GetOptionItems_ListsOnlyResolvedGroupsWithDisplayAndCodeName()
    {
        var options = new AutomationExtensionsOptions { ContactGroupMember = { ContactGroups = ["HighIntent", "Unknown", "Newsletter", "Customers"] } };
        var resolver = new ContactGroupResolver(new TestOptionsMonitor<AutomationExtensionsOptions>(options), (_, _) =>
            Task.FromResult<IEnumerable<ContactGroupCandidate>>(
            [
                new(1, "Customers", "Customers", ContactGroupCandidateKind.ContactGroup),
                new(2, "Newsletter", "Newsletter", ContactGroupCandidateKind.RecipientList),
                new(3, "HighIntent", "High intent", ContactGroupCandidateKind.ContactGroup)
            ]));
        var provider = new ContactGroupOptionsProvider(resolver, new ResourceLocalizationService());

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(new[] { "HighIntent", "Customers" }));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[] { "High intent (HighIntent)", "Customers (Customers)" }));
        });
    }


    [Test]
    public async Task GetOptionItems_NothingConfigured_IsEmpty()
    {
        var resolver = new ContactGroupResolver(
            new TestOptionsMonitor<AutomationExtensionsOptions>(new AutomationExtensionsOptions()),
            (_, _) => throw new AssertionException("Candidates must not be loaded."));
        var provider = new ContactGroupOptionsProvider(resolver, new ResourceLocalizationService());

        Assert.That(await provider.GetOptionItems(), Is.Empty);
    }
}
