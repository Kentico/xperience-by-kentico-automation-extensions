using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;

/// <summary>
/// Provides time zone options for the time zone dropdown component.
/// </summary>
public sealed class TimeZoneOptionsProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var zones = TimeZoneInfo.GetSystemTimeZones();
        var items = zones
            .OrderBy(z => z.StandardName)
            .Select(zone => new DropDownOptionItem
            {
                Value = zone.Id,
                Text = $"{zone.StandardName} (UTC{(zone.BaseUtcOffset.Hours >= 0 ? "+" : "")}{zone.BaseUtcOffset.Hours})"
            })
            .ToList();

        return await Task.FromResult(items);
    }
}


