using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

namespace Kentico.Xperience.Labs.AutomationExtensions.Configuration;

/// <summary>
/// Automation components provided by the commerce assembly. Added to the shared catalog by
/// <c>AddAutomationExtensionsCommerce</c>.
/// </summary>
public static class CommerceAutomationComponents
{
    public static readonly AutomationComponentDescriptor OrderCompleted = new(OrderCompletedTrigger.NAME, AutomationComponentKind.Trigger);


    /// <summary>
    /// All components provided by the commerce assembly.
    /// </summary>
    public static IReadOnlyList<AutomationComponentDescriptor> All { get; } =
    [
        OrderCompleted
    ];
}
