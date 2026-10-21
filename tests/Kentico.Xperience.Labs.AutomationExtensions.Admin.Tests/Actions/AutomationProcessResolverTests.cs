using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

/// <summary>
/// Covers the matching rule. The database query needs an initialized Xperience container and isn't tested here.
/// </summary>
public class AutomationProcessResolverTests
{
    [Test]
    public async Task FindIdByDisplayName_SingleExactMatch_ReturnsId() =>
        Assert.That(await CreateResolver((1, "Lead nurture")).FindIdByDisplayName("Lead nurture", CancellationToken.None), Is.EqualTo(1));


    [Test]
    public async Task FindIdByDisplayName_CaseVariantFromDatabase_IgnoredOrdinally() =>
        Assert.That(await CreateResolver((1, "Lead nurture"), (2, "LEAD NURTURE")).FindIdByDisplayName("Lead nurture", CancellationToken.None), Is.EqualTo(1));


    [Test]
    public async Task FindIdByDisplayName_SeveralProcessesWithName_ReturnsNull() =>
        Assert.That(await CreateResolver((1, "Lead nurture"), (2, "Lead nurture")).FindIdByDisplayName("Lead nurture", CancellationToken.None), Is.Null);


    [Test]
    public async Task FindIdByDisplayName_NoMatch_ReturnsNull() =>
        Assert.That(await CreateResolver().FindIdByDisplayName("Lead nurture", CancellationToken.None), Is.Null);


    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task FindIdByDisplayName_EmptyName_DoesNotQuery(string? displayName)
    {
        bool queried = false;
        var resolver = new AutomationProcessResolver((_, _) =>
        {
            queried = true;
            return Task.FromResult<IReadOnlyList<(int Id, string DisplayName)>>([]);
        }, NullLoggerFactory.Instance);

        int? result = await resolver.FindIdByDisplayName(displayName, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(queried, Is.False);
        });
    }


    private static AutomationProcessResolver CreateResolver(params (int Id, string DisplayName)[] candidates) =>
        new((_, _) => Task.FromResult<IReadOnlyList<(int Id, string DisplayName)>>(candidates), NullLoggerFactory.Instance);
}
