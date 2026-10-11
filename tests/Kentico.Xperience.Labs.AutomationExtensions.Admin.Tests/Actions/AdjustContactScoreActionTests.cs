using System.Reflection;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class AdjustContactScoreActionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Execute_DisabledComponentDoesNothingBeforeSkipOrValidation(bool skipStep)
    {
        var provider = new RecordingContactProvider();
        var action = CreateAction(enabled: false, provider, ThrowingResolver());
        var properties = new AdjustContactScoreActionProperties { SkipStep = skipStep, Field = "AcmeContactScore", Amount = -1 };

        await action.Execute(properties, CreateContext(), CancellationToken.None);

        Assert.That(provider.SavedContacts, Is.Empty);
    }


    [Test]
    public async Task Execute_SkippedStepDoesNothingBeforeValidation()
    {
        var provider = new RecordingContactProvider();
        var action = CreateAction(enabled: true, provider, ThrowingResolver());
        var properties = new AdjustContactScoreActionProperties { SkipStep = true, Field = "AcmeContactScore", Amount = -1 };

        await action.Execute(properties, CreateContext(), CancellationToken.None);

        Assert.That(provider.SavedContacts, Is.Empty);
    }


    [TestCase("", 10)]
    [TestCase("ContactOwnerUserID", 10)]
    [TestCase("Missing", 10)]
    [TestCase("AcmeContactScore", -1)]
    public async Task Execute_InvalidConfigurationDoesNotSave(string field, int amount)
    {
        var provider = new RecordingContactProvider();
        var action = CreateAction(enabled: true, provider, ContactScoreFieldResolverTests.CreateResolver());
        var properties = new AdjustContactScoreActionProperties { Field = field, Amount = amount };

        await action.Execute(properties, CreateContext(), CancellationToken.None);

        Assert.That(provider.SavedContacts, Is.Empty);
    }


    [Test]
    public void Execute_InvalidConfigurationValidatedBeforeCancellation()
    {
        var action = CreateAction(enabled: true, new RecordingContactProvider(), ContactScoreFieldResolverTests.CreateResolver());
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.That(
            async () => await action.Execute(new AdjustContactScoreActionProperties(), CreateContext(), cancellationSource.Token),
            Throws.Nothing);
    }


    [Test]
    public void Execute_PropagatesCancellationBeforeReadingProcessedContact()
    {
        var provider = new RecordingContactProvider();
        var action = CreateAction(enabled: true, provider, ContactScoreFieldResolverTests.CreateResolver());
        var properties = new AdjustContactScoreActionProperties { Field = "AcmeContactScore" };
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(
                async () => await action.Execute(properties, CreateContext(), cancellationSource.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(provider.SavedContacts, Is.Empty);
        });
    }


    private static AdjustContactScoreAction CreateAction(bool enabled, IInfoProvider<ContactInfo> provider, ContactScoreFieldResolver resolver) =>
        new(new StubComponentStatus(enabled), provider, resolver, NullLoggerFactory.Instance);


    private static ContactScoreFieldResolver ThrowingResolver() => new(
        new ContactScoreFieldResolverTests.TestLocalization(text => text),
        () => throw new InvalidOperationException("Field definition must not be read."));


    private static AutomationProcessContext CreateContext()
    {
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        // The platform exposes no public test factory for its process context.
        object?[] arguments =
        [
            new ContactInfo(),
            new AutomationProcessMetadata(),
            (Func<string>)(() => string.Empty),
            (Action<string>)(_ => { }),
            string.Empty,
            null,
            NullLogger<AutomationProcessContext>.Instance
        ];

        return (AutomationProcessContext)constructor.Invoke(arguments);
    }


    private sealed class StubComponentStatus(bool enabled) : IAutomationComponentStatus
    {
        public bool IsEnabled(AutomationComponentDescriptor component) => enabled;
    }


    private sealed class RecordingContactProvider : IInfoProvider<ContactInfo>
    {
        public List<ContactInfo> SavedContacts { get; } = [];


        public ObjectQuery<ContactInfo> Get() => throw new NotSupportedException();


        public void Set(ContactInfo info) => SavedContacts.Add(info);


        public void Delete(ContactInfo info) => throw new NotSupportedException();
    }
}
