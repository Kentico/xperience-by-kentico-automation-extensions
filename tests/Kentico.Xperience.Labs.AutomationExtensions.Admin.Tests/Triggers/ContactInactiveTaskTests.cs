using System.Reflection;

using CMS.Activities;
using CMS.DataEngine;
using CMS.Scheduler;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

/// <summary>
/// Executing the activity and contact queries requires an Xperience database, so the enabled path is covered by the query
/// shape (<see cref="ContactInactiveTask.SelectContactIds"/>) and running-app validation.
/// </summary>
public class ContactInactiveTaskTests
{
    [Test]
    public async Task Execute_Disabled_ReturnsResultTextWithoutQueries()
    {
        var monitor = new TestOptionsMonitor<AutomationExtensionsOptions>(new AutomationExtensionsOptions());
        // Null dispatcher and providers: any query or fired trigger would throw.
        var task = new ContactInactiveTask(null!, null!, null!, new AutomationComponentStatus(monitor), monitor, TimeProvider.System, NullLoggerFactory.Instance);

        var result = await task.Execute(new ScheduledTaskConfigurationInfo(), CancellationToken.None);

        // ScheduledTaskExecutionResult.Result isn't public.
        object? message = typeof(ScheduledTaskExecutionResult)
            .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(result);

        Assert.That(message,
            Is.EqualTo($"Disabled via configuration ('{AutomationComponentCatalog.ContactInactive.ConfigurationPath}'). No triggers fired."));
    }


    [Test]
    public void SelectContactIds_GroupsByContactWithParameterizedRangeAndKeysetPaging()
    {
        var range = InactivityCalendar.GetRange(new DateOnly(2026, 10, 11), 7);

        var query = ContactInactiveTask.SelectContactIds(new ObjectQuery<ActivityInfo>(), range, 1234, 500);

        Assert.Multiple(() =>
        {
            Assert.That(query.SelectColumnsList.ToString(), Is.EqualTo("[ActivityContactID]"));
            Assert.That(query.WhereCondition, Does.Match(@"^\[ActivityCreated\] >= @\w+ AND \[ActivityContactID\] > @\w+$"));
            Assert.That(query.GroupByColumns, Is.EqualTo("ActivityContactID"));
            Assert.That(query.HavingCondition, Does.Match(@"^MAX\(\[?ActivityCreated\]?\) < @\w+$"));
            Assert.That(query.OrderByColumns, Is.EqualTo("ActivityContactID"));
            Assert.That(query.TopNRecords, Is.EqualTo(500));
            Assert.That(query.Parameters.Select(parameter => parameter.Value),
                Is.EquivalentTo(new object[] { range.Start, 1234, range.End }));
        });
    }
}
