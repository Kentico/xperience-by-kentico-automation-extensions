using CMS.Automation;
using CMS.Commerce;
using CMS.ContactManagement;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

/// <inheritdoc />
internal sealed class OrderCompletedTriggerDispatcher : IOrderCompletedTriggerDispatcher
{
    private readonly Func<ContactInfo, OrderCompletedTriggerData, CancellationToken, Task> fireTrigger;
    private readonly IOrderCompletedDataSource dataSource;
    private readonly IAutomationComponentStatus componentStatus;
    private readonly ILogger logger;


    public OrderCompletedTriggerDispatcher(
        IAutomationTriggerDispatcher triggerDispatcher,
        IOrderCompletedDataSource dataSource,
        IAutomationComponentStatus componentStatus,
        ILoggerFactory loggerFactory)
        : this((contact, triggerData, cancellationToken) =>
            triggerDispatcher.FireTrigger<OrderCompletedTrigger>(new AutomationTriggerDispatch(contact, triggerData), cancellationToken),
            dataSource, componentStatus, loggerFactory)
    {
    }


    /// <summary>
    /// Test seam: <see cref="IAutomationTriggerDispatcher"/> can't be implemented outside Xperience.
    /// </summary>
    internal OrderCompletedTriggerDispatcher(
        Func<ContactInfo, OrderCompletedTriggerData, CancellationToken, Task> fireTrigger,
        IOrderCompletedDataSource dataSource,
        IAutomationComponentStatus componentStatus,
        ILoggerFactory loggerFactory)
    {
        this.fireTrigger = fireTrigger;
        this.dataSource = dataSource;
        this.componentStatus = componentStatus;
        logger = loggerFactory.CreateLogger(LogCategories.For(CommerceAutomationComponents.OrderCompleted));
    }


    /// <inheritdoc />
    public Task<OrderCompletedFireResult> Fire(OrderInfo order, ContactInfo? contact, string? currency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);

        return Fire(new CompletedOrder(order.OrderID, order.OrderCustomerID, order.OrderGrandTotal), contact, currency, cancellationToken);
    }


    /// <summary>
    /// Fires for an order snapshot. Info object fields can't be set without an Xperience database, so tests use this overload.
    /// </summary>
    internal async Task<OrderCompletedFireResult> Fire(CompletedOrder order, ContactInfo? contact, string? currency, CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(CommerceAutomationComponents.OrderCompleted))
        {
            logger.LogDebug(CommerceLogEvents.OrderCompletedNotFired,
                "Order completed trigger not fired for order {OrderId}: component disabled ('{ConfigurationPath}').",
                order.OrderId, CommerceAutomationComponents.OrderCompleted.ConfigurationPath);

            return OrderCompletedFireResult.Disabled;
        }

        // AutomationTriggerDispatch throws for a null contact. The contact is never looked up from the order's customer.
        if (contact is null)
        {
            logger.LogDebug(CommerceLogEvents.OrderCompletedNotFired,
                "Order completed trigger not fired for order {OrderId}: no contact passed (visitor not tracked or no consent).",
                order.OrderId);

            return OrderCompletedFireResult.NoContact;
        }

        try
        {
            var details = await dataSource.GetDetails(order, cancellationToken);
            var triggerData = new OrderCompletedTriggerData
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                Total = order.Total,
                Currency = currency?.Trim() ?? string.Empty,
                ItemCount = details.ItemCount,
                IsFirstOrder = details.IsFirstOrder
            };

            await fireTrigger(contact, triggerData, cancellationToken);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(CommerceLogEvents.OrderCompletedFired,
                    "Order completed trigger fired for order {OrderId}, contact {ContactId} (first order: {IsFirstOrder}).",
                    order.OrderId, contact.ContactID, details.IsFirstOrder);
            }

            return OrderCompletedFireResult.Fired;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            // The order already exists – don't fail the checkout.
            LogFailed(order.OrderId, contact, exception.GetType());

            return OrderCompletedFireResult.Failed;
        }
    }


    /// <summary>
    /// Logs a failure without the exception message: database errors can contain values (personal data).
    /// </summary>
    private void LogFailed(int orderId, ContactInfo contact, Type exceptionType)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(CommerceLogEvents.OrderCompletedFailed,
                "Order completed trigger failed for order {OrderId}, contact {ContactId}: {ExceptionType}.",
                orderId, contact.ContactID, exceptionType.FullName);
        }
    }
}
