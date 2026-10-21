using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.TestDoubles;

internal sealed class FakeOrderCompletedDataSource(OrderCompletedDetails details, Exception? exception = null) : IOrderCompletedDataSource
{
    public int Calls { get; private set; }


    public Task<OrderCompletedDetails> GetDetails(CompletedOrder order, CancellationToken cancellationToken)
    {
        Calls++;

        return exception is null ? Task.FromResult(details) : Task.FromException<OrderCompletedDetails>(exception);
    }
}
