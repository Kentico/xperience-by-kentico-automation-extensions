using System.Reflection;

using CMS.Automation;
using CMS.Automation.Internal;
using CMS.ContactManagement;

using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.TestDoubles;

/// <summary>
/// Custom trigger store with the Order completed trigger, like a running application with the commerce package.
/// </summary>
internal sealed class StubTriggerProvider(params AutomationTriggerDefinition[] definitions) : ICustomAutomationTriggerProvider
{
    public static StubTriggerProvider WithOrderCompleted() => new(
        CreateDefinition(OrderCompletedTrigger.IDENTIFIER, "labs.automationextensions.ordercompleted.displayname", typeof(OrderCompletedTrigger)));


    /// <summary>
    /// Definitions are created by registration attributes in the application; the constructor is internal.
    /// </summary>
    private static AutomationTriggerDefinition CreateDefinition(string identifier, string displayName, Type triggerType)
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
}


/// <summary>
/// Creates <see cref="AutomationProcessContext"/> instances with a given trigger identifier and trigger data. The platform
/// exposes no public test factory; the context reads both through its (public) trigger data serializer.
/// </summary>
internal static class TriggerProcessContext
{
    public static AutomationProcessContext Create(string? triggerIdentifier, IAutomationTriggerData? triggerData)
    {
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        object?[] arguments =
        [
            new ContactInfo(),
            new AutomationProcessMetadata { DisplayName = "Order follow-up" },
            (Func<string>)(() => string.Empty),
            (Action<string>)(_ => { }),
            "state",
            new StubTriggerDataSerializer(triggerIdentifier, triggerData),
            NullLogger<AutomationProcessContext>.Instance
        ];

        return (AutomationProcessContext)constructor.Invoke(arguments);
    }


    private sealed class StubTriggerDataSerializer(string? triggerIdentifier, IAutomationTriggerData? triggerData) : IAutomationTriggerDataSerializer
    {
        public string Serialize(string triggerIdentifier, IAutomationTriggerData triggerData) => throw new NotSupportedException();


        public string GetTriggerIdentifier(string json) => triggerIdentifier!;


        public IAutomationTriggerData TryDeserialize(string json) => triggerData!;
    }
}
