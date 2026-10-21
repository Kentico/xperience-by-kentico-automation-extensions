using Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.Triggers;

public class OrderCompletedTriggerTests
{
    [TestCase(0, 0, ExpectedResult = true)]
    [TestCase(0, 10.5, ExpectedResult = true)]
    [TestCase(50, 49.99, ExpectedResult = false)]
    [TestCase(50, 50, ExpectedResult = true)]
    [TestCase(50, 50.01, ExpectedResult = true)]
    public bool Matches_MinimumTotalInclusive(decimal minimumTotal, decimal total) =>
        OrderCompletedTrigger.Matches(new OrderCompletedTriggerProperties { MinimumTotal = minimumTotal }, new OrderCompletedTriggerData { Total = total });


    [TestCase(null, "USD", ExpectedResult = true)]
    [TestCase("", "USD", ExpectedResult = true)]
    [TestCase("  ", "", ExpectedResult = true)]
    [TestCase("USD", "USD", ExpectedResult = true)]
    [TestCase(" usd ", "USD", ExpectedResult = true)]
    [TestCase("USD", "EUR", ExpectedResult = false)]
    [TestCase("USD", "", ExpectedResult = false)]
    public bool Matches_CurrencyEmptyMeansAny(string? configured, string fired) =>
        OrderCompletedTrigger.Matches(new OrderCompletedTriggerProperties { Currency = configured }, new OrderCompletedTriggerData { Currency = fired });


    [TestCase(false, false, ExpectedResult = true)]
    [TestCase(false, true, ExpectedResult = true)]
    [TestCase(true, true, ExpectedResult = true)]
    [TestCase(true, false, ExpectedResult = false)]
    public bool Matches_FirstOrderOnly(bool firstOrderOnly, bool isFirstOrder) =>
        OrderCompletedTrigger.Matches(new OrderCompletedTriggerProperties { FirstOrderOnly = firstOrderOnly }, new OrderCompletedTriggerData { IsFirstOrder = isFirstOrder });


    [Test]
    public void Matches_AllConditionsMustHold()
    {
        var properties = new OrderCompletedTriggerProperties { MinimumTotal = 100, Currency = "USD", FirstOrderOnly = true };

        Assert.Multiple(() =>
        {
            Assert.That(OrderCompletedTrigger.Matches(properties, new OrderCompletedTriggerData { Total = 100, Currency = "USD", IsFirstOrder = true }), Is.True);
            Assert.That(OrderCompletedTrigger.Matches(properties, new OrderCompletedTriggerData { Total = 99, Currency = "USD", IsFirstOrder = true }), Is.False);
            Assert.That(OrderCompletedTrigger.Matches(properties, new OrderCompletedTriggerData { Total = 100, Currency = "EUR", IsFirstOrder = true }), Is.False);
            Assert.That(OrderCompletedTrigger.Matches(properties, new OrderCompletedTriggerData { Total = 100, Currency = "USD", IsFirstOrder = false }), Is.False);
        });
    }


    [TestCase(true, ExpectedResult = true)]
    [TestCase(false, ExpectedResult = false)]
    public async Task<bool> Evaluate_RequiresEnabledComponent(bool enabled)
    {
        var trigger = new OrderCompletedTrigger(new FakeComponentStatus(enabled));

        return await trigger.Evaluate(null!, new OrderCompletedTriggerProperties(), new OrderCompletedTriggerData(), CancellationToken.None);
    }


    [Test]
    public void Properties_DefaultToAnyOrder()
    {
        var properties = new OrderCompletedTriggerProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.MinimumTotal, Is.Zero);
            Assert.That(properties.Currency, Is.Null);
            Assert.That(properties.FirstOrderOnly, Is.False);
        });
    }


    [Test]
    public void Identifiers_AreStable() =>
        Assert.That(new[] { OrderCompletedTrigger.IDENTIFIER, new OrderCompletedTriggerData().Identifier }, Is.EqualTo(new[]
        {
            "Labs.AutomationExtensions.OrderCompleted",
            "Labs.AutomationExtensions.OrderCompletedTriggerData"
        }));


    [Test]
    public void TriggerData_HasNoPersonalOrProductFields()
    {
        var names = typeof(OrderCompletedTriggerData).GetProperties().Select(property => property.Name);

        Assert.That(names, Is.EquivalentTo(new[] { "Identifier", "OrderId", "CustomerId", "Total", "Currency", "ItemCount", "IsFirstOrder" }));
    }
}
