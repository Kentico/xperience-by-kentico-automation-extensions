using CMS.Automation;
using CMS.Automation.Internal;

using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;

/// <summary>
/// Describes the trigger that started a process: the identifier plus, for data of the registered trigger
/// (<see cref="RegisteredTriggers"/>), the text of <see cref="ITriggerDataDescription.Describe"/> or a fixed description of
/// the built-in form submission, registration, and custom activity data. Only identifiers and non-personal values are included.
/// </summary>
internal static class TriggerDetailsDescriber
{
    public static async Task<string> Describe(
        AutomationProcessContext context,
        ICustomAutomationTriggerProvider triggerProvider,
        CancellationToken cancellationToken)
    {
        string identifier = await context.GetTriggerIdentifier(cancellationToken) ?? "unknown";
        var trigger = RegisteredTriggers.Find(triggerProvider, identifier);
        if (trigger?.DataType is null)
        {
            return identifier;
        }

        var triggerData = await context.GetTriggerData<IAutomationTriggerData>(cancellationToken);
        if (!trigger.IsDataOfTrigger(triggerData))
        {
            return identifier;
        }

        string? data;
        try
        {
            data = DescribeData(triggerData);
        }
        catch (Exception exception)
        {
            // Describe() is project code – a failure must not stop the log entry. The message isn't logged (may hold data).
            data = $"description failed: {exception.GetType().Name}";
        }

        return string.IsNullOrWhiteSpace(data) ? identifier : $"{identifier} ({data})";
    }


    private static string? DescribeData(IAutomationTriggerData? triggerData) => triggerData switch
    {
        ITriggerDataDescription described => described.Describe(),
        FormSubmissionTriggerData form => $"form {form.BizFormId}, record {form.BizFormItemId}",
        RegistrationTriggerData registration => $"member {registration.MemberId}",
        CustomActivityTriggerData activity => $"activity {activity.ActivityType}, item {activity.ActivityItemId}",
        _ => null
    };
}
