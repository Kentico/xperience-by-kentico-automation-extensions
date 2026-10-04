using System.Reflection;

using CMS;

using Kentico.Xperience.Labs.AutomationExperiments.Configuration;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Configuration;

/// <summary>
/// Without class discovery Xperience ignores all registration attributes of the library (steps, triggers, tasks, localization).
/// </summary>
public class AssemblyDiscoveryTests
{
    [Test]
    public void Assembly_IsDiscoverable()
    {
        var attribute = typeof(AutomationComponentCatalog).Assembly.GetCustomAttribute<AssemblyDiscoverableAttribute>();

        Assert.That(attribute, Is.Not.Null);
    }
}
