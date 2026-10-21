using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.Configuration;

public class CommerceServiceRegistrationTests
{
    [Test]
    public void AddAutomationExtensionsCommerce_RegistersComponentAndServices()
    {
        var services = new ServiceCollection().AddAutomationExtensionsCommerce();

        Assert.Multiple(() =>
        {
            Assert.That(services.Where(descriptor => descriptor.ServiceType == typeof(AutomationComponentDescriptor))
                .Select(descriptor => descriptor.ImplementationInstance), Is.EqualTo(new[] { CommerceAutomationComponents.OrderCompleted }));
            Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(IOrderCompletedTriggerDispatcher)).ImplementationType,
                Is.EqualTo(typeof(OrderCompletedTriggerDispatcher)));
            Assert.That(services.Single(descriptor => descriptor.ServiceType == typeof(IOrderCompletedDataSource)).ImplementationType,
                Is.EqualTo(typeof(OrderCompletedDataSource)));
        });
    }


    [Test]
    public void AddAutomationExtensionsCommerce_CalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection().AddAutomationExtensionsCommerce().AddAutomationExtensionsCommerce();

        Assert.Multiple(() =>
        {
            Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(AutomationComponentDescriptor)), Is.EqualTo(1));
            Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IOrderCompletedTriggerDispatcher)), Is.EqualTo(1));
        });
    }


    [Test]
    public void OrderCompleted_ConfigurationPath() =>
        Assert.That(CommerceAutomationComponents.OrderCompleted.ConfigurationPath, Is.EqualTo("AutomationExtensions:Components:OrderCompleted"));
}
