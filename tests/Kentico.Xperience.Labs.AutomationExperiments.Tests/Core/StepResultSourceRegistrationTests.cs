using Kentico.Xperience.Labs.AutomationExperiments.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Core;

public class StepResultSourceRegistrationTests
{
    [Test]
    public void AddAutomationExperiments_RegistersSendWebhookSource()
    {
        var sources = CreateServices().BuildServiceProvider().GetServices<StepResultSource>();

        Assert.That(sources, Is.EqualTo(new[] { StepResultKeys.SendWebhook }));
    }


    [Test]
    public void AddStepResultSource_CustomSource_ResolvedAfterLibrarySources()
    {
        var services = CreateServices().AddStepResultSource("Acme.SyncToCrm", "Sync to CRM");

        var sources = services.BuildServiceProvider().GetServices<StepResultSource>();

        Assert.That(sources.Select(source => source.Name), Is.EqualTo(new[] { StepResultKeys.SendWebhook.Name, "Acme.SyncToCrm" }));
    }


    [Test]
    public void AddStepResultSource_SameRegistrationTwice_RegisteredOnce()
    {
        var services = CreateServices()
            .AddStepResultSource("Acme.SyncToCrm", "Sync to CRM")
            .AddStepResultSource("Acme.SyncToCrm", "Sync to CRM");

        var sources = services.BuildServiceProvider().GetServices<StepResultSource>();

        Assert.That(sources.Count(source => source.Name == "Acme.SyncToCrm"), Is.EqualTo(1));
    }


    [Test]
    public void AddAutomationExperiments_CalledTwice_RegistersLibrarySourceOnce()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
            .AddAutomationExperiments(configuration)
            .AddAutomationExperiments(configuration);

        var sources = services.BuildServiceProvider().GetServices<StepResultSource>();

        Assert.That(sources.Count(), Is.EqualTo(1));
    }


    [Test]
    public void AddStepResultSource_NameTakenWithDifferentDisplayName_Throws()
    {
        var services = CreateServices();

        Assert.That(
            () => services.AddStepResultSource("sendwebhook", "My webhook"),
            Throws.ArgumentException);
    }


    [TestCase("", "Sync to CRM")]
    [TestCase("Acme.SyncToCrm", " ")]
    public void AddStepResultSource_MissingValues_Throws(string name, string displayName)
    {
        var services = CreateServices();

        Assert.That(() => services.AddStepResultSource(name, displayName), Throws.ArgumentException);
    }


    private static IServiceCollection CreateServices() =>
        new ServiceCollection().AddAutomationExperiments(new ConfigurationBuilder().Build());
}
