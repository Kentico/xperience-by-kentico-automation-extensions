using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ContactFieldValueTemplateTests
{
    private static readonly Guid contactGuid = Guid.Parse("8f6c2b8e-6d33-4c4a-9c47-0e2b1d1f6a10");
    private static readonly StubTriggerProvider triggers = ProjectTriggers.CreateProvider();


    [TestCase("{now}", "2026-10-11T14:05:09")]
    [TestCase("{today}", "2026-10-11")]
    [TestCase("{processName}", "Win-back")]
    [TestCase("{triggerIdentifier}", "Labs.AutomationExtensions.ContactInactive")]
    [TestCase("{contactId}", "42")]
    [TestCase("{contactGuid}", "8f6c2b8e-6d33-4c4a-9c47-0e2b1d1f6a10")]
    [TestCase("{contact.AcmeCounter}", "7")]
    [TestCase("{triggerData.InactiveDays}", "90")]
    [TestCase("{triggerData.LastActivityDate}", "2026-07-13T00:00:00")]
    public void Resolve_EachToken(string template, string expected) => Assert.That(Resolve(template, out _), Is.EqualTo(expected));


    [TestCase("{NOW}", "2026-10-11T14:05:09")]
    [TestCase("{ProcessName}", "Win-back")]
    [TestCase("{CONTACT.acmecounter}", "7")]
    [TestCase("{TriggerData.inactivedays}", "90")]
    public void Resolve_CaseInsensitive(string template, string expected) => Assert.That(Resolve(template, out _), Is.EqualTo(expected));


    [Test]
    public void Resolve_MixedTextAndTokens() =>
        Assert.That(Resolve("{processName} on {today} after {triggerData.InactiveDays} days", out _), Is.EqualTo("Win-back on 2026-10-11 after 90 days"));


    [TestCase("{unknown}", "unknown")]
    [TestCase("{contact.Missing}", "contact.Missing")]
    [TestCase("{triggerData.Missing}", "triggerData.Missing")]
    [TestCase("{triggerData.Identifier}", "triggerData.Identifier")]
    [TestCase("{now.Year}", "now.Year")]
    [TestCase("{other.Part}", "other.Part")]
    public void Resolve_UnknownTokenBecomesEmptyAndIsReported(string template, string reported)
    {
        string result = Resolve($"a{template}b", out var unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("ab"));
            Assert.That(unresolved, Is.EqualTo(new[] { reported }));
        });
    }


    [TestCase("{contact}")]
    [TestCase("{triggerData}")]
    public void Resolve_PrefixWithoutPartIsUnknown(string template)
    {
        Resolve(template, out var unresolved);

        Assert.That(unresolved, Has.Count.EqualTo(1));
    }


    [Test]
    public void Resolve_EmptyContactFieldIsResolvedWithoutWarning()
    {
        string result = Resolve("[{contact.AcmeEmpty}]", out var unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("[]"));
            Assert.That(unresolved, Is.Empty);
        });
    }


    [TestCase("Plain text")]
    [TestCase("{}")]
    [TestCase("{a b}")]
    [TestCase("{a.b.c}")]
    [TestCase("{1}")]
    [TestCase("{ now }")]
    [TestCase("{contact.}")]
    [TestCase("JSON {\"a\": 1}")]
    public void Resolve_TextWithoutTokensIsKept(string template)
    {
        string result = Resolve(template, out var unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(template));
            Assert.That(unresolved, Is.Empty);
        });
    }


    [TestCase(null)]
    [TestCase("")]
    public void Resolve_EmptyTemplateReturnsEmpty(string? template) => Assert.That(Resolve(template, out _), Is.Empty);


    [Test]
    public void Resolve_NoTriggerDataReportsTriggerDataTokens()
    {
        var unresolved = new List<string>();
        var values = CreateValues(triggerData: null);

        string result = ContactFieldValueTemplate.Resolve("{triggerData.InactiveDays}", values, unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Empty);
            Assert.That(unresolved, Is.EqualTo(new[] { "triggerData.InactiveDays" }));
        });
    }


    [Test]
    public void TriggerData_FormSubmission() => AssertTriggerData(
        new FormSubmissionTriggerData { BizFormId = 3, BizFormItemId = 17 },
        ("BizFormId", "3"), ("BizFormItemId", "17"));


    [Test]
    public void TriggerData_Registration() => AssertTriggerData(new RegistrationTriggerData { MemberId = 5 }, ("MemberId", "5"));


    [Test]
    public void TriggerData_CustomActivity() => AssertTriggerData(
        new CustomActivityTriggerData { ActivityType = "acme_webinar", ActivityItemId = 2, ActivityItemDetailId = 4, ActivityValue = "1.5", ActivityChannelId = 1 },
        ("ActivityType", "acme_webinar"), ("ActivityItemId", "2"), ("ActivityItemDetailId", "4"), ("ActivityValue", "1.5"), ("ActivityChannelId", "1"));


    [Test]
    public void TriggerData_CustomActivityNullValueIsEmpty() => AssertTriggerData(new CustomActivityTriggerData(), ("ActivityValue", ""));


    [Test]
    public void TriggerData_ContactDateAnniversary() => AssertTriggerData(
        new ContactDateAnniversaryTriggerData { FieldName = "ContactBirthday", DaysBefore = 7, AnniversaryDate = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc), Years = 40 },
        ("FieldName", "ContactBirthday"), ("DaysBefore", "7"), ("AnniversaryDate", "2026-10-18T00:00:00"), ("Years", "40"));


    [Test]
    public void TriggerData_ContactInactive() => AssertTriggerData(
        new ContactInactiveTriggerData { InactiveDays = 30, LastActivityDate = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc) },
        ("InactiveDays", "30"), ("LastActivityDate", "2026-09-11T00:00:00"));


    [Test]
    public void TriggerData_ContactGroupMember() => AssertTriggerData(
        new ContactGroupMemberTriggerData { ContactGroupName = "HighIntent", ContactGroupDisplayName = "High intent" },
        ("ContactGroupName", "HighIntent"), ("ContactGroupDisplayName", "High intent"));


    [Test]
    public void TriggerData_ProjectTriggerFromTriggerStore() => AssertTriggerData(
        new ProjectTriggers.DescribedTriggerData { Score = 5, Amount = 12.5m, Code = "B2" },
        ("Score", "5"), ("Amount", "12.5"), ("Code", "B2"));


    [Test]
    public void TriggerData_UnregisteredTypeIsNotRead()
    {
        var data = new UnknownTriggerData { Secret = "value" };
        var unresolved = new List<string>();

        string result = ContactFieldValueTemplate.Resolve("{triggerData.Secret}", CreateValues(data), unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(data.Secret, Is.Not.Empty);
            Assert.That(result, Is.Empty);
            Assert.That(unresolved, Is.EqualTo(new[] { "triggerData.Secret" }));
            Assert.That(TriggerDataTokens.TryGetValue(null, data, nameof(UnknownTriggerData.Secret), out _), Is.False);
        });
    }


    [Test]
    public void TriggerData_DataOfAnotherTriggerIsNotRead()
    {
        var unresolved = new List<string>();
        var values = new ContactFieldTokenValues
        {
            Trigger = RegisteredTriggers.Find(triggers, ContactInactiveTrigger.IDENTIFIER),
            TriggerData = new ContactGroupMemberTriggerData { ContactGroupName = "HighIntent" }
        };

        string result = ContactFieldValueTemplate.Resolve("{triggerData.ContactGroupName}", values, unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Empty);
            Assert.That(unresolved, Is.EqualTo(new[] { "triggerData.ContactGroupName" }));
        });
    }


    [Test]
    public void TriggerData_TriggerWithoutDataIsNotRead() => Assert.That(
        TriggerDataTokens.TryGetValue(RegisteredTriggers.Find(triggers, StubTriggerProvider.NO_DATA_TRIGGER), new ContactInactiveTriggerData(), "InactiveDays", out _),
        Is.False);


    private static void AssertTriggerData(IAutomationTriggerData data, params (string Property, string Expected)[] expectations) => Assert.Multiple(() =>
    {
        foreach (var (property, expected) in expectations)
        {
            var unresolved = new List<string>();
            string result = ContactFieldValueTemplate.Resolve($"{{triggerData.{property}}}", CreateValues(data), unresolved);

            Assert.That(result, Is.EqualTo(expected), property);
            Assert.That(unresolved, Is.Empty, property);
        }

        var missing = new List<string>();
        ContactFieldValueTemplate.Resolve("{triggerData.NoSuchProperty}", CreateValues(data), missing);
        Assert.That(missing, Is.EqualTo(new[] { "triggerData.NoSuchProperty" }));
    });


    private static string Resolve(string? template, out List<string> unresolved)
    {
        unresolved = [];

        return ContactFieldValueTemplate.Resolve(
            template,
            CreateValues(new ContactInactiveTriggerData { InactiveDays = 90, LastActivityDate = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc) }),
            unresolved);
    }


    private static ContactFieldTokenValues CreateValues(object? triggerData) => new()
    {
        Now = new DateTime(2026, 10, 11, 14, 5, 9, 123, DateTimeKind.Local),
        ProcessName = "Win-back",
        TriggerIdentifier = "Labs.AutomationExtensions.ContactInactive",
        ContactId = 42,
        ContactGuid = contactGuid,
        GetContactFieldValue = name => name.ToLowerInvariant() switch
        {
            "acmecounter" => "7",
            "acmeempty" => string.Empty,
            _ => null
        },
        Trigger = triggerData is null ? null : RegisteredTriggers.GetAll(triggers).FirstOrDefault(trigger => trigger.DataType == triggerData.GetType()),
        TriggerData = triggerData
    };


    private sealed class UnknownTriggerData : IAutomationTriggerData
    {
        public string Identifier => "Acme.Unknown";


        public string Secret { get; init; } = string.Empty;
    }
}
