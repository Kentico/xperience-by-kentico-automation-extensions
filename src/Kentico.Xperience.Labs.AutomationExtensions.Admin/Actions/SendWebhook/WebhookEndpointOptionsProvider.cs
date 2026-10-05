using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// Supplies the named webhook endpoints from configuration to the endpoint drop-down of <see cref="SendWebhookActionProperties"/>.
/// </summary>
internal sealed class WebhookEndpointOptionsProvider(IOptionsMonitor<AutomationExtensionsOptions> options) : IDropDownOptionsProvider
{
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = options.CurrentValue.Webhooks.Endpoints
            .OrderBy(endpoint => endpoint.Key, StringComparer.OrdinalIgnoreCase)
            .Select(endpoint => new DropDownOptionItem
            {
                Value = endpoint.Key,
                Text = string.IsNullOrWhiteSpace(endpoint.Value.DisplayName) ? endpoint.Key : endpoint.Value.DisplayName
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
