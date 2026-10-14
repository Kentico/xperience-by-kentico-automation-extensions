using CMS.Commerce;
using CMS.DataEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

/// <summary>
/// Order details loaded from the database when <see cref="OrderCompletedTrigger"/> fires.
/// </summary>
/// <param name="ItemCount">Sum of order item quantities, rounded up.</param>
/// <param name="IsFirstOrder"><c>true</c> when the customer has no order with a lower ID.</param>
internal sealed record OrderCompletedDetails(int ItemCount, bool IsFirstOrder);


/// <summary>
/// Order values used by <see cref="OrderCompletedTrigger"/> (snapshot of <see cref="OrderInfo"/>).
/// </summary>
internal sealed record CompletedOrder(int OrderId, int CustomerId, decimal Total);


/// <summary>
/// Loads <see cref="OrderCompletedDetails"/>. Separate from the dispatcher so the firing logic is testable without a database.
/// </summary>
internal interface IOrderCompletedDataSource
{
    public Task<OrderCompletedDetails> GetDetails(CompletedOrder order, CancellationToken cancellationToken);
}


/// <inheritdoc />
internal sealed class OrderCompletedDataSource(
    IInfoProvider<OrderInfo> orderInfoProvider,
    IInfoProvider<OrderItemInfo> orderItemInfoProvider) : IOrderCompletedDataSource
{
    private readonly IInfoProvider<OrderInfo> orderInfoProvider = orderInfoProvider;
    private readonly IInfoProvider<OrderItemInfo> orderItemInfoProvider = orderItemInfoProvider;


    /// <inheritdoc />
    public async Task<OrderCompletedDetails> GetDetails(CompletedOrder order, CancellationToken cancellationToken)
    {
        var items = await SelectItemQuantities(orderItemInfoProvider.Get(), order.OrderId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
        int itemCount = ToItemCount(items.Sum(item => item.OrderItemQuantity));

        bool isFirstOrder = false;
        if (order.CustomerId > 0)
        {
            var earlierOrders = await SelectEarlierOrder(orderInfoProvider.Get(), order.CustomerId, order.OrderId)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
            isFirstOrder = !earlierOrders.Any();
        }

        return new OrderCompletedDetails(itemCount, isFirstOrder);
    }


    /// <summary>
    /// Selects the quantities of the order's items.
    /// </summary>
    internal static ObjectQuery<OrderItemInfo> SelectItemQuantities(ObjectQuery<OrderItemInfo> query, int orderId) => query
        .Columns(nameof(OrderItemInfo.OrderItemQuantity))
        .WhereEquals(nameof(OrderItemInfo.OrderItemOrderID), orderId);


    /// <summary>
    /// Selects at most one order of the customer created before <paramref name="orderId"/> (lower ID). Comparing IDs
    /// instead of dates keeps the result stable when the order is fired late and ignores later orders.
    /// </summary>
    internal static ObjectQuery<OrderInfo> SelectEarlierOrder(ObjectQuery<OrderInfo> query, int customerId, int orderId) => query
        .Columns(nameof(OrderInfo.OrderID))
        .WhereEquals(nameof(OrderInfo.OrderCustomerID), customerId)
        .WhereLessThan(nameof(OrderInfo.OrderID), orderId)
        .TopN(1);


    /// <summary>
    /// Rounds a quantity sum up to a whole, non-negative number of items (capped at <see cref="int.MaxValue"/>).
    /// </summary>
    internal static int ToItemCount(decimal quantity)
    {
        if (quantity <= 0)
        {
            return 0;
        }

        decimal rounded = decimal.Ceiling(quantity);

        return rounded >= int.MaxValue ? int.MaxValue : (int)rounded;
    }
}
