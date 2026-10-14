using System.Globalization;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.Triggers;

/// <summary>
/// Order completed trigger data in steps of the admin assembly, which doesn't reference Commerce: Write to log trigger
/// details (<see cref="ITriggerDataDescription"/>) and <c>{triggerData.X}</c> tokens of Set contact field (trigger store).
/// </summary>
public class OrderCompletedTriggerDataTests
{
    private static readonly OrderCompletedTriggerData data = new()
    {
        OrderId = 42,
        CustomerId = 7,
        Total = 25m,
        Currency = "USD",
        ItemCount = 3,
        IsFirstOrder = true
    };


    [Test]
    public void Describe_IdentifiersAndAmountsOnly() =>
        Assert.That(data.Describe(), Is.EqualTo("order 42, customer 7, total 25.00 USD, 3 item(s), first order"));


    [TestCase(1234.5, "", false, ExpectedResult = "order 42, customer 7, total 1234.50, 0 item(s), repeat order")]
    [TestCase(9.999, " eur ", true, ExpectedResult = "order 42, customer 7, total 9.999 eur, 0 item(s), first order")]
    [TestCase(0, "USD", false, ExpectedResult = "order 42, customer 7, total 0.00 USD, 0 item(s), repeat order")]
    public string Describe_TotalCurrencyAndOrderKind(decimal total, string currency, bool isFirstOrder) =>
        new OrderCompletedTriggerData { OrderId = 42, CustomerId = 7, Total = total, Currency = currency, IsFirstOrder = isFirstOrder }.Describe();


    [Test]
    public void Describe_InvariantCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            Assert.That(new OrderCompletedTriggerData { OrderId = 1234, Total = 1234.5m }.Describe(), Is.EqualTo("order 1234, customer 0, total 1234.50, 0 item(s), repeat order"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }


    [Test]
    public async Task WriteToLog_TriggerDetailsUseDescription()
    {
        var context = TriggerProcessContext.Create(OrderCompletedTrigger.IDENTIFIER, data);

        string text = await TriggerDetailsDescriber.Describe(context, StubTriggerProvider.WithOrderCompleted(), CancellationToken.None);

        Assert.That(text, Is.EqualTo("Labs.AutomationExtensions.OrderCompleted (order 42, customer 7, total 25.00 USD, 3 item(s), first order)"));
    }


    [Test]
    public async Task WriteToLog_UnregisteredTriggerIsIdentifierOnly()
    {
        var context = TriggerProcessContext.Create(OrderCompletedTrigger.IDENTIFIER, data);

        string text = await TriggerDetailsDescriber.Describe(context, new StubTriggerProvider(), CancellationToken.None);

        Assert.That(text, Is.EqualTo(OrderCompletedTrigger.IDENTIFIER));
    }


    [TestCase("OrderId", "42")]
    [TestCase("CustomerId", "7")]
    [TestCase("Total", "25")]
    [TestCase("Currency", "USD")]
    [TestCase("ItemCount", "3")]
    [TestCase("IsFirstOrder", "true")]
    [TestCase("isfirstorder", "true")]
    public void Tokens_ReadRegisteredOrderCompletedData(string property, string expected)
    {
        var unresolved = new List<string>();

        string result = ContactFieldValueTemplate.Resolve($"{{triggerData.{property}}}", CreateValues(StubTriggerProvider.WithOrderCompleted()), unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(unresolved, Is.Empty);
        });
    }


    [Test]
    public void Tokens_UnregisteredTriggerIsEmptyAndReported()
    {
        var unresolved = new List<string>();

        string result = ContactFieldValueTemplate.Resolve("[{triggerData.OrderId}]", CreateValues(new StubTriggerProvider()), unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("[]"));
            Assert.That(unresolved, Is.EqualTo(new[] { "triggerData.OrderId" }));
        });
    }


    private static ContactFieldTokenValues CreateValues(StubTriggerProvider triggerProvider) => new()
    {
        TriggerIdentifier = OrderCompletedTrigger.IDENTIFIER,
        Trigger = RegisteredTriggers.Find(triggerProvider, OrderCompletedTrigger.IDENTIFIER),
        TriggerData = data
    };
}
