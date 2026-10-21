using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Supplies the configured day counts (<see cref="ContactInactiveOptions.GetInactiveDays"/>) to the drop-down of
/// <see cref="ContactInactiveTriggerProperties"/>. Only day counts scanned by the scheduled task can be selected.
/// </summary>
internal sealed class InactiveDaysOptionsProvider(
    IOptionsMonitor<AutomationExtensionsOptions> options,
    ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        string daysFormat = localizationService.GetString("labs.automationextensions.contactinactive.inactivedays.format");

        var items = options.CurrentValue.ContactInactive
            .GetInactiveDays()
            .Select(days => new DropDownOptionItem
            {
                Value = days.ToString(CultureInfo.InvariantCulture),
                Text = string.Format(CultureInfo.CurrentCulture, daysFormat, days)
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
