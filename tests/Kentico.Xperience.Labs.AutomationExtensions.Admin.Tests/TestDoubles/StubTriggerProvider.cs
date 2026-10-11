using System.Reflection;

using CMS.Automation;
using CMS.Automation.Internal;

using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

/// <summary>
/// Custom trigger store with the library triggers and a plain-text-named trigger without data, like a running application
/// with the library and a project trigger.
/// </summary>
internal sealed class StubTriggerProvider(params AutomationTriggerDefinition[] definitions) : ICustomAutomationTriggerProvider
{
    public const string NO_DATA_TRIGGER = "Acme.NoDataTrigger";


    public static StubTriggerProvider WithLibraryTriggers() => new(
        CreateDefinition(ContactDateAnniversaryTrigger.IDENTIFIER, "labs.automationextensions.contactdateanniversary.displayname", typeof(ContactDateAnniversaryTrigger)),
        CreateDefinition(ContactInactiveTrigger.IDENTIFIER, "labs.automationextensions.contactinactive.displayname", typeof(ContactInactiveTrigger)),
        CreateDefinition(ContactGroupMemberTrigger.IDENTIFIER, "labs.automationextensions.contactgroupmember.displayname", typeof(ContactGroupMemberTrigger)),
        CreateDefinition(NO_DATA_TRIGGER, "Acme trigger without data", typeof(NoDataTrigger)));


    /// <summary>
    /// Definitions are created by registration attributes in the application; the constructor is internal.
    /// </summary>
    public static AutomationTriggerDefinition CreateDefinition(string identifier, string displayName, Type triggerType)
    {
        var constructor = typeof(AutomationTriggerDefinition)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        return (AutomationTriggerDefinition)constructor.Invoke([identifier, displayName, triggerType, null, null, "om.contact"]);
    }


    public bool TryGet(string identifier, out AutomationTriggerDefinition definition)
    {
        definition = definitions.FirstOrDefault(item => item.Identifier == identifier)!;
        return definition is not null;
    }


    public AutomationTriggerDefinition Get(string identifier) => definitions.Single(item => item.Identifier == identifier);


    public AutomationTriggerDefinition Get(Type triggerType) => definitions.Single(item => item.TriggerType == triggerType);


    public IEnumerable<AutomationTriggerDefinition> GetAll() => definitions;


    public sealed class NoDataTrigger : AutomationTrigger
    {
    }
}
