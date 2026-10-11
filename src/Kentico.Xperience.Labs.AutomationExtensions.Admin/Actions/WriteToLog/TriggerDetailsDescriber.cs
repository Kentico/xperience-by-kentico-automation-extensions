using System.Globalization;

using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;

/// <summary>
/// Describes the trigger that started a process, including data of built-in and library triggers (<see cref="KnownTriggers"/>).
/// Only identifiers and non-personal values are included.
/// </summary>
internal static class TriggerDetailsDescriber
{
    public static async Task<string> Describe(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        string identifier = await context.GetTriggerIdentifier(cancellationToken) ?? "unknown";
        string? data = await DescribeData(context, KnownTriggers.FindByIdentifier(identifier), cancellationToken);

        return data is null ? identifier : $"{identifier} ({data})";
    }


    private static async Task<string?> DescribeData(AutomationProcessContext context, KnownTrigger? trigger, CancellationToken cancellationToken)
    {
        if (trigger is null)
        {
            return null;
        }

        var triggerData = await context.GetTriggerData<IAutomationTriggerData>(cancellationToken);

        return triggerData switch
        {
            ContactDateAnniversaryTriggerData anniversary when trigger == KnownTriggers.ContactDateAnniversary =>
                string.Format(CultureInfo.InvariantCulture, "field {0}, {1} day(s) before, {2:yyyy-MM-dd}, {3} year(s)",
                    anniversary.FieldName, anniversary.DaysBefore, anniversary.AnniversaryDate, anniversary.Years),
            ContactGroupMemberTriggerData groupMember when trigger == KnownTriggers.ContactGroupMember =>
                $"contact group {groupMember.ContactGroupName}",
            ContactInactiveTriggerData inactive when trigger == KnownTriggers.ContactInactive =>
                string.Format(CultureInfo.InvariantCulture, "inactive {0} day(s), last activity {1:yyyy-MM-dd}",
                    inactive.InactiveDays, inactive.LastActivityDate),
            FormSubmissionTriggerData form when trigger == KnownTriggers.FormSubmission =>
                $"form {form.BizFormId}, record {form.BizFormItemId}",
            RegistrationTriggerData registration when trigger == KnownTriggers.Registration =>
                $"member {registration.MemberId}",
            CustomActivityTriggerData activity when trigger == KnownTriggers.CustomActivity =>
                $"activity {activity.ActivityType}, item {activity.ActivityItemId}",
            _ => null
        };
    }
}
