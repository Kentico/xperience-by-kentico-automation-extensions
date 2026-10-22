using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class RecipientListResolverTests
{
    [Test]
    public void Resolve_SortsOutUnknownNamesContactGroupsAndOtherTypes_KeepsConfigurationOrder()
    {
        ContactGroupCandidate[] candidates =
        [
            new(1, "Customers", "Customers group", ContactGroupCandidateKind.ContactGroup),
            new(2, "Newsletter", "Newsletter list", ContactGroupCandidateKind.RecipientList),
            new(3, "Events", "Event invitations", ContactGroupCandidateKind.RecipientList),
            new(4, "Segment", "Some segment", ContactGroupCandidateKind.Other)
        ];

        var resolution = RecipientListResolver.Resolve(["events", "Unknown", "Newsletter", "Customers", "Segment"], candidates);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Lists, Is.EqualTo(new[]
            {
                new ResolvedContactGroup(3, "Events", "Event invitations"),
                new ResolvedContactGroup(2, "Newsletter", "Newsletter list")
            }));
            Assert.That(resolution.NotFound, Is.EqualTo(new[] { "Unknown" }));
            Assert.That(resolution.NotRecipientLists, Is.EqualTo(new[] { "Customers", "Segment" }));
        });
    }


    [Test]
    public void Resolve_SameCodeNameInSeveralTypes_PrefersRecipientList()
    {
        ContactGroupCandidate[] candidates =
        [
            new(1, "Shared", "Group", ContactGroupCandidateKind.ContactGroup),
            new(2, "Shared", "List", ContactGroupCandidateKind.RecipientList)
        ];

        var resolution = RecipientListResolver.Resolve(["Shared"], candidates);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Lists, Is.EqualTo(new[] { new ResolvedContactGroup(2, "Shared", "List") }));
            Assert.That(resolution.NotRecipientLists, Is.Empty);
        });
    }


    [Test]
    public void Resolve_EmptyDisplayName_FallsBackToCodeName()
    {
        var resolution = RecipientListResolver.Resolve(["Newsletter"], [new(1, "Newsletter", " ", ContactGroupCandidateKind.RecipientList)]);

        Assert.That(resolution.Lists.Single().DisplayName, Is.EqualTo("Newsletter"));
    }


    [Test]
    public async Task Resolve_NothingConfigured_DoesNotLoadCandidates()
    {
        var resolver = new RecipientListResolver(
            new TestOptionsMonitor<AutomationExtensionsOptions>(new AutomationExtensionsOptions()),
            (_, _) => throw new AssertionException("Candidates must not be loaded."));

        var resolution = await resolver.Resolve(CancellationToken.None);

        Assert.That(resolution, Is.SameAs(RecipientListResolution.Empty));
    }


    [Test]
    public async Task Resolve_LoadsCandidatesForEffectiveCodeNames()
    {
        IReadOnlyList<string>? requested = null;
        var options = new AutomationExtensionsOptions { RecipientListMember = { RecipientLists = [" Newsletter ", "newsletter", ""] } };
        var resolver = new RecipientListResolver(new TestOptionsMonitor<AutomationExtensionsOptions>(options), (codeNames, _) =>
        {
            requested = codeNames;
            return Task.FromResult<IEnumerable<ContactGroupCandidate>>([new(1, "Newsletter", "Newsletter", ContactGroupCandidateKind.RecipientList)]);
        });

        var resolution = await resolver.Resolve(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(requested, Is.EqualTo(new[] { "Newsletter" }));
            Assert.That(resolution.Lists.Select(list => list.Id), Is.EqualTo(new[] { 1 }));
        });
    }
}
