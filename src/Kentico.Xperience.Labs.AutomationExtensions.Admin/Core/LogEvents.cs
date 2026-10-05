using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Event identifiers of log entries written by the library. The name is shown in the Code column of the Xperience Event log.
/// </summary>
public static class LogEvents
{
    public static readonly EventId AutomationLog = new(41000, nameof(AutomationLog));
    public static readonly EventId ComponentStatus = new(41001, nameof(ComponentStatus));
    public static readonly EventId UnknownComponent = new(41002, nameof(UnknownComponent));
    public static readonly EventId ComponentDisabled = new(41003, nameof(ComponentDisabled));
    public static readonly EventId StepSkipped = new(41004, nameof(StepSkipped));
    public static readonly EventId WebhookFailed = new(41010, nameof(WebhookFailed));
    public static readonly EventId WebhookResponse = new(41011, nameof(WebhookResponse));
    public static readonly EventId StepResultMissing = new(41020, nameof(StepResultMissing));
    public static readonly EventId InvalidDateField = new(41030, nameof(InvalidDateField));
    public static readonly EventId AnniversaryTaskCompleted = new(41031, nameof(AnniversaryTaskCompleted));
    public static readonly EventId ContactScoreAdjusted = new(41040, nameof(ContactScoreAdjusted));
    public static readonly EventId InvalidContactScoreConfiguration = new(41041, nameof(InvalidContactScoreConfiguration));
    public static readonly EventId InvalidNumberCompareConfiguration = new(41050, nameof(InvalidNumberCompareConfiguration));
    public static readonly EventId ContactNumberFieldCompared = new(41051, nameof(ContactNumberFieldCompared));
}
