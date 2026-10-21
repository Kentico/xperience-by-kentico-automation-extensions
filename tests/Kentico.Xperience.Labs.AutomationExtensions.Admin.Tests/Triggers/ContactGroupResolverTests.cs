using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class ContactGroupResolverTests
{
    [Test]
    public void Resolve_SortsOutUnknownRecipientListsAndOtherTypes_KeepsConfigurationOrder()
    {
        ContactGroupCandidate[] candidates =
        [
            new(1, "Customers", "Customers group", ContactGroupCandidateKind.ContactGroup),
            new(2, "Newsletter", "Newsletter list", ContactGroupCandidateKind.RecipientList),
            new(3, "HighIntent", "High intent", ContactGroupCandidateKind.ContactGroup),
            new(4, "Segment", "Some segment", ContactGroupCandidateKind.Other)
        ];

        var resolution = ContactGroupResolver.Resolve(["highintent", "Unknown", "Newsletter", "Customers", "Segment"], candidates);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Groups, Is.EqualTo(new[]
            {
                new ResolvedContactGroup(3, "HighIntent", "High intent"),
                new ResolvedContactGroup(1, "Customers", "Customers group")
            }));
            Assert.That(resolution.NotFound, Is.EqualTo(new[] { "Unknown" }));
            Assert.That(resolution.RecipientLists, Is.EqualTo(new[] { "Newsletter" }));
            Assert.That(resolution.OtherTypes, Is.EqualTo(new[] { "Segment" }));
        });
    }


    [Test]
    public void Resolve_SameCodeNameInSeveralTypes_PrefersContactGroup()
    {
        ContactGroupCandidate[] candidates =
        [
            new(2, "Shared", "List", ContactGroupCandidateKind.RecipientList),
            new(1, "Shared", "Group", ContactGroupCandidateKind.ContactGroup)
        ];

        var resolution = ContactGroupResolver.Resolve(["Shared"], candidates);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Groups, Is.EqualTo(new[] { new ResolvedContactGroup(1, "Shared", "Group") }));
            Assert.That(resolution.RecipientLists, Is.Empty);
        });
    }


    [Test]
    public void Resolve_EmptyDisplayName_FallsBackToCodeName()
    {
        var resolution = ContactGroupResolver.Resolve(["Customers"], [new(1, "Customers", " ", ContactGroupCandidateKind.ContactGroup)]);

        Assert.That(resolution.Groups.Single().DisplayName, Is.EqualTo("Customers"));
    }


    [Test]
    public async Task Resolve_NothingConfigured_DoesNotLoadCandidates()
    {
        var resolver = new ContactGroupResolver(
            new TestOptionsMonitor<AutomationExtensionsOptions>(new AutomationExtensionsOptions()),
            (_, _) => throw new AssertionException("Candidates must not be loaded."));

        var resolution = await resolver.Resolve(CancellationToken.None);

        Assert.That(resolution, Is.SameAs(ContactGroupResolution.Empty));
    }


    [Test]
    public async Task Resolve_LoadsCandidatesForEffectiveCodeNames()
    {
        IReadOnlyList<string>? requested = null;
        var options = new AutomationExtensionsOptions { ContactGroupMember = { ContactGroups = [" Customers ", "customers", ""] } };
        var resolver = new ContactGroupResolver(new TestOptionsMonitor<AutomationExtensionsOptions>(options), (codeNames, _) =>
        {
            requested = codeNames;
            return Task.FromResult<IEnumerable<ContactGroupCandidate>>([new(1, "Customers", "Customers", ContactGroupCandidateKind.ContactGroup)]);
        });

        var resolution = await resolver.Resolve(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(requested, Is.EqualTo(new[] { "Customers" }));
            Assert.That(resolution.Groups.Select(group => group.Id), Is.EqualTo(new[] { 1 }));
        });
    }
}
