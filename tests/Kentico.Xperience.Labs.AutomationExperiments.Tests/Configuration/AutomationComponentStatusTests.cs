using Kentico.Xperience.Labs.AutomationExperiments.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Configuration;

public class AutomationComponentStatusTests
{
    [Test]
    public void IsEnabled_NoConfiguration_ComponentsDisabled()
    {
        var status = CreateStatus([]);

        Assert.That(AutomationComponentCatalog.All.Select(status.IsEnabled), Is.All.False);
    }


    [Test]
    public void IsEnabled_ComponentOptedIn_OnlyThatComponentEnabled()
    {
        var status = CreateStatus(new()
        {
            ["AutomationExperiments:Components:SendWebhook"] = "true"
        });

        Assert.Multiple(() =>
        {
            Assert.That(status.IsEnabled(AutomationComponentCatalog.SendWebhook), Is.True);
            Assert.That(status.IsEnabled(AutomationComponentCatalog.PreviousStepSucceeded), Is.False);
        });
    }


    [Test]
    public void IsEnabled_EnabledByDefaultWithOptOut_OptedOutComponentDisabled()
    {
        var status = CreateStatus(new()
        {
            ["AutomationExperiments:EnabledByDefault"] = "true",
            ["AutomationExperiments:Components:SendWebhook"] = "false"
        });

        Assert.Multiple(() =>
        {
            Assert.That(status.IsEnabled(AutomationComponentCatalog.SendWebhook), Is.False);
            Assert.That(status.IsEnabled(AutomationComponentCatalog.PreviousStepSucceeded), Is.True);
        });
    }


    [Test]
    public void IsEnabled_ComponentNameDifferentCase_Matches()
    {
        var status = CreateStatus(new()
        {
            ["AutomationExperiments:Components:sendwebhook"] = "true"
        });

        Assert.That(status.IsEnabled(AutomationComponentCatalog.SendWebhook), Is.True);
    }


    private static IAutomationComponentStatus CreateStatus(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new ServiceCollection()
            .AddAutomationExperiments(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IAutomationComponentStatus>();
    }
}
