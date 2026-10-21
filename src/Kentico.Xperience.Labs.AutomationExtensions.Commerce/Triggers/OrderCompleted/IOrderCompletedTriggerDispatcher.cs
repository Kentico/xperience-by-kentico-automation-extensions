using CMS.Commerce;
using CMS.ContactManagement;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.OrderCompleted;

/// <summary>
/// Fires <see cref="OrderCompletedTrigger"/> from site checkout code after an order is created.
/// </summary>
public interface IOrderCompletedTriggerDispatcher
{
    /// <summary>
    /// Fires the trigger for <paramref name="contact"/> with data of <paramref name="order"/>. Never throws for expected
    /// failures (the order already exists) – check the result instead.
    /// </summary>
    /// <param name="order">The created order.</param>
    /// <param name="contact">
    /// Contact of the visitor who placed the order, e.g. <c>ContactManagementContext.GetCurrentContact(false)</c>.
    /// Pass it explicitly: the library never looks up a contact from the customer, member, or email (anyone can check out
    /// with an email they don't own). <c>null</c> (not tracked, no consent) → the trigger isn't fired.
    /// </param>
    /// <param name="currency">Currency code of the order (e.g. <c>USD</c>). Xperience orders store no currency. Optional.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<OrderCompletedFireResult> Fire(OrderInfo order, ContactInfo? contact, string? currency, CancellationToken cancellationToken);
}


/// <summary>
/// Outcome of <see cref="IOrderCompletedTriggerDispatcher.Fire"/>.
/// </summary>
public enum OrderCompletedFireResult
{
    /// <summary>
    /// Trigger handed to the automation trigger queue. Processes still decide in <c>Evaluate</c> whether they start.
    /// </summary>
    Fired,

    /// <summary>
    /// Component disabled in configuration. Nothing loaded or fired.
    /// </summary>
    Disabled,

    /// <summary>
    /// No contact passed. Nothing fired.
    /// </summary>
    NoContact,

    /// <summary>
    /// Loading order details or firing failed. Error logged.
    /// </summary>
    Failed
}
