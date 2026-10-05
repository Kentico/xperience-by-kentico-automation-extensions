using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.PreviousStepSucceeded;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.RandomSplit;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

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
    public static readonly AutomationComponentDescriptor PreviousStepSucceeded = new(PreviousStepSucceededCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor RandomSplit = new(RandomSplitCondition.NAME, AutomationComponentKind.Condition);
    public static readonly AutomationComponentDescriptor SendWebhook = new(SendWebhookAction.NAME, AutomationComponentKind.Action);
    public static readonly AutomationComponentDescriptor WriteToLog = new(WriteToLogAction.NAME, AutomationComponentKind.Action);


    /// <summary>
    /// All components provided by the library.
    /// </summary>
    public static IReadOnlyList<AutomationComponentDescriptor> All { get; } =
    [
        ContactDateAnniversary,
        PreviousStepSucceeded,
        RandomSplit,
        SendWebhook,
        WriteToLog
    ];
}
