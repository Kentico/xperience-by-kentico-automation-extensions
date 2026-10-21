using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Supplies the named chat channels from configuration to the channel drop-down of <see cref="PostChatMessageActionProperties"/>.
/// </summary>
internal sealed class ChatChannelOptionsProvider(IOptionsMonitor<AutomationExtensionsOptions> options) : IDropDownOptionsProvider
{
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = options.CurrentValue.ChatChannels.Channels
            .OrderBy(channel => channel.Key, StringComparer.OrdinalIgnoreCase)
            .Select(channel => new DropDownOptionItem
            {
                Value = channel.Key,
                Text = string.IsNullOrWhiteSpace(channel.Value.DisplayName) ? channel.Key : channel.Value.DisplayName
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
