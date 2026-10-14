using System.Globalization;

using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

/// <summary>
/// Payload of <see cref="OrderCompletedTrigger"/>. Available to later steps via <c>GetTriggerData</c>.
/// Holds identifiers and amounts only – no personal data and no product references.
/// </summary>
public sealed class OrderCompletedTriggerData : IAutomationTriggerData, ITriggerDataDescription
{
    /// <inheritdoc />
    public string Identifier => "Labs.AutomationExtensions.OrderCompletedTriggerData";


    /// <summary>
    /// ID of the order (<c>OrderInfo.OrderID</c>).
    /// </summary>
    public int OrderId { get; init; }


    /// <summary>
    /// ID of the order's customer (<c>OrderInfo.OrderCustomerID</c>).
    /// </summary>
    public int CustomerId { get; init; }


    /// <summary>
    /// Grand total of the order including tax and shipping (<c>OrderInfo.OrderGrandTotal</c>).
    /// </summary>
    public decimal Total { get; init; }


    /// <summary>
    /// Currency code reported by the firing code (Xperience orders store no currency). Empty when unknown.
    /// </summary>
    public string Currency { get; init; } = string.Empty;


    /// <summary>
    /// Number of purchased units: sum of order item quantities, rounded up to a whole number.
    /// </summary>
    public int ItemCount { get; init; }


    /// <summary>
    /// <c>true</c> when the customer had no earlier order when the trigger fired.
    /// </summary>
    public bool IsFirstOrder { get; init; }


    /// <inheritdoc />
    /// <remarks>For example <c>order 42, customer 7, total 25.00 USD, 3 item(s), first order</c> (invariant culture).</remarks>
    public string Describe()
    {
        string total = Total.ToString("0.00##", CultureInfo.InvariantCulture);
        string currency = string.IsNullOrWhiteSpace(Currency) ? string.Empty : " " + Currency.Trim();
        string orderKind = IsFirstOrder ? "first order" : "repeat order";

        return string.Format(CultureInfo.InvariantCulture, "order {0}, customer {1}, total {2}{3}, {4} item(s), {5}", OrderId, CustomerId, total, currency, ItemCount, orderKind);
    }
}
