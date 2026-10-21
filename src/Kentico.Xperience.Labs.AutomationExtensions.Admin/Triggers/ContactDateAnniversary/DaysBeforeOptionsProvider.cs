using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

/// <summary>
/// Supplies the configured day offsets (<see cref="ContactDateAnniversaryOptions.GetDaysBefore"/>) to the timing drop-down
/// of <see cref="ContactDateAnniversaryTriggerProperties"/>. Only offsets scanned by the scheduled task can be selected.
/// </summary>
internal sealed class DaysBeforeOptionsProvider(
    IOptionsMonitor<AutomationExtensionsOptions> options,
    ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        string onTheDayText = localizationService.GetString("labs.automationextensions.contactdateanniversary.daysbefore.ontheday");
        string daysBeforeFormat = localizationService.GetString("labs.automationextensions.contactdateanniversary.daysbefore.format");

        var items = options.CurrentValue.ContactDateAnniversary
            .GetDaysBefore()
            .Select(days => new DropDownOptionItem
            {
                Value = days.ToString(CultureInfo.InvariantCulture),
                Text = days == 0 ? onTheDayText : string.Format(CultureInfo.CurrentCulture, daysBeforeFormat, days)
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
