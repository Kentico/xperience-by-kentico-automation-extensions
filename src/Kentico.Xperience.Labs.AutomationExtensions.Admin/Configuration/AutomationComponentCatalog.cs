using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.PreviousStepSucceeded;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.RandomSplit;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Configuration;

/// <summary>
/// List of all automation components provided by the library.
/// </summary>
public static class AutomationComponentCatalog
{
    /// <summary>
    /// Prefix of all component registration identifiers.
    /// </summary>
    public const string IDENTIFIER_PREFIX = "Labs.AutomationExtensions.";


    public static readonly AutomationComponentDescriptor ContactDateAnniversary = new(ContactDateAnniversaryTrigger.NAME, AutomationComponentKind.Trigger);
    public static readonly AutomationComponentDescriptor ContactGroupMember = new(ContactGroupMemberTrigger.NAME, AutomationComponentKind.Trigger);
    public static readonly AutomationComponentDescriptor ContactInactive = new(ContactInactiveTrigger.NAME, AutomationComponentKind.Trigger);
    public static readonly AutomationComponentDescriptor ActivityFrequency = new(ActivityFrequencyCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor ContactNumberFieldCompare = new(ContactNumberFieldCompareCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor PreviousStepSucceeded = new(PreviousStepSucceededCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor RandomSplit = new(RandomSplitCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor StartedByTrigger = new(StartedByTriggerCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor WithinTimeWindow = new(WithinTimeWindowCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor AdjustContactScore = new(AdjustContactScoreAction.NAME, AutomationComponentKind.Action);
    public static readonly AutomationComponentDescriptor SendWebhook = new(SendWebhookAction.NAME, AutomationComponentKind.Action);
    public static readonly AutomationComponentDescriptor SetContactField = new(SetContactFieldAction.NAME, AutomationComponentKind.Action);
    public static readonly AutomationComponentDescriptor WriteToLog = new(WriteToLogAction.NAME, AutomationComponentKind.Action);


    /// <summary>
    /// All components provided by the library.
    /// </summary>
    public static IReadOnlyList<AutomationComponentDescriptor> All { get; } =
    [
        ContactDateAnniversary,
        ContactGroupMember,
        ContactInactive,
        ActivityFrequency,
        ContactNumberFieldCompare,
        PreviousStepSucceeded,
        RandomSplit,
        StartedByTrigger,
        WithinTimeWindow,
        AdjustContactScore,
        SendWebhook,
        SetContactField,
        WriteToLog
    ];
}
