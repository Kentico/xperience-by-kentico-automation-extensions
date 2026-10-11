using System.Reflection;

using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Core;

public class LogEventsTests
{
    [Test]
    public void All_IdsAndNamesUnique()
    {
        var events = typeof(LogEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (EventId)field.GetValue(null)!)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events.Select(eventId => eventId.Id), Is.Unique);
            Assert.That(events.Select(eventId => eventId.Name), Is.Unique);
        });
    }
}
