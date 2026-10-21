using CMS.Automation;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

[assembly: RegisterAutomationTrigger<OrderCompletedTrigger>(
    OrderCompletedTrigger.IDENTIFIER,
    "Order completed",
    Description = "labs.automationextensions.ordercompleted.description",
    IconName = Icons.ShoppingCart)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

/// <summary>
/// Starts a process when a visitor completes an order. Fired by site checkout code through
/// <see cref="IOrderCompletedTriggerDispatcher"/>.
/// </summary>
public sealed class OrderCompletedTrigger(IAutomationComponentStatus componentStatus)
    : AutomationTrigger<OrderCompletedTriggerData, OrderCompletedTriggerProperties>
{
    public const string NAME = "OrderCompleted";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;

    /// <summary>
    /// Maximum length of the currency code entered by marketers.
    /// </summary>
    public const int CURRENCY_MAX_LENGTH = 10;


    private readonly IAutomationComponentStatus componentStatus = componentStatus;


    /// <inheritdoc />
    public override Task<bool> Evaluate(
        AutomationTriggerContext context,
        OrderCompletedTriggerProperties properties,
        OrderCompletedTriggerData triggerData,
        CancellationToken cancellationToken)
    {
        bool matches = componentStatus.IsEnabled(CommerceAutomationComponents.OrderCompleted)
            && Matches(properties, triggerData);

        return Task.FromResult(matches);
    }


    /// <summary>
    /// Returns <c>true</c> when the order meets the minimum total, has the selected currency (if any), and – when
    /// <see cref="OrderCompletedTriggerProperties.FirstOrderOnly"/> is set – is the customer's first order.
    /// </summary>
    internal static bool Matches(OrderCompletedTriggerProperties properties, OrderCompletedTriggerData triggerData)
    {
        string? currency = properties.Currency?.Trim();

        return triggerData.Total >= properties.MinimumTotal
            && (string.IsNullOrEmpty(currency) || string.Equals(currency, triggerData.Currency?.Trim(), StringComparison.OrdinalIgnoreCase))
            && (!properties.FirstOrderOnly || triggerData.IsFirstOrder);
    }
}
