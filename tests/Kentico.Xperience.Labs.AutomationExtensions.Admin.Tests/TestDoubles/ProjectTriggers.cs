using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Triggers;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

/// <summary>
/// Triggers of a project or another package (not known to the library at compile time): one whose data describes itself
/// (<see cref="ITriggerDataDescription"/>), one with plain data, and one whose description throws.
/// </summary>
internal static class ProjectTriggers
{
    public const string DESCRIBED = "Acme.DescribedTrigger";
    public const string PLAIN = "Acme.PlainTrigger";
    public const string THROWING = "Acme.ThrowingTrigger";


    /// <summary>
    /// Library triggers (<see cref="StubTriggerProvider.WithLibraryTriggers"/>) plus the project triggers.
    /// </summary>
    public static StubTriggerProvider CreateProvider() => new(
        StubTriggerProvider.WithLibraryTriggers().GetAll()
            .Append(StubTriggerProvider.CreateDefinition(DESCRIBED, "Acme described trigger", typeof(DescribedTrigger)))
            .Append(StubTriggerProvider.CreateDefinition(PLAIN, "Acme plain trigger", typeof(PlainTrigger)))
            .Append(StubTriggerProvider.CreateDefinition(THROWING, "Acme throwing trigger", typeof(ThrowingTrigger)))
            .ToArray());


    public sealed class DescribedTriggerData : IAutomationTriggerData, ITriggerDataDescription
    {
        public string Identifier => "Acme.DescribedTriggerData";


        public int Score { get; init; }


        public decimal Amount { get; init; }


        public string Code { get; init; } = string.Empty;


        public string Describe() => $"score {Score}, code {Code}";
    }


    public sealed class PlainTriggerData : IAutomationTriggerData
    {
        public string Identifier => "Acme.PlainTriggerData";


        public string Secret { get; init; } = string.Empty;
    }


    public sealed class ThrowingTriggerData : IAutomationTriggerData, ITriggerDataDescription
    {
        public string Identifier => "Acme.ThrowingTriggerData";


        public string Describe() => throw new InvalidOperationException("Contains secret@example.com");
    }


    public sealed class TriggerProperties : IAutomationTriggerProperties
    {
    }


    public sealed class DescribedTrigger : AutomationTrigger<DescribedTriggerData, TriggerProperties>
    {
        public override Task<bool> Evaluate(AutomationTriggerContext context, TriggerProperties properties, DescribedTriggerData triggerData, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }


    public sealed class PlainTrigger : AutomationTrigger<PlainTriggerData, TriggerProperties>
    {
        public override Task<bool> Evaluate(AutomationTriggerContext context, TriggerProperties properties, PlainTriggerData triggerData, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }


    public sealed class ThrowingTrigger : AutomationTrigger<ThrowingTriggerData, TriggerProperties>
    {
        public override Task<bool> Evaluate(AutomationTriggerContext context, TriggerProperties properties, ThrowingTriggerData triggerData, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
