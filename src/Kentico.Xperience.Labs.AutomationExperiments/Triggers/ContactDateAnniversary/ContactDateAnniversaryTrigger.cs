using System.Globalization;

using CMS.Automation;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExperiments.Configuration;
using Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

[assembly: RegisterAutomationTrigger<ContactDateAnniversaryTrigger>(
    ContactDateAnniversaryTrigger.IDENTIFIER,
    "labs.automationexperiments.contactdateanniversary.displayname",
    Description = "labs.automationexperiments.contactdateanniversary.description",
    IconName = Icons.Gift)]

namespace Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

/// <summary>
/// Starts a process on (or a configured number of days before) the yearly anniversary of a contact date field,
/// such as a birthday or signup date. Fired once per day by <see cref="AnniversaryTask"/>.
/// </summary>
public sealed class ContactDateAnniversaryTrigger(IAutomationComponentStatus componentStatus)
    : AutomationTrigger<ContactDateAnniversaryTriggerData, ContactDateAnniversaryTriggerProperties>
{
    public const string NAME = "ContactDateAnniversary";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private readonly IAutomationComponentStatus componentStatus = componentStatus;


    /// <inheritdoc />
    public override Task<bool> Evaluate(
        AutomationTriggerContext context,
        ContactDateAnniversaryTriggerProperties properties,
        ContactDateAnniversaryTriggerData triggerData,
        CancellationToken cancellationToken)
    {
        bool matches = componentStatus.IsEnabled(AutomationComponentCatalog.ContactDateAnniversary)
            && Matches(properties, triggerData);

        return Task.FromResult(matches);
    }


    /// <summary>
    /// Returns <c>true</c> when the fired anniversary matches the field and timing configured on the trigger.
    /// </summary>
    internal static bool Matches(ContactDateAnniversaryTriggerProperties properties, ContactDateAnniversaryTriggerData triggerData) =>
        string.Equals(properties.DateField, triggerData.FieldName, StringComparison.OrdinalIgnoreCase)
        && int.TryParse(properties.DaysBefore, NumberStyles.Integer, CultureInfo.InvariantCulture, out int daysBefore)
        && daysBefore == triggerData.DaysBefore;
}
