using System.Reflection;

using CMS;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Configuration;

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
