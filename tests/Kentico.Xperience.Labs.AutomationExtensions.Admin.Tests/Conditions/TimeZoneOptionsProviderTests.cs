using Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class TimeZoneOptionsProviderTests
{
    [Test]
    public async Task GetOptionItems_SortsByUtcOffsetThenStandardName()
    {
        var provider = new TimeZoneOptionsProvider();
        var zones = TimeZoneInfo.GetSystemTimeZones();

        var items = (await provider.GetOptionItems()).ToList();
        var expectedIds = zones
            .OrderBy(zone => zone.BaseUtcOffset)
            .ThenBy(zone => zone.StandardName)
            .Select(zone => zone.Id);
        var offsetsById = zones.ToDictionary(zone => zone.Id, zone => zone.BaseUtcOffset);

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(expectedIds));
            Assert.That(items.Select(item => offsetsById[item.Value]), Is.Ordered);
        });
    }
}
