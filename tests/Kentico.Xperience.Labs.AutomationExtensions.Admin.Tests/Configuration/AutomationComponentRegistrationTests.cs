using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Configuration;

/// <summary>
/// Components from other assemblies (e.g. commerce) join the catalog through DI.
/// </summary>
public class AutomationComponentRegistrationTests
{
    private static readonly AutomationComponentDescriptor extra = new("OrderCompleted", AutomationComponentKind.Trigger);


    [Test]
    public void AddAutomationComponent_RegistersDescriptorOnce()
    {
        var services = new ServiceCollection()
            .AddAutomationComponent(extra)
            .AddAutomationComponent(extra);

        Assert.That(services.Where(descriptor => descriptor.ServiceType == typeof(AutomationComponentDescriptor))
            .Select(descriptor => descriptor.ImplementationInstance), Is.EqualTo(new[] { extra }));
    }


    [Test]
    public void AddAutomationComponent_SameNameDifferentKind_Throws()
    {
        var services = new ServiceCollection().AddAutomationComponent(extra);

        Assert.That(() => services.AddAutomationComponent(new AutomationComponentDescriptor("ordercompleted", AutomationComponentKind.Action)),
            Throws.ArgumentException);
    }


    [Test]
    public void AddAutomationComponent_CatalogName_Throws() =>
        Assert.That(() => new ServiceCollection().AddAutomationComponent(AutomationComponentCatalog.SendWebhook), Throws.ArgumentException);


    [Test]
    public void WithAdditional_AppendsWithoutDuplicates()
    {
        var components = AutomationComponentCatalog.WithAdditional([extra, extra, AutomationComponentCatalog.RandomSplit]);

        Assert.That(components, Is.EqualTo(AutomationComponentCatalog.All.Append(extra)));
    }


    [Test]
    public void WithAdditional_Null_ReturnsCatalog() =>
        Assert.That(AutomationComponentCatalog.WithAdditional(null), Is.EqualTo(AutomationComponentCatalog.All));
}
