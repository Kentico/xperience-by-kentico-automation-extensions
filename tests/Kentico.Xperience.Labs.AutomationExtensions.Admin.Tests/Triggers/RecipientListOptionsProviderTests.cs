using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class RecipientListOptionsProviderTests
{
    [Test]
    public async Task GetOptionItems_ListsOnlyResolvedListsWithDisplayAndCodeName()
    {
        var options = new AutomationExtensionsOptions { RecipientListMember = { RecipientLists = ["Newsletter", "Unknown", "Customers", "Events"] } };
        var resolver = new RecipientListResolver(new TestOptionsMonitor<AutomationExtensionsOptions>(options), (_, _) =>
            Task.FromResult<IEnumerable<ContactGroupCandidate>>(
            [
                new(1, "Events", "Events", ContactGroupCandidateKind.RecipientList),
                new(2, "Customers", "Customers", ContactGroupCandidateKind.ContactGroup),
                new(3, "Newsletter", "Monthly newsletter", ContactGroupCandidateKind.RecipientList)
            ]));
        var provider = new RecipientListOptionsProvider(resolver, new ResourceLocalizationService());

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(new[] { "Newsletter", "Events" }));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[] { "Monthly newsletter (Newsletter)", "Events (Events)" }));
        });
    }


    [Test]
    public async Task GetOptionItems_NothingConfigured_IsEmpty()
    {
        var resolver = new RecipientListResolver(
            new TestOptionsMonitor<AutomationExtensionsOptions>(new AutomationExtensionsOptions()),
            (_, _) => throw new AssertionException("Candidates must not be loaded."));
        var provider = new RecipientListOptionsProvider(resolver, new ResourceLocalizationService());

        Assert.That(await provider.GetOptionItems(), Is.Empty);
    }
}
