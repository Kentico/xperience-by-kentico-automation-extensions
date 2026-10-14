using CMS.Automation.Internal;
using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Supplies the triggers selectable in <see cref="StartedByTriggerProperties.Trigger"/>: built-in triggers and every
/// registered custom trigger, sorted by display name. Value = trigger identifier.
/// </summary>
internal sealed class TriggerOptionsProvider(
    ICustomAutomationTriggerProvider triggerProvider,
    ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = RegisteredTriggers.GetAll(triggerProvider)
            .Select(trigger => new DropDownOptionItem
            {
                Value = trigger.Identifier,
                Text = LocalizeIfResourceKey(trigger.DisplayName, trigger.Identifier)
            })
            .OrderBy(item => item.Text, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }


    /// <summary>
    /// Display names are plain text or resource keys (registration attributes accept both) – same rule as the step registry.
    /// </summary>
    private string LocalizeIfResourceKey(string? displayName, string identifier)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return identifier;
        }

        return displayName.Contains('.') && !displayName.Any(char.IsWhiteSpace)
            ? localizationService.GetString(displayName)
            : displayName;
    }
}
