using CMS.ContactManagement;

using Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.Triggers;

public class OrderCompletedTriggerDispatcherTests
{
    private readonly List<(ContactInfo Contact, OrderCompletedTriggerData Data)> fired = [];


    [SetUp]
    public void SetUp() => fired.Clear();


    [Test]
    public async Task Fire_Disabled_DoesNotLoadOrFire()
    {
        var dataSource = new FakeOrderCompletedDataSource(new OrderCompletedDetails(1, true));

        var result = await CreateDispatcher(dataSource, enabled: false).Fire(CreateOrder(), new ContactInfo(), "USD", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(OrderCompletedFireResult.Disabled));
            Assert.That(dataSource.Calls, Is.Zero);
            Assert.That(fired, Is.Empty);
        });
    }


    [Test]
    public async Task Fire_NoContact_DoesNotLoadOrFire()
    {
        var dataSource = new FakeOrderCompletedDataSource(new OrderCompletedDetails(1, true));

        var result = await CreateDispatcher(dataSource).Fire(CreateOrder(), null, "USD", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(OrderCompletedFireResult.NoContact));
            Assert.That(dataSource.Calls, Is.Zero);
            Assert.That(fired, Is.Empty);
        });
    }


    [Test]
    public async Task Fire_Contact_FiresForPassedContactWithOrderData()
    {
        var contact = new ContactInfo();

        var result = await CreateDispatcher(new FakeOrderCompletedDataSource(new OrderCompletedDetails(3, true)))
            .Fire(CreateOrder(), contact, " USD ", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(OrderCompletedFireResult.Fired));
            Assert.That(fired, Has.Count.EqualTo(1));
            Assert.That(fired[0].Contact, Is.SameAs(contact));
            Assert.That(fired[0].Data.OrderId, Is.EqualTo(7));
            Assert.That(fired[0].Data.CustomerId, Is.EqualTo(5));
            Assert.That(fired[0].Data.Total, Is.EqualTo(123.45m));
            Assert.That(fired[0].Data.Currency, Is.EqualTo("USD"));
            Assert.That(fired[0].Data.ItemCount, Is.EqualTo(3));
            Assert.That(fired[0].Data.IsFirstOrder, Is.True);
        });
    }


    [Test]
    public async Task Fire_NoCurrency_UsesEmptyText()
    {
        await CreateDispatcher(new FakeOrderCompletedDataSource(new OrderCompletedDetails(1, false)))
            .Fire(CreateOrder(), new ContactInfo(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(fired.Single().Data.Currency, Is.Empty);
            Assert.That(fired.Single().Data.IsFirstOrder, Is.False);
        });
    }


    [Test]
    public async Task Fire_DataSourceFails_ReturnsFailedWithoutThrowing()
    {
        var dataSource = new FakeOrderCompletedDataSource(new OrderCompletedDetails(1, true), new InvalidOperationException("db"));

        var result = await CreateDispatcher(dataSource).Fire(CreateOrder(), new ContactInfo(), "USD", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(OrderCompletedFireResult.Failed));
            Assert.That(fired, Is.Empty);
        });
    }


    [Test]
    public async Task Fire_TriggerDispatchFails_ReturnsFailed()
    {
        var dispatcher = new OrderCompletedTriggerDispatcher(
            (_, _, _) => throw new ArgumentException("queue"),
            new FakeOrderCompletedDataSource(new OrderCompletedDetails(1, true)),
            new FakeComponentStatus(true),
            NullLoggerFactory.Instance);

        var result = await dispatcher.Fire(CreateOrder(), new ContactInfo(), "USD", CancellationToken.None);

        Assert.That(result, Is.EqualTo(OrderCompletedFireResult.Failed));
    }


    [Test]
    public void Fire_Cancelled_Throws()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var dataSource = new FakeOrderCompletedDataSource(new OrderCompletedDetails(1, true), new OperationCanceledException(cancellation.Token));

        Assert.That(() => CreateDispatcher(dataSource).Fire(CreateOrder(), new ContactInfo(), "USD", cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }


    [Test]
    public void Fire_NullOrder_Throws() =>
        Assert.That(() => CreateDispatcher(new FakeOrderCompletedDataSource(new OrderCompletedDetails(1, true))).Fire((CMS.Commerce.OrderInfo)null!, new ContactInfo(), null, CancellationToken.None),
            Throws.ArgumentNullException);


    private OrderCompletedTriggerDispatcher CreateDispatcher(IOrderCompletedDataSource dataSource, bool enabled = true) =>
        new((contact, data, _) =>
        {
            fired.Add((contact, data));
            return Task.CompletedTask;
        }, dataSource, new FakeComponentStatus(enabled), NullLoggerFactory.Instance);


    private static CompletedOrder CreateOrder() => new(7, 5, 123.45m);
}
