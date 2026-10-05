using System.Reflection;

using CMS;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Admin;
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


    [Test]
    public void Assembly_RegistersAdminModule()
    {
        var assembly = typeof(AutomationComponentCatalog).Assembly;
        var modules = assembly.GetCustomAttributes<RegisterModuleAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(assembly.GetName().Name, Is.EqualTo("Kentico.Xperience.Labs.AutomationExtensions.Admin"));
            Assert.That(modules.Select(module => module.Type), Does.Contain(typeof(AutomationExtensionsAdminModule)));
            Assert.That(typeof(AutomationExtensionsAdminModule).IsSubclassOf(typeof(AdminModule)), Is.True);
        });
    }
}
