using System.Reflection;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class SetContactFieldActionTests
{
    private static readonly DateTimeOffset now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);


    [TestCase(false)]
    [TestCase(true)]
    public async Task Execute_DisabledComponentDoesNothingBeforeSkipOrValidation(bool skipStep)
    {
        var provider = new RecordingContactProvider();
        var processData = new ProcessDataStore();
        var action = CreateAction(enabled: false, provider, ThrowingResolver());
        var properties = new SetContactFieldActionProperties { SkipStep = skipStep, Field = "AcmeCounter" };

        await action.Execute(properties, CreateContext(processData), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(provider.SavedContacts, Is.Empty);
            Assert.That(processData.Value, Is.Null);
        });
    }


    [Test]
    public async Task Execute_SkippedStepSavesSkippedResultBeforeValidation()
    {
        var provider = new RecordingContactProvider();
        var processData = new ProcessDataStore();
        var action = CreateAction(enabled: true, provider, ThrowingResolver());
        var properties = new SetContactFieldActionProperties { SkipStep = true, Field = "AcmeCounter", ResultNumber = "3" };
        var context = CreateContext(processData);

        await action.Execute(properties, context, CancellationToken.None);

        var result = await GetResult(context, "3");
        Assert.Multiple(() =>
        {
            Assert.That(provider.SavedContacts, Is.Empty);
            Assert.That(result?.Skipped, Is.True);
            Assert.That(result?.Succeeded, Is.False);
            Assert.That(result?.ActionIdentifier, Is.EqualTo(SetContactFieldAction.IDENTIFIER));
            Assert.That(result?.RecordedAt, Is.EqualTo(now.UtcDateTime));
        });
    }


    [TestCase("", "SetValue", "Add")]
    [TestCase("Missing", "SetValue", "Add")]
    [TestCase("ContactGUID", "SetValue", "Add")]
    [TestCase("AcmeCounter", "Other", "Add")]
    [TestCase("AcmeLastCampaign", "Calculate", "Add")]
    [TestCase("AcmeCounter", "Calculate", "Power")]
    public async Task Execute_InvalidConfigurationSavesFailedResultWithoutWriting(string field, string mode, string arithmeticOperator)
    {
        var provider = new RecordingContactProvider();
        var processData = new ProcessDataStore();
        var action = CreateAction(enabled: true, provider, ContactFieldResolverTests.CreateResolver());
        var properties = new SetContactFieldActionProperties { Field = field, Mode = mode, Operator = arithmeticOperator };
        var context = CreateContext(processData);

        await action.Execute(properties, context, CancellationToken.None);

        var result = await GetResult(context, "1");
        Assert.Multiple(() =>
        {
            Assert.That(provider.SavedContacts, Is.Empty);
            Assert.That(result?.Succeeded, Is.False);
            Assert.That(result?.Skipped, Is.False);
            Assert.That(result?.Message, Is.EqualTo(SetContactFieldEvaluator.MESSAGE_INVALID_CONFIGURATION));
        });
    }


    [Test]
    public void Execute_InvalidConfigurationValidatedBeforeCancellation()
    {
        var action = CreateAction(enabled: true, new RecordingContactProvider(), ContactFieldResolverTests.CreateResolver());
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.That(
            async () => await action.Execute(new SetContactFieldActionProperties(), CreateContext(new ProcessDataStore()), cancellationSource.Token),
            Throws.Nothing);
    }


    [Test]
    public void Execute_PropagatesCancellationBeforeReadingProcessedContact()
    {
        var provider = new RecordingContactProvider();
        var action = CreateAction(enabled: true, provider, ContactFieldResolverTests.CreateResolver());
        var properties = new SetContactFieldActionProperties { Field = "AcmeCounter", ValueTemplate = "1" };
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(
                async () => await action.Execute(properties, CreateContext(new ProcessDataStore()), cancellationSource.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(provider.SavedContacts, Is.Empty);
        });
    }


    [Test]
    public void Registration_IdentifierIconAndCatalog()
    {
        var registration = typeof(SetContactFieldAction).Assembly
            .GetCustomAttributes<RegisterAutomationActionAttribute<SetContactFieldAction>>()
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(SetContactFieldAction.IDENTIFIER, Is.EqualTo("Labs.AutomationExtensions.SetContactField"));
            Assert.That(registration.Identifier, Is.EqualTo(SetContactFieldAction.IDENTIFIER));
            Assert.That(registration.IconName, Is.EqualTo(Icons.EditInSquare));
            Assert.That(AutomationComponentCatalog.SetContactField.Identifier, Is.EqualTo(SetContactFieldAction.IDENTIFIER));
            Assert.That(AutomationComponentCatalog.SetContactField.Kind, Is.EqualTo(AutomationComponentKind.Action));
            Assert.That(AutomationComponentCatalog.All, Does.Contain(AutomationComponentCatalog.SetContactField));
            Assert.That(StepResultKeys.SetContactField.Name, Is.EqualTo(SetContactFieldAction.NAME));
        });
    }


    [Test]
    public void Registration_IconNotUsedByOtherLibrarySteps()
    {
        var icons = typeof(SetContactFieldAction).Assembly
            .GetCustomAttributes()
            .Select(attribute => attribute.GetType().GetProperty("IconName")?.GetValue(attribute) as string)
            .Where(icon => icon is not null)
            .ToList();

        Assert.That(icons.Count(icon => icon == Icons.EditInSquare), Is.EqualTo(1));
    }


    [Test]
    public void Properties_SkippableWithStepBehaviorCategory() =>
        Assert.That(typeof(SkippableActionProperties).IsAssignableFrom(typeof(SetContactFieldActionProperties)), Is.True);


    private static SetContactFieldAction CreateAction(bool enabled, IInfoProvider<ContactInfo> provider, ContactFieldResolver resolver) =>
        new(new StubComponentStatus(enabled), provider, resolver, new FixedTimeProvider(), NullLoggerFactory.Instance);


    private static ContactFieldResolver ThrowingResolver() => new(
        new ContactScoreFieldResolverTests.TestLocalization(text => text),
        () => throw new InvalidOperationException("Field definition must not be read."));


    private static async Task<StepResult?> GetResult(AutomationProcessContext context, string resultNumber) =>
        StepResultRecorder.Find(
            await context.GetProcessData<StepResultData>(),
            StepResultKeys.Create(StepResultKeys.SetContactField.Name, resultNumber));


    private static AutomationProcessContext CreateContext(ProcessDataStore processData)
    {
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        // The platform exposes no public test factory for its process context.
        object?[] arguments =
        [
            new ContactInfo(),
            new AutomationProcessMetadata { DisplayName = "Test process" },
            (Func<string>)(() => processData.Value!),
            (Action<string>)(value => processData.Value = value),
            string.Empty,
            null,
            NullLogger<AutomationProcessContext>.Instance
        ];

        return (AutomationProcessContext)constructor.Invoke(arguments);
    }


    private sealed class ProcessDataStore
    {
        public string? Value { get; set; }
    }


    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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
