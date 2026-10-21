# Implementation spec: Order completed

## Selection

Trigger #6 from `PLAN.md` (3.1). Design settled by user 2026-10-13 (`PLAN.md` section 7 decision). #24 and its product
resolver are **out of scope**.

## User decisions (2026-10-13)

- Trigger with data + marketer properties: *Minimum order total*, *Currency*, *First order only*.
- Trigger data: order ID, customer ID, total, currency, item count. No product IDs, no personal data.
- Firing helper (public API) called by site checkout code with order + contact. **Contact passed explicitly by caller**;
  library never resolves a contact from customer, member or email. No contact → no fire, log Debug, no exception.
- *First order only* = no earlier order for the same customer ID.
- Consent: library doesn't check consent; only needs a contact. Contact usually exists only with tracking consent →
  untracked orders don't start processes. Not changed. Docs: site code may create/pick a contact itself (no email
  lookup; new contact per order = duplicates + needs legal basis); transactional emails → order status notifications.
- New project `src/Kentico.Xperience.Labs.AutomationExtensions.Commerce` (package split = pending decision 3; don't
  restructure existing projects). May reference Admin project.

## Platform facts (31.9.1, checked via ILSpy)

- `AutomationTriggerDispatch(BaseInfo, …)` throws `ArgumentNullException` for null contact and `ArgumentException` for
  non-`om.contact` objects. Helper must check null itself.
- `OrderInfo` has no currency column (`OrderGrandTotal`, `OrderTotalPrice`, `OrderCustomerID`, `OrderCreatedWhen`, …).
  `OrderItemInfo` has `OrderItemQuantity` (decimal), no product reference.
- `ContactManagementContext.GetCurrentContact(createAnonymous: false)` returns `null` when cookie level < Visitor,
  online marketing off, crawler/bot, or no existing contact.

## Identity

- Name `OrderCompleted`; identifier `Labs.AutomationExtensions.OrderCompleted`; data identifier
  `Labs.AutomationExtensions.OrderCompletedTriggerData`. Never change.
- Icon `Icons.ShoppingCart` (verify exists).
- Display name "Order completed". Description: starts when a customer completes an order; requires checkout code that
  fires it with the visitor's contact.

## Component catalog extension (Admin project, minimal)

Admin can't reference Commerce. Add DI registration of extra descriptors:
`services.AddAutomationComponent(AutomationComponentDescriptor)` (dedupe by name). Startup status logger and step
registry use `AutomationComponentCatalog.All` + registered extras. Enable/disable unchanged
(`AutomationExtensions:Components:OrderCompleted`).

## Trigger

- `OrderCompletedTrigger : AutomationTrigger<OrderCompletedTriggerData, OrderCompletedTriggerProperties>`.
- Properties:
  - `MinimumTotal` (decimal, `DecimalNumberInputComponent`, ≥ 0, default 0 = any).
  - `Currency` (string, `TextInputComponent`, max 10, empty = any; trimmed, ordinal ignore case).
  - `FirstOrderOnly` (bool, checkbox, default false).
- Data: `OrderId` (int), `CustomerId` (int), `Total` (decimal = `OrderGrandTotal`), `Currency` (string, empty if
  unknown), `ItemCount` (int = sum of item quantities, rounded up), `IsFirstOrder` (bool).
- `Evaluate`: enabled **and** `Total >= MinimumTotal` **and** (currency empty or equal) **and** (!FirstOrderOnly or
  `IsFirstOrder`). Static `Matches` for tests.

## First order: computed at fire time

`IsFirstOrder` stored in trigger data, computed once in the helper (`OrderInfo` exists for same `OrderCustomerID` with
`OrderID < order.OrderID` → false). Reasons: one query per order instead of one per process in `Evaluate`; snapshot at
checkout is correct (queued `Evaluate` may run after the customer's next order); value usable by #11 *Started by trigger*
and later steps.

## Currency

No currency in XbyK orders. Caller passes currency code (e.g. ISO `USD`), optional. Property is optional free text: a
dropdown needs a source list (no currency object in XbyK) and most sites have one currency.

## Firing helper

`IOrderCompletedTriggerDispatcher.Fire(OrderInfo order, ContactInfo? contact, string? currency, CancellationToken)` →
`OrderCompletedFireResult` (`Fired`, `Disabled`, `NoContact`, `Failed`).

1. Disabled → Debug log, return `Disabled` (no queries).
2. Contact null → Debug log, return `NoContact`.
3. Load item quantity sum + first-order flag (`IOrderCompletedDataSource`, internal, DB queries; fakeable).
4. `FireTrigger<OrderCompletedTrigger>(new AutomationTriggerDispatch(contact, data))`.
5. Non-cancellation exception → Error log (order ID, contact ID, exception type), return `Failed` – never break checkout.

Registration: `services.AddAutomationExtensionsCommerce()` (after `AddAutomationExtensions`). Log category via
`LogCategories.For(descriptor)`. Log events: `CommerceLogEvents` 41110 `OrderCompletedFired` (Debug),
41111 `OrderCompletedNotFired` (Debug), 41112 `OrderCompletedFailed` (Error).

## Localization

Own `OrderCompletedResources.resx` in Commerce project, registered `Server` + `Builder` like Admin.
Keys `labs.automationextensions.ordercompleted.*`.

## DancingGoat

- Reference Commerce project; `AddAutomationExtensionsCommerce()`; enable `OrderCompleted` in
  `appsettings.Development.json`.
- `ConfirmOrder`: after order creation, `ContactManagementContext.GetCurrentContact(false)` → `Fire(order, contact,
  PriceFormatter.CurrencyCode)`. Reuse loaded `OrderInfo`.

## Tests (`tests/...Commerce.Tests`, new project mirroring Admin.Tests per assembly)

- `Matches` (min total, currency, first order).
- Helper: disabled, null contact, fired data, dispatcher exception → `Failed`, cancellation rethrown.
- Query shape of first-order and item queries.
- Registration/catalog/identifier prefix, localization keys, log category/event lengths + no ID clash with Admin.
- DI extension: descriptor + services registered; Admin: extras in step registry rows + status logger.

## Documentation

Usage Guide: install/registration, component table row, section (firing code sample, contact rule, data, properties,
currency/first order notes), dependency row, log events.

## Work boundaries

No commits. Don't modify DB/CI repository data. Report what was/wasn't verified in the running app.

## As built (2026-10-13)

- `IAutomationTriggerDispatcher` has an internal member (can't be faked) → internal ctor takes a fire delegate.
- Info object field access needs the Xperience container → internal `Fire(CompletedOrder …)` overload for tests; contact
  ID read only when logging is enabled.
- Tests: Admin 1300, Commerce 51. App: startup log only; end-to-end firing not done.
