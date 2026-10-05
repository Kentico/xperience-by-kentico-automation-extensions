using System.Globalization;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;

/// <summary>
/// Provides 15-minute time-of-day options (HH:mm) so marketers select rather than type times.
/// </summary>
public sealed class TimeOfDayOptionsProvider : IDropDownOptionsProvider
{
    private const int STEP_MINUTES = 15;

    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = Enumerable.Range(0, 24 * 60 / STEP_MINUTES)
            .Select(i => TimeSpan.FromMinutes(i * STEP_MINUTES).ToString(@"hh\:mm", CultureInfo.InvariantCulture))
            .Select(time => new DropDownOptionItem { Value = time, Text = time })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
