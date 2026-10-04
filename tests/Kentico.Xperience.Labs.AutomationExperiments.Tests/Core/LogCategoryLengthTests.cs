using System.Reflection;

using Kentico.Xperience.Labs.AutomationExperiments.Configuration;
using Kentico.Xperience.Labs.AutomationExperiments.Core;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Core;

/// <summary>
/// The Xperience event log stores the logger category in a 100-character Source column and the event name in a
/// 100-character EventCode column. Entries exceeding either limit are silently dropped from the event log.
/// </summary>
public class LogCategoryLengthTests
{
    private const int EVENT_LOG_COLUMN_LENGTH = 100;


    [Test]
    public void LibraryTypes_DoNotInjectTypedLoggers()
    {
        var typesWithTypedLoggers = typeof(AutomationComponentCatalog).Assembly
            .GetTypes()
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>)))
            .Select(type => type.FullName);

        Assert.That(typesWithTypedLoggers, Is.Empty, "Use ILoggerFactory with LogCategories instead of ILogger<T> (full type names can exceed the event log Source length).");
    }


    [Test]
    public void Categories_FitEventLogSourceColumn()
    {
        var categories = AutomationComponentCatalog.All
            .Select(LogCategories.For)
            .Append(LogCategories.CONFIGURATION);

        Assert.That(categories, Is.All.Length.LessThanOrEqualTo(EVENT_LOG_COLUMN_LENGTH));
    }


    [Test]
    public void For_BuildsCategoryFromComponentName()
    {
        Assert.That(LogCategories.For(AutomationComponentCatalog.SendWebhook), Is.EqualTo("Kentico.Xperience.Labs.AutomationExperiments.SendWebhook"));
    }


    [Test]
    public void EventNames_FitEventLogEventCodeColumn()
    {
        var names = typeof(LogEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => ((EventId)field.GetValue(null)!).Name!);

        Assert.That(names, Is.All.Length.LessThanOrEqualTo(EVENT_LOG_COLUMN_LENGTH));
    }
}
