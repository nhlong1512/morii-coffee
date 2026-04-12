# Feature Specification: Cart, Order, and Payment

**Feature Branch**: `007-cart-order-payment-stripe`
**Created**: 2026-04-11
**Status**: Draft
**Input**: User description: "Cart and payment feature, include, order, payment with stripe"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Add Items to Cart (Priority: P1)

A logged-in customer browses the product catalog, selects a product variant (e.g., "Large Iced Latte"), and adds it to their personal shopping cart. They can adjust the quantity, remove items, and see a live running total before committing to an order.

**Why this priority**: Cart is the foundation of the purchase flow. Without a working cart, no other commerce feature is possible.

**Independent Test**: A customer can add a product variant to their cart, update its quantity, remove it, and always see the correct subtotal. The cart persists across sessions (user logs out and back in, cart is still there).

**Acceptance Scenarios**:

1. **Given** a logged-in customer viewing a product page, **When** they add a variant to the cart with quantity 2, **Then** the cart contains that item with quantity 2 and the subtotal reflects `variant.TotalPrice × 2`.
2. **Given** an item already in the cart, **When** the customer updates its quantity to 0 or clicks Remove, **Then** the item is removed from the cart and the subtotal is recalculated.
3. **Given** a cart with items, **When** the customer adds the same variant again, **Then** the quantity increments rather than creating a duplicate line item.
4. **Given** an out-of-stock variant (StockQuantity = 0), **When** the customer attempts to add it, **Then** the request is rejected with a clear "out of stock" message.
5. **Given** a guest user (not logged in), **When** they attempt to access or modify the cart, **Then** they receive a 401 Unauthorized response.

---

### User Story 2 — View Cart Summary (Priority: P1)

A logged-in customer can view the full contents of their cart at any time: each item's name, selected variant, unit price, quantity, line total, and the cart grand total.

**Why this priority**: Cart visibility is required before any checkout can begin.

**Independent Test**: Call the "get cart" endpoint; verify response includes all line items with correct prices and a correct grand total.

**Acceptance Scenarios**:

1. **Given** a cart with multiple items, **When** the customer fetches the cart, **Then** the response includes each item (product name, variant name, unit price, quantity, line total) and a correct grand total summing all line totals.
2. **Given** an empty cart, **When** the customer fetches the cart, **Then** the response returns an empty items list with a grand total of 0.
3. **Given** a product whose base price was updated after it was added to the cart, **When** the customer views the cart, **Then** the cart reflects the price at the time of adding (cart price is snapshotted at add-time).

---

### User Story 3 — Place an Order (Checkout) (Priority: P2)

A logged-in customer with a non-empty cart confirms their delivery details and places an order. The order is created from the current cart contents, the cart is cleared, and the customer is directed to payment.

**Why this priority**: Order creation converts cart intent into a committed purchase record.

**Independent Test**: Submit a checkout request with valid delivery details; verify an order record is created with the correct line items, total, and status "Pending Payment".

**Acceptance Scenarios**:

1. **Given** a cart with valid items and all required delivery fields provided, **When** the customer places an order, **Then** a new order is created with status "Pending Payment", containing all cart items, and the cart is emptied.
2. **Given** a cart where one variant has since become out-of-stock, **When** the customer attempts to place the order, **Then** the order is rejected and the customer is notified which item is unavailable.
3. **Given** an empty cart, **When** the customer attempts to place an order, **Then** the request is rejected with "Cart is empty".
4. **Given** a successfully created order, **When** the customer submits the same checkout request again (double-submit), **Then** duplicate orders are prevented (idempotency check on active pending orders for the same customer).

---

### User Story 4 — Pay for an Order via Stripe (Priority: P2)

A customer with a "Pending Payment" order is redirected to a secure Stripe Checkout session. Upon successful payment, the order status updates to "Paid" and the customer receives an order confirmation.

**Why this priority**: Payment converts a "pending" order into revenue.

**Independent Test**: Trigger a checkout session for a pending order → simulate Stripe webhook `checkout.session.completed` → verify order status changes to "Paid" and confirmation email is sent.

**Acceptance Scenarios**:

1. **Given** an order with status "Pending Payment", **When** the customer initiates payment, **Then** a unique, time-limited payment session URL is returned and the customer is redirected to the secure payment page.
2. **Given** a completed payment (webhook `checkout.session.completed`), **When** the webhook arrives with a valid signature, **Then** the matching order status changes to "Paid" and a confirmation email is dispatched to the customer.
3. **Given** a failed or cancelled payment (webhook `payment_intent.payment_failed`), **When** the webhook arrives, **Then** the order status changes to "Payment Failed" and the customer is notified.
4. **Given** a payment webhook with an invalid or missing signature, **When** the system receives it, **Then** the request is rejected (HTTP 400) and the order is not modified.
5. **Given** an already "Paid" order, **When** a duplicate webhook event arrives, **Then** the system processes it idempotently — no duplicate state change, no duplicate email.

---

### User Story 5 — View Order History (Priority: P3)

A logged-in customer can view a paginated list of their past and current orders, ordered from most recent to oldest. Each entry shows the order number, date, total, status, and a detail view with all line items.

**Why this priority**: Customers need post-purchase visibility for trust and support resolution.

**Independent Test**: After creating and paying for an order, call the order history endpoint; verify the order appears with correct status and line items.

**Acceptance Scenarios**:

1. **Given** a customer who has placed orders, **When** they fetch their order history, **Then** they see a paginated list of their own orders only, sorted newest first.
2. **Given** an order ID belonging to another customer, **When** the current customer fetches that order, **Then** they receive a 403 Forbidden response.
3. **Given** a specific order ID, **When** the customer fetches its detail, **Then** the response includes order date, status, delivery address, all line items (product name, variant name, quantity, unit price), and the total.

---

### User Story 6 — Admin / Staff Order Management (Priority: P3)

An admin or staff member can view all orders across all customers, filter by status and date range, and manually progress an order through its fulfilment lifecycle (e.g., mark as Preparing, Ready for Pickup, Delivered, or Cancelled).

**Why this priority**: Operations staff need visibility and control over order fulfilment workflow.

**Independent Test**: As STAFF, fetch all orders; filter by "Paid" status; update one order to "Preparing"; verify status change is persisted and customer is notified.

**Acceptance Scenarios**:

1. **Given** an admin or staff user, **When** they fetch all orders, **Then** they see orders from all customers with full detail, and can filter by status and date range.
2. **Given** a "Paid" order, **When** STAFF or ADMIN transitions it to "Preparing", **Then** the order status is updated and a status-change notification is sent to the customer.
3. **Given** a CUSTOMER role user, **When** they attempt to access the admin order list endpoint, **Then** they receive a 403 Forbidden response.
4. **Given** an order in a terminal state ("Delivered" or "Cancelled"), **When** staff attempts to change its status, **Then** the transition is rejected with a descriptive error message.

---

### Edge Cases

- What happens when a product variant is deactivated or deleted while it exists in an active cart? The cart item should be flagged as "unavailable" when the cart is viewed, and it should be rejected at checkout.
- How does the system handle stock — is stock reserved at cart-add time or order-placement time? (Assumed: validated at order-placement time only.)
- What happens if the Stripe webhook arrives before the database record for the pending order is committed? Retry logic on the webhook endpoint should handle eventual consistency.
- What is the cart expiry policy? Carts inactive for more than 7 days can be purged.
- What happens if a customer places an order but never pays? The pending order expires after 30 minutes and the status transitions to "Expired".
- How are refunds handled? Admin-initiated cancellation only; Stripe refund API calls are out of scope for this feature.

---

## Requirements *(mandatory)*

### Functional Requirements

**Cart Management**:

- **FR-001**: System MUST associate a single cart per authenticated customer account.
- **FR-002**: System MUST allow customers to add a product variant to their cart with a specified quantity (minimum 1).
- **FR-003**: System MUST prevent adding a variant that is unavailable (`IsAvailable = false`) or out-of-stock (`StockQuantity = 0`).
- **FR-004**: System MUST merge duplicate cart items — adding an already-present variant increases its quantity rather than creating a new line item.
- **FR-005**: System MUST allow customers to update the quantity of an existing cart item (minimum 1; setting to 0 removes the item).
- **FR-006**: System MUST allow customers to remove a specific item from the cart.
- **FR-007**: System MUST allow customers to clear (empty) the entire cart.
- **FR-008**: System MUST snapshot the unit price of each item at the time it is added to the cart; subsequent product price changes do not retroactively alter the cart.
- **FR-009**: System MUST return a cart summary including all line items, unit prices, quantities, line totals, and a grand total.
- **FR-010**: System MUST persist the cart across authenticated sessions.

**Order Placement**:

- **FR-011**: System MUST allow a customer to create an order from their current non-empty cart, providing a delivery address and optional order notes.
- **FR-012**: System MUST validate that all items in the cart remain available and in-stock at order-placement time before committing the order.
- **FR-013**: System MUST assign each order a human-readable, unique order number (e.g., `ORD-20260411-0001`).
- **FR-014**: System MUST set newly placed orders to "Pending Payment" status.
- **FR-015**: System MUST clear the customer's cart after successful order creation.
- **FR-016**: System MUST record each order line item as an immutable snapshot of the product name, variant name, unit price, and quantity at order-placement time.

**Payment**:

- **FR-017**: System MUST generate a secure, time-limited payment session URL for an order in "Pending Payment" status.
- **FR-018**: System MUST accept inbound payment lifecycle webhooks and verify their authenticity using a shared signing secret before processing.
- **FR-019**: System MUST update the order status to "Paid" upon receiving a verified successful payment completion webhook.
- **FR-020**: System MUST update the order status to "Payment Failed" upon receiving a verified payment failure webhook.
- **FR-021**: System MUST process payment webhooks idempotently — duplicate webhook events for the same payment MUST NOT trigger duplicate state changes or duplicate notifications.
- **FR-022**: System MUST send an order confirmation notification to the customer upon successful payment.
- **FR-023**: System MUST expire "Pending Payment" orders that have not been paid within 30 minutes, transitioning them to an "Expired" status.

**Order History & Management**:

- **FR-024**: System MUST allow customers to view a paginated list of their own orders, sorted newest first.
- **FR-025**: System MUST allow customers to view the full detail of any of their own orders.
- **FR-026**: System MUST prevent customers from accessing orders belonging to other customers (return 403).
- **FR-027**: System MUST allow ADMIN and STAFF roles to view all orders with filtering by status and date range.
- **FR-028**: System MUST allow ADMIN and STAFF to transition an order's fulfilment status (Paid → Preparing → ReadyForPickup → Delivered; any non-terminal → Cancelled).
- **FR-029**: System MUST reject status transitions that violate the defined state machine and return a descriptive error.
- **FR-030**: System MUST notify the customer by email when ADMIN/STAFF updates their order status.

### Key Entities

- **Cart**: One cart per customer. Contains zero or more CartItems. Has a computed grand total. An inactive cart expires after 7 days.
- **CartItem**: A line in the customer's cart. References a `ProductVariant` by ID. Stores the snapshotted unit price and the desired quantity. Belongs to a Cart.
- **Order**: A confirmed purchase. Holds an auto-generated order number, reference to the customer, delivery address, optional notes, order status, total amount (snapshotted from cart), and a collection of OrderItems. Immutable after creation (only status changes).
- **OrderItem**: An immutable snapshot of a purchased item. Stores product name, variant name, unit price, and quantity. Not a live reference to ProductVariant — product changes do not mutate past order records.
- **Payment**: One payment record per order. Stores the external payment session ID, payment event ID (for idempotency), payment status, amount, currency, and timestamps. Linked 1:1 to an Order.
- **OrderStatus** (enum): `PendingPayment`, `Expired`, `Paid`, `PaymentFailed`, `Preparing`, `ReadyForPickup`, `Delivered`, `Cancelled`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A customer can add items to cart, complete checkout, and initiate payment in under 3 minutes from an empty cart.
- **SC-002**: Cart price snapshots are correct 100% of the time — changing a product's price after adding it to the cart never alters the stored cart item price (verified by automated tests).
- **SC-003**: Payment status webhooks are processed and the order status is updated within 5 seconds of receipt under normal load.
- **SC-004**: Duplicate webhook events for the same payment trigger zero duplicate state changes and zero duplicate notification emails (verified by idempotency tests).
- **SC-005**: Cross-customer order access is blocked in 100% of test cases — a customer fetching another customer's order ID always receives 403 Forbidden.
- **SC-006**: All invalid order status transitions are rejected with a descriptive error message, verified across all defined invalid transition pairs in the test suite.
- **SC-007**: Order confirmation email reaches the customer within 30 seconds of a verified successful payment webhook.
- **SC-008**: No out-of-stock or unavailable variant can be added to the cart or included in a placed order — verified by 0 bypass cases in the test suite.

---

## Assumptions

- **Payment Provider**: Stripe Checkout is the designated payment provider. The "payment session URL" in FR-017 is a Stripe Checkout Session URL. Webhook events map to Stripe's `checkout.session.completed` and `payment_intent.payment_failed` events. Stripe's webhook signing secret is used for authenticity verification (FR-018).
- **Currency**: All prices are in VND (Vietnamese Dong). Stripe is configured with VND as the currency.
- **Stock Management**: Stock is validated at order-placement time only (not reserved at cart-add). This is the simpler check-and-decrement strategy; overselling edge cases can be addressed in a follow-up feature.
- **Delivery Model**: A delivery address is required at checkout. Physical delivery logistics (routing, rider assignment) are out of scope.
- **Guest Checkout**: Not supported. All cart and order operations require an authenticated customer session.
- **Refunds**: Stripe-initiated refunds and partial refunds are out of scope. Admin-initiated order cancellation only.
- **Notifications**: All notifications (order confirmation, status updates) are email-based via the existing `IEmailService` abstraction. Push or SMS notifications are out of scope.
- **Cart Expiry Job**: The 7-day cart expiry and 30-minute pending order expiry policies need to be stored in the domain, but the background job that enforces them is a separate concern and may be deferred.
- **Idempotency Key**: Stripe webhook idempotency is enforced by storing the Stripe `event.id` on the Payment record and rejecting processing if it already exists.
- **Order Number Format**: Human-readable order numbers follow the pattern `ORD-YYYYMMDD-NNNN` where NNNN is a daily sequential counter. Exact generation strategy is an implementation detail.
