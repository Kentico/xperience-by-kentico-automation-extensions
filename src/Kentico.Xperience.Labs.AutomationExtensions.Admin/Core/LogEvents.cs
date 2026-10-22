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
    public static readonly EventId InvalidActivityFrequencyConfiguration = new(41060, nameof(InvalidActivityFrequencyConfiguration));
    public static readonly EventId ActivityFrequencyEvaluated = new(41061, nameof(ActivityFrequencyEvaluated));
    public static readonly EventId ActivityUrlLimitExceeded = new(41062, nameof(ActivityUrlLimitExceeded));
    public static readonly EventId UrlMatchTimeout = new(41063, nameof(UrlMatchTimeout));
    public static readonly EventId ContactInactiveTaskCompleted = new(41070, nameof(ContactInactiveTaskCompleted));
    public static readonly EventId InvalidSetContactFieldConfiguration = new(41080, nameof(InvalidSetContactFieldConfiguration));
    public static readonly EventId SetContactFieldTokenUnresolved = new(41081, nameof(SetContactFieldTokenUnresolved));
    public static readonly EventId SetContactFieldValueTruncated = new(41082, nameof(SetContactFieldValueTruncated));
    public static readonly EventId SetContactFieldFailed = new(41083, nameof(SetContactFieldFailed));
    public static readonly EventId ContactFieldSet = new(41084, nameof(ContactFieldSet));
    public static readonly EventId ContactGroupMemberTaskCompleted = new(41090, nameof(ContactGroupMemberTaskCompleted));
    public static readonly EventId ContactGroupNotFound = new(41091, nameof(ContactGroupNotFound));
    public static readonly EventId ContactGroupIsRecipientList = new(41092, nameof(ContactGroupIsRecipientList));
    public static readonly EventId InvalidStartedByTriggerConfiguration = new(41100, nameof(InvalidStartedByTriggerConfiguration));
    public static readonly EventId StartedByTriggerDataMissing = new(41101, nameof(StartedByTriggerDataMissing));
    public static readonly EventId ChatMessagePosted = new(41120, nameof(ChatMessagePosted));
    public static readonly EventId ChatMessageFailed = new(41121, nameof(ChatMessageFailed));
    public static readonly EventId ChatMessageTokensUnresolved = new(41122, nameof(ChatMessageTokensUnresolved));
    public static readonly EventId ChatMessageResponse = new(41123, nameof(ChatMessageResponse));
    public static readonly EventId ChatMessageProcessLinkSkipped = new(41124, nameof(ChatMessageProcessLinkSkipped));
    public static readonly EventId RecipientListMemberTaskCompleted = new(41130, nameof(RecipientListMemberTaskCompleted));
    public static readonly EventId RecipientListNotFound = new(41131, nameof(RecipientListNotFound));
    public static readonly EventId RecipientListIsContactGroup = new(41132, nameof(RecipientListIsContactGroup));
}
