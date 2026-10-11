using System.Reflection;

using CMS.Automation;
using CMS.Automation.Internal;
using CMS.ContactManagement;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

/// <summary>
/// Creates <see cref="AutomationProcessContext"/> instances with a given trigger identifier and trigger data. The platform
/// exposes no public test factory; the context reads both through its (public) trigger data serializer.
/// </summary>
internal static class TriggerProcessContext
{
    public const string PROCESS_NAME = "Nurture";


    public static AutomationProcessContext Create(string? triggerIdentifier, IAutomationTriggerData? triggerData = null)
    {
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        object?[] arguments =
        [
            new ContactInfo(),
            new AutomationProcessMetadata { DisplayName = PROCESS_NAME },
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
