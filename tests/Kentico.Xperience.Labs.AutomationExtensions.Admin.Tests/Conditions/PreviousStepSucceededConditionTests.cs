using System.Reflection;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Labs.AutomationExtensions.Conditions.PreviousStepSucceeded;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class PreviousStepSucceededConditionTests
{
    [Test]
    public async Task Evaluate_DisabledConditionReturnsNoBeforeSkipAndResultLookup()
    {
        var condition = CreateCondition(enabled: false);
        var properties = new PreviousStepSucceededConditionProperties
        {
            SkipStep = true,
            SkippedBranch = "yes",
            SourceName = null!,
            ResultNumber = null!
        };
        var context = CreateContext();

        bool result = await condition.Evaluate(properties, context, CancellationToken.None);

        Assert.That(result, Is.False);
    }


    [Test]
    public async Task Evaluate_EnabledConditionWithMissingResultReturnsNo()
    {
        var condition = CreateCondition(enabled: true);
        var properties = new PreviousStepSucceededConditionProperties();
        var context = CreateContext();

        bool result = await condition.Evaluate(properties, context, CancellationToken.None);

        Assert.That(result, Is.False);
    }


    private static PreviousStepSucceededCondition CreateCondition(bool enabled) =>
        new(new StubComponentStatus(enabled), NullLoggerFactory.Instance);


    private static AutomationProcessContext CreateContext()
    {
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        object?[] arguments =
        [
            new ContactInfo(),
            new AutomationProcessMetadata(),
            (Func<string>)(() => string.Empty),
            (Action<string>)(_ => { }),
            string.Empty,
            null,
            NullLogger<AutomationProcessContext>.Instance
        ];

        return (AutomationProcessContext)constructor.Invoke(arguments);
    }


    private sealed class StubComponentStatus(bool enabled) : IAutomationComponentStatus
    {
        public bool IsEnabled(AutomationComponentDescriptor component) => enabled;
    }
}
