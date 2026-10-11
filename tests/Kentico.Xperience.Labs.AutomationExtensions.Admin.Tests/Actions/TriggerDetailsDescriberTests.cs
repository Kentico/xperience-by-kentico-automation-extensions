using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

/// <summary>
/// Pins the <c>{triggerDetails}</c> text of Write to log (behavior before the known trigger registry refactoring).
/// </summary>
public class TriggerDetailsDescriberTests
{
    private static readonly IEnumerable<TestCaseData> cases =
    [
        new TestCaseData(DefaultTriggerIdentifiers.FORM_SUBMISSION, new FormSubmissionTriggerData { BizFormId = 3, BizFormItemId = 17 })
            .Returns("Kentico.FormSubmissionTrigger (form 3, record 17)"),
        new TestCaseData(DefaultTriggerIdentifiers.REGISTRATION, new RegistrationTriggerData { MemberId = 5 })
            .Returns("Kentico.RegistrationTrigger (member 5)"),
        new TestCaseData(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, new CustomActivityTriggerData { ActivityType = "acme_webinar", ActivityItemId = 2 })
            .Returns("Kentico.CustomActivityTrigger (activity acme_webinar, item 2)"),
        new TestCaseData(ContactDateAnniversaryTrigger.IDENTIFIER, new ContactDateAnniversaryTriggerData
        {
            FieldName = "ContactBirthday",
            DaysBefore = 7,
            AnniversaryDate = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
            Years = 40
        }).Returns("Labs.AutomationExtensions.ContactDateAnniversary (field ContactBirthday, 7 day(s) before, 2026-10-18, 40 year(s))"),
        new TestCaseData(ContactInactiveTrigger.IDENTIFIER, new ContactInactiveTriggerData { InactiveDays = 30, LastActivityDate = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc) })
            .Returns("Labs.AutomationExtensions.ContactInactive (inactive 30 day(s), last activity 2026-09-11)"),
        new TestCaseData(ContactGroupMemberTrigger.IDENTIFIER, new ContactGroupMemberTriggerData { ContactGroupName = "HighIntent" })
            .Returns("Labs.AutomationExtensions.ContactGroupMember (contact group HighIntent)"),
        new TestCaseData(DefaultTriggerIdentifiers.FORM_SUBMISSION, null).Returns("Kentico.FormSubmissionTrigger"),
        new TestCaseData(DefaultTriggerIdentifiers.FORM_SUBMISSION, new RegistrationTriggerData { MemberId = 5 }).Returns("Kentico.FormSubmissionTrigger"),
        new TestCaseData("Acme.OrderCompleted", new FormSubmissionTriggerData { BizFormId = 3 }).Returns("Acme.OrderCompleted"),
        new TestCaseData(null, null).Returns("unknown")
    ];


    [TestCaseSource(nameof(cases))]
    public Task<string> Describe(string? identifier, IAutomationTriggerData? data) =>
        TriggerDetailsDescriber.Describe(TriggerProcessContext.Create(identifier, data), CancellationToken.None);
}
