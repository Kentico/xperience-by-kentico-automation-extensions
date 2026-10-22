using CMS.Automation;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

[assembly: RegisterAutomationTrigger<RecipientListMemberTrigger>(
    RecipientListMemberTrigger.IDENTIFIER,
    "Contact in recipient list (daily)",
    Description = "labs.automationextensions.recipientlistmember.description",
    IconName = Icons.ListBullets)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Starts a process for subscribed contacts of the configured recipient list (e.g. newsletter welcome series). Fired once
/// per day for every subscribed contact by <see cref="RecipientListMemberTask"/>.
/// </summary>
public sealed class RecipientListMemberTrigger(IAutomationComponentStatus componentStatus)
    : AutomationTrigger<RecipientListMemberTriggerData, RecipientListMemberTriggerProperties>
{
    public const string NAME = "RecipientListMember";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private readonly IAutomationComponentStatus componentStatus = componentStatus;


    /// <inheritdoc />
    public override Task<bool> Evaluate(
        AutomationTriggerContext context,
        RecipientListMemberTriggerProperties properties,
        RecipientListMemberTriggerData triggerData,
        CancellationToken cancellationToken)
    {
        bool matches = componentStatus.IsEnabled(AutomationComponentCatalog.RecipientListMember)
            && Matches(properties, triggerData);

        return Task.FromResult(matches);
    }


    /// <summary>
    /// Returns <c>true</c> when the fired recipient list is the list configured on the trigger (code names, case-insensitive).
    /// </summary>
    internal static bool Matches(RecipientListMemberTriggerProperties properties, RecipientListMemberTriggerData triggerData) =>
        !string.IsNullOrEmpty(properties.RecipientList)
        && string.Equals(properties.RecipientList, triggerData.RecipientListName, StringComparison.OrdinalIgnoreCase);
}
