using CMS.Automation;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

[assembly: RegisterAutomationTrigger<ContactGroupMemberTrigger>(
    ContactGroupMemberTrigger.IDENTIFIER,
    "Contact in contact group (daily)",
    Description = "labs.automationextensions.contactgroupmember.description",
    IconName = Icons.Users)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Starts a process for members of the configured contact group (segment-based nurture). Fired once per day for every
/// member by <see cref="ContactGroupMemberTask"/>.
/// </summary>
public sealed class ContactGroupMemberTrigger(IAutomationComponentStatus componentStatus)
    : AutomationTrigger<ContactGroupMemberTriggerData, ContactGroupMemberTriggerProperties>
{
    public const string NAME = "ContactGroupMember";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private readonly IAutomationComponentStatus componentStatus = componentStatus;


    /// <inheritdoc />
    public override Task<bool> Evaluate(
        AutomationTriggerContext context,
        ContactGroupMemberTriggerProperties properties,
        ContactGroupMemberTriggerData triggerData,
        CancellationToken cancellationToken)
    {
        bool matches = componentStatus.IsEnabled(AutomationComponentCatalog.ContactGroupMember)
            && Matches(properties, triggerData);

        return Task.FromResult(matches);
    }


    /// <summary>
    /// Returns <c>true</c> when the fired contact group is the group configured on the trigger (code names, case-insensitive).
    /// </summary>
    internal static bool Matches(ContactGroupMemberTriggerProperties properties, ContactGroupMemberTriggerData triggerData) =>
        !string.IsNullOrEmpty(properties.ContactGroup)
        && string.Equals(properties.ContactGroup, triggerData.ContactGroupName, StringComparison.OrdinalIgnoreCase);
}
