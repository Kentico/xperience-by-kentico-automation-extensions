using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExperiments.Configuration;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

/// <summary>
/// Supplies the configured day offsets (<see cref="ContactDateAnniversaryOptions.GetDaysBefore"/>) to the timing drop-down
/// of <see cref="ContactDateAnniversaryTriggerProperties"/>. Only offsets scanned by the scheduled task can be selected.
/// </summary>
internal sealed class DaysBeforeOptionsProvider(
    IOptionsMonitor<AutomationExperimentsOptions> options,
    ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly IOptionsMonitor<AutomationExperimentsOptions> options = options;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        string onTheDayText = localizationService.GetString("labs.automationexperiments.contactdateanniversary.daysbefore.ontheday");
        string daysBeforeFormat = localizationService.GetString("labs.automationexperiments.contactdateanniversary.daysbefore.format");

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
