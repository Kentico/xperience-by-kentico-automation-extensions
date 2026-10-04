using System.Globalization;

using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

namespace Kentico.Xperience.Labs.AutomationExperiments.Actions.WriteToLog;

/// <summary>
/// Describes the trigger that started a process, including data of built-in and library triggers.
/// Only identifiers and non-personal values are included.
/// </summary>
internal static class TriggerDetailsDescriber
{
    public static async Task<string> Describe(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        string identifier = await context.GetTriggerIdentifier(cancellationToken) ?? "unknown";
        string? data = await DescribeData(context, identifier, cancellationToken);

        return data is null ? identifier : $"{identifier} ({data})";
    }


    private static async Task<string?> DescribeData(AutomationProcessContext context, string identifier, CancellationToken cancellationToken)
    {
        if (string.Equals(identifier, ContactDateAnniversaryTrigger.IDENTIFIER, StringComparison.Ordinal))
        {
            var anniversary = await context.GetTriggerData<ContactDateAnniversaryTriggerData>(cancellationToken);

            return anniversary is null
                ? null
                : string.Format(CultureInfo.InvariantCulture, "field {0}, {1} day(s) before, {2:yyyy-MM-dd}, {3} year(s)",
                    anniversary.FieldName, anniversary.DaysBefore, anniversary.AnniversaryDate, anniversary.Years);
        }

        if (await context.TriggeredByFormSubmission(cancellationToken))
        {
            var form = await context.GetTriggerData<FormSubmissionTriggerData>(cancellationToken);

            return form is null ? null : $"form {form.BizFormId}, record {form.BizFormItemId}";
        }

        if (await context.TriggeredByRegistration(cancellationToken))
        {
            var registration = await context.GetTriggerData<RegistrationTriggerData>(cancellationToken);

            return registration is null ? null : $"member {registration.MemberId}";
        }

        if (await context.TriggeredByCustomActivity(cancellationToken))
        {
            var activity = await context.GetTriggerData<CustomActivityTriggerData>(cancellationToken);

            return activity is null ? null : $"activity {activity.ActivityType}, item {activity.ActivityItemId}";
        }

        return null;
    }
}
