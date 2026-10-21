using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ChatChannelOptionsProviderTests
{
    [Test]
    public async Task GetOptionItems_SortedByNameWithDisplayNameFallback()
    {
        var options = new AutomationExtensionsOptions();
        options.ChatChannels.Channels["SalesTeams"] = new() { Provider = ChatProvider.Teams, DisplayName = "Sales (Teams)", Url = "https://example.test/a" };
        options.ChatChannels.Channels["marketing"] = new() { Provider = ChatProvider.Slack, DisplayName = " ", Url = "https://example.test/b" };
        options.ChatChannels.Channels["SalesSlack"] = new() { Provider = ChatProvider.Slack, DisplayName = "#sales (Slack)", Url = "https://example.test/c" };

        var items = (await new ChatChannelOptionsProvider(new TestOptionsMonitor<AutomationExtensionsOptions>(options)).GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(new[] { "marketing", "SalesSlack", "SalesTeams" }));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[] { "marketing", "#sales (Slack)", "Sales (Teams)" }));
            Assert.That(items.Select(item => item.Text), Has.None.Contains("https://"));
        });
    }


    [Test]
    public async Task GetOptionItems_NoChannels_ReturnsEmpty()
    {
        var items = await new ChatChannelOptionsProvider(new TestOptionsMonitor<AutomationExtensionsOptions>(new())).GetOptionItems();

        Assert.That(items, Is.Empty);
    }
}
