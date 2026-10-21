using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class StartedByTriggerConditionTests
{
    [TestCase(false, "no")]
    [TestCase(true, "no")]
    [TestCase(true, "yes")]
    public async Task Evaluate_DisabledComponentReturnsNoBeforeSkipOrMatching(bool skipStep, string skippedBranch)
    {
        var properties = new StartedByTriggerProperties { Trigger = DefaultTriggerIdentifiers.FORM_SUBMISSION, SkipStep = skipStep, SkippedBranch = skippedBranch };

        bool result = await CreateCondition(enabled: false).Evaluate(properties, TriggerProcessContext.Create(DefaultTriggerIdentifiers.FORM_SUBMISSION), CancellationToken.None);

        Assert.That(result, Is.False);
    }


    [TestCase("yes", true)]
    [TestCase("no", false)]
    public async Task Evaluate_SkippedConditionReturnsSelectedBranchBeforeValidation(string branch, bool expected)
    {
        var properties = new StartedByTriggerProperties { Trigger = "", SkipStep = true, SkippedBranch = branch };

        bool result = await CreateCondition().Evaluate(properties, TriggerProcessContext.Create("Acme.Other"), CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }


    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, DefaultTriggerIdentifiers.FORM_SUBMISSION, true)]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, DefaultTriggerIdentifiers.REGISTRATION, false)]
    [TestCase(DefaultTriggerIdentifiers.REGISTRATION, DefaultTriggerIdentifiers.REGISTRATION, true)]
    [TestCase(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, true)]
    [TestCase(ContactInactiveTrigger.IDENTIFIER, ContactInactiveTrigger.IDENTIFIER, true)]
    [TestCase(ContactInactiveTrigger.IDENTIFIER, "Labs.AutomationExtensions.ContactDateAnniversary", false)]
    [TestCase("Labs.AutomationExtensions.ContactGroupMember", "Labs.AutomationExtensions.ContactGroupMember", true)]
    [TestCase("Labs.AutomationExtensions.ContactDateAnniversary", null, false)]
    public async Task Evaluate_TriggerOnly(string trigger, string? identifier, bool expected)
    {
        bool result = await CreateCondition().Evaluate(new StartedByTriggerProperties { Trigger = trigger }, TriggerProcessContext.Create(identifier), CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }


    [TestCase(StubTriggerProvider.NO_DATA_TRIGGER, StubTriggerProvider.NO_DATA_TRIGGER, true)]
    [TestCase("Acme.UnregisteredTrigger", "Acme.UnregisteredTrigger", true)]
    [TestCase("Acme.UnregisteredTrigger", "acme.unregisteredtrigger", false)]
    public async Task Evaluate_TriggerWithoutDataOrUnregisteredMatchesByIdentifier(string trigger, string identifier, bool expected)
    {
        bool result = await CreateCondition().Evaluate(new StartedByTriggerProperties { Trigger = trigger }, TriggerProcessContext.Create(identifier), CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }


    [TestCase(30, true)]
    [TestCase(90, false)]
    public async Task Evaluate_TriggerDataMatch(int inactiveDays, bool expected)
    {
        var data = new ContactInactiveTriggerData { InactiveDays = inactiveDays, LastActivityDate = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc) };
        var properties = DataCheck(ContactInactiveTrigger.IDENTIFIER, "InactiveDays", nameof(TriggerDataOperator.LessThanOrEqual), "30");

        bool result = await CreateCondition().Evaluate(properties, TriggerProcessContext.Create(ContactInactiveTrigger.IDENTIFIER, data), CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }


    [Test]
    public async Task Evaluate_BuiltInTriggerDataMatch()
    {
        var data = new CustomActivityTriggerData { ActivityType = "acme_webinar", ActivityValue = "12.5" };
        var properties = DataCheck(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, "ActivityType", nameof(TriggerDataOperator.StartsWith), "ACME_");

        bool result = await CreateCondition().Evaluate(properties, TriggerProcessContext.Create(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, data), CancellationToken.None);

        Assert.That(result, Is.True);
    }


    [Test]
    public async Task Evaluate_MissingTriggerDataReturnsNo()
    {
        var properties = DataCheck(ContactInactiveTrigger.IDENTIFIER, "InactiveDays", nameof(TriggerDataOperator.IsEmpty), "");

        bool result = await CreateCondition().Evaluate(properties, TriggerProcessContext.Create(ContactInactiveTrigger.IDENTIFIER, triggerData: null), CancellationToken.None);

        Assert.That(result, Is.False);
    }


    [Test]
    public async Task Evaluate_TriggerDataOfAnotherTypeReturnsNo()
    {
        var properties = DataCheck(ContactInactiveTrigger.IDENTIFIER, "InactiveDays", nameof(TriggerDataOperator.IsNotEmpty), "");

        bool result = await CreateCondition().Evaluate(
            properties,
            TriggerProcessContext.Create(ContactInactiveTrigger.IDENTIFIER, new FormSubmissionTriggerData { BizFormId = 1 }),
            CancellationToken.None);

        Assert.That(result, Is.False);
    }


    [Test]
    public async Task Evaluate_TriggerMismatchReturnsNoWithoutReadingData()
    {
        var properties = DataCheck(ContactInactiveTrigger.IDENTIFIER, "InactiveDays", nameof(TriggerDataOperator.IsEmpty), "");

        bool result = await CreateCondition().Evaluate(properties, TriggerProcessContext.Create(DefaultTriggerIdentifiers.FORM_SUBMISSION, new ThrowingTriggerData()), CancellationToken.None);

        Assert.That(result, Is.False);
    }


    [TestCase("", false, "")]
    [TestCase(StubTriggerProvider.NO_DATA_TRIGGER, true, "BizFormId")]
    [TestCase("Acme.UnregisteredTrigger", true, "BizFormId")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, true, "MemberId")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, true, "Missing")]
    public async Task Evaluate_InvalidConfigurationReturnsNoWithoutReadingTrigger(string trigger, bool checkData, string field)
    {
        var properties = new StartedByTriggerProperties
        {
            Trigger = trigger,
            CheckTriggerData = checkData,
            Field = field,
            Value = "1"
        };
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        // A cancelled token throws right before the trigger is read, so returning normally proves no read happened.
        bool result = await CreateCondition().Evaluate(properties, TriggerProcessContext.Create(DefaultTriggerIdentifiers.FORM_SUBMISSION), cancellationSource.Token);

        Assert.That(result, Is.False);
    }


    [Test]
    public void Evaluate_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.That(
            async () => await CreateCondition().Evaluate(
                new StartedByTriggerProperties { Trigger = DefaultTriggerIdentifiers.FORM_SUBMISSION },
                TriggerProcessContext.Create(DefaultTriggerIdentifiers.FORM_SUBMISSION),
                cancellationSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }


    private static StartedByTriggerProperties DataCheck(string trigger, string field, string comparisonOperator, string value) => new()
    {
        Trigger = trigger,
        CheckTriggerData = true,
        Field = field,
        Operator = comparisonOperator,
        Value = value
    };


    private static StartedByTriggerCondition CreateCondition(bool enabled = true) =>
        new(new StubComponentStatus(enabled), StubTriggerProvider.WithLibraryTriggers(), NullLoggerFactory.Instance);


    private sealed class StubComponentStatus(bool enabled) : IAutomationComponentStatus
    {
        public bool IsEnabled(AutomationComponentDescriptor component) => enabled;
    }


    private sealed class ThrowingTriggerData : IAutomationTriggerData
    {
        public string Identifier => throw new InvalidOperationException("Trigger data must not be read.");
    }
}
