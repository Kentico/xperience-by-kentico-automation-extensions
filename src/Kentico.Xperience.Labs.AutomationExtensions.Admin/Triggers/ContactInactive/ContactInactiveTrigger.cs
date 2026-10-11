using System.Globalization;

using CMS.Automation;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

[assembly: RegisterAutomationTrigger<ContactInactiveTrigger>(
    ContactInactiveTrigger.IDENTIFIER,
    "labs.automationextensions.contactinactive.displayname",
    Description = "labs.automationextensions.contactinactive.description",
    IconName = Icons.Pause)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Starts a process when a contact's last activity was exactly the configured number of days ago (win-back).
/// Fired by <see cref="ContactInactiveTask"/>.
/// </summary>
public sealed class ContactInactiveTrigger(IAutomationComponentStatus componentStatus)
    : AutomationTrigger<ContactInactiveTriggerData, ContactInactiveTriggerProperties>
{
    public const string NAME = "ContactInactive";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private readonly IAutomationComponentStatus componentStatus = componentStatus;


    /// <inheritdoc />
    public override Task<bool> Evaluate(
        AutomationTriggerContext context,
        ContactInactiveTriggerProperties properties,
        ContactInactiveTriggerData triggerData,
        CancellationToken cancellationToken)
    {
        bool matches = componentStatus.IsEnabled(AutomationComponentCatalog.ContactInactive)
            && Matches(properties, triggerData);

        return Task.FromResult(matches);
    }


    /// <summary>
    /// Returns <c>true</c> when the fired number of inactive days matches the number configured on the trigger.
    /// </summary>
    internal static bool Matches(ContactInactiveTriggerProperties properties, ContactInactiveTriggerData triggerData) =>
        int.TryParse(properties.InactiveDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out int inactiveDays)
        && inactiveDays == triggerData.InactiveDays;
}
