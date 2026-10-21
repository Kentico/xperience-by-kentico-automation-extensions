using CMS.Commerce;
using CMS.DataEngine;

using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.Triggers;

/// <summary>
/// Executing the queries requires an Xperience database, so they're covered by query shape and running-app validation.
/// </summary>
public class OrderCompletedDataSourceTests
{
    [Test]
    public void SelectEarlierOrder_SameCustomerLowerIdTopOne()
    {
        var query = OrderCompletedDataSource.SelectEarlierOrder(new ObjectQuery<OrderInfo>(), 5, 7);

        Assert.Multiple(() =>
        {
            Assert.That(query.SelectColumnsList.ToString(), Is.EqualTo("[OrderID]"));
            Assert.That(query.WhereCondition, Does.Match(@"^\[OrderCustomerID\] = @\w+ AND \[OrderID\] < @\w+$"));
            Assert.That(query.TopNRecords, Is.EqualTo(1));
            Assert.That(query.Parameters.Select(parameter => parameter.Value), Is.EquivalentTo(new object[] { 5, 7 }));
        });
    }


    [Test]
    public void SelectItemQuantities_FiltersByOrder()
    {
        var query = OrderCompletedDataSource.SelectItemQuantities(new ObjectQuery<OrderItemInfo>(), 7);

        Assert.Multiple(() =>
        {
            Assert.That(query.SelectColumnsList.ToString(), Is.EqualTo("[OrderItemQuantity]"));
            Assert.That(query.WhereCondition, Does.Match(@"^\[OrderItemOrderID\] = @\w+$"));
            Assert.That(query.Parameters.Select(parameter => parameter.Value), Is.EquivalentTo(new object[] { 7 }));
        });
    }


    [TestCase(0, ExpectedResult = 0)]
    [TestCase(-1, ExpectedResult = 0)]
    [TestCase(3, ExpectedResult = 3)]
    [TestCase(2.5, ExpectedResult = 3)]
    [TestCase(0.1, ExpectedResult = 1)]
    public int ToItemCount_RoundsUp(decimal quantity) => OrderCompletedDataSource.ToItemCount(quantity);


    [Test]
    public void ToItemCount_CapsAtIntMax() => Assert.That(OrderCompletedDataSource.ToItemCount(decimal.MaxValue), Is.EqualTo(int.MaxValue));
}
