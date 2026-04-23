# Feature Specification: Cart, Checkout & Payment

**Feature Branch**: `009-cart-payment`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "Please analyze the entire codebase of the MoriiCoffee repo. Create for me a spec and plan so I can implement features related to ordering products for Morii Coffee — cart, payment."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Add Products to Cart (Priority: P1)

A customer browses the menu, selects a product variant (e.g., Latte Classic — Nhỏ), and adds it to their cart. They can adjust quantities or remove items before proceeding. The cart works for both guest and authenticated users.

**Why this priority**: The cart is the entry point for all purchasing. Nothing downstream (checkout, payment) can function without it.

**Independent Test**: Can be fully tested by adding a variant to cart, changing its quantity, and removing it — delivers a persistent browsable cart independently of checkout or payment.

**Acceptance Scenarios**:

1. **Given** a product variant is available, **When** a user adds it with quantity 1, **Then** the cart contains that item with the correct name, variant, unit price, and subtotal.
2. **Given** the same variant already exists in the cart, **When** the user adds it again, **Then** the quantity increments by 1 rather than creating a duplicate line item.
3. **Given** the cart has an item, **When** the user sets its quantity to 0 or removes it, **Then** the item is removed and the cart total updates accordingly.
4. **Given** a guest user has items in their cart, **When** they log in, **Then** their guest cart items are merged into their authenticated cart.
5. **Given** a product variant has been deactivated since being added, **When** the cart is fetched, **Then** that item is flagged as unavailable and the user is notified.

---

### User Story 2 — Checkout and Place an Order (Priority: P1)

A logged-in customer reviews their cart, enters a delivery address, and places an order. The system creates an order record, clears the cart, and sends a confirmation email.

**Why this priority**: Converting a cart into a confirmed order is the core business transaction.

**Independent Test**: Can be tested by placing a COD order — order is created with a unique number, cart is cleared, confirmation email is received — independently of online payment.

**Acceptance Scenarios**:

1. **Given** a logged-in customer with a non-empty cart, **When** they submit a valid delivery address and choose COD, **Then** an order is created with a unique order number, status "Confirmed", and the cart is cleared.
2. **Given** a logged-in customer, **When** they attempt checkout with an empty cart, **Then** the system returns an error and no order is created.
3. **Given** a cart item's variant has become unavailable, **When** the customer attempts checkout, **Then** they are informed of the unavailable item before the order is created.
4. **Given** an order is successfully placed, **Then** the customer receives an order confirmation email within 2 minutes.

---

### User Story 3 — Pay Online for an Order (Priority: P1)

A logged-in customer selects an online payment method, is redirected to the payment gateway, and completes payment. The order status updates automatically upon return.

**Why this priority**: Online payment is the primary revenue collection mechanism for non-COD orders.

**Independent Test**: Can be fully tested end-to-end in sandbox mode of the payment gateway — verifies order status transitions on successful and failed payments independently of other features.

**Acceptance Scenarios**:

1. **Given** a Pending order with online payment selected, **When** the customer completes payment on the gateway, **Then** the order status transitions to "Confirmed" and the payment is recorded as successful.
2. **Given** the customer abandons the payment flow, **When** 15 minutes elapse without payment, **Then** the order is automatically cancelled.
3. **Given** the payment gateway reports failure, **When** the callback is received, **Then** the order remains in "AwaitingPayment" and the customer can retry payment.
4. **Given** the same payment webhook is delivered twice, **When** the second delivery arrives, **Then** the system ignores the duplicate — no double processing occurs.

---

### User Story 4 — View Order History and Cancel (Priority: P2)

A customer views their past and active orders, tracks their current status, and cancels an order that has not yet been confirmed.

**Why this priority**: Order visibility builds trust. Cancellation prevents unnecessary fulfilment of abandoned orders.

**Independent Test**: Can be tested by placing an order and verifying it appears in order history with correct detail and a cancel action.

**Acceptance Scenarios**:

1. **Given** a logged-in customer, **When** they open "My Orders", **Then** they see a paginated list sorted most-recent-first, showing order number, date, status, and total.
2. **Given** the customer selects an order, **Then** they see full detail: all items, variant names, prices, delivery address, payment method, and current status.
3. **Given** an order is in "Pending" or "AwaitingPayment" status, **When** the customer cancels it, **Then** the order status becomes "Cancelled".
4. **Given** an order is in "Confirmed" or later, **When** the customer attempts to cancel, **Then** the system rejects the request with an explanation.

---

### User Story 5 — Admin and Staff Order Management (Priority: P2)

Admin and Staff users view all orders, filter by status or date, and advance orders through the fulfilment pipeline.

**Why this priority**: Without operational management, the order system has no fulfilment workflow.

**Independent Test**: Can be tested independently by having an Admin view placed orders and update a status — delivers operational visibility without customer-facing dependencies.

**Acceptance Scenarios**:

1. **Given** an Admin or Staff user, **When** they access the orders dashboard, **Then** they see all orders filterable by status, date range, and searchable by order number or customer email.
2. **Given** an Admin updates an order status (e.g., Confirmed → Processing), **Then** the order reflects the new status and the customer receives an email notification.
3. **Given** a paid order is cancelled by Admin, **Then** the order is flagged "Refund Required" and Admin can manually mark it "Refunded".

---

### Edge Cases

- What if a product's price changes after it was added to cart? The cart preserves the price at time of addition; the order snapshot captures prices at checkout time.
- What if two users simultaneously check out the only available unit? Availability is validated at checkout time; the second request fails with a clear error.
- What if the payment webhook arrives before the order reaches "AwaitingPayment"? Webhooks must be idempotent — a short delay or retry mechanism resolves the race condition.
- What if the auto-cancel Hangfire job fails? Jobs retry 3 times with exponential backoff; unresolvable jobs are dead-lettered and surfaced for investigation.
- What if a guest registers a new account after building a cart? The guest cart merges into the new account on first authenticated request.
- What if checkout is called with 0 items? The endpoint returns a 400 error with a clear message before any order is created.

---

## Requirements *(mandatory)*

### Functional Requirements

**Cart Management:**

- **FR-001**: System MUST allow both guest users (identified by an anonymous session) and authenticated users to maintain a persistent cart.
- **FR-002**: System MUST allow adding a product variant to the cart with a quantity between 1 and 99.
- **FR-003**: System MUST allow updating the quantity of an existing cart item (minimum 1, maximum 99).
- **FR-004**: System MUST allow removing a single item or clearing the entire cart.
- **FR-005**: System MUST return cart totals: unit price per item, line subtotal (unit price × quantity), and grand total.
- **FR-006**: System MUST migrate guest cart items into the authenticated user's cart upon login.
- **FR-007**: System MUST preserve item prices in the cart as they were at time of addition — catalog price changes do not retroactively update cart prices.
- **FR-008**: Authenticated user carts MUST persist for at least 7 days; guest carts for at least 24 hours.

**Checkout & Orders:**

- **FR-009**: System MUST require authentication to place an order; guest users cannot checkout.
- **FR-010**: Checkout MUST require a delivery address: recipient name, phone number, street address, ward, district, and city.
- **FR-011**: System MUST generate a unique, human-readable order number for every order (e.g., `MCF-20260419-0001`).
- **FR-012**: System MUST snapshot each ordered item's product name, variant name, unit price, and quantity at order creation — independent of future catalog changes.
- **FR-013**: System MUST support the following order status lifecycle:
  - `Pending` → `AwaitingPayment` → `Confirmed` → `Processing` → `Delivering` → `Delivered`
  - Terminal states: `Cancelled`, `Refunded`
- **FR-014**: Customers MUST only be permitted to cancel orders in `Pending` or `AwaitingPayment` status.
- **FR-015**: Admin and Staff MUST be able to transition an order to any valid next status.
- **FR-016**: System MUST send an email confirmation to the customer immediately upon successful order creation.
- **FR-017**: System MUST send an email notification to the customer when order status transitions to `Confirmed`, `Delivering`, or `Delivered`.

**Payment:**

- **FR-018**: System MUST support **Cash on Delivery (COD)** — selecting COD transitions the order directly from `Pending` → `Confirmed` without a payment window.
- **FR-019**: System MUST support at least one online payment gateway. [NEEDS CLARIFICATION: Which online payment gateway should be implemented for Phase 1 — VNPay only, MoMo only, or both? This decision directly impacts integration scope and delivery timeline.]
- **FR-020**: System MUST record every payment attempt with: method, amount, status (`Pending` / `Success` / `Failed` / `Cancelled`), and gateway transaction ID.
- **FR-021**: System MUST process gateway webhooks idempotently — duplicate webhook deliveries MUST NOT create duplicate payment records or trigger duplicate status transitions.
- **FR-022**: System MUST verify gateway callback signatures before processing — unsigned or tampered payloads MUST be rejected.
- **FR-023**: System MUST automatically cancel an online-payment order that remains unpaid after **15 minutes** via a background job.
- **FR-024**: System MUST provide a payment status endpoint so the frontend can confirm final payment state after returning from the gateway redirect.

---

### Key Entities

- **Cart**: Temporary session-scoped collection. Not persisted in the database. Owned by either a user ID (authenticated) or an anonymous session ID (guest). Contains items with their quantities and prices at time of addition. Subject to TTL expiration.

- **CartItem**: One line within a Cart. References a product variant ID, and caches the product name, variant name, and unit price for display without requiring a product lookup on every fetch.

- **Order**: The confirmed purchase record. Persisted in the database. Contains a snapshot of all ordered items, the delivery address, payment method, grand total, and status. Once created, order items are immutable.

- **OrderItem**: An immutable record of one product variant in an order. Stores product name, variant name, unit price, quantity, and line subtotal. Has no live reference to the product catalog.

- **DeliveryAddress**: Value object on Order. Contains recipient name, phone, street, ward, district, city, and an optional delivery note.

- **Payment**: A payment transaction record tied to an Order. Stores payment method, amount, status, gateway transaction ID, raw callback payload (for audit/dispute), and timestamps.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A customer can add items to cart, complete checkout, and have a confirmed order within 3 minutes on a standard mobile connection.
- **SC-002**: The system sustains 500 concurrent users browsing, managing carts, and checking out without page response times exceeding 2 seconds.
- **SC-003**: Unpaid online orders are cancelled within 16 minutes of creation (15-minute window plus at most 1 minute of job execution lag).
- **SC-004**: Order status is updated within 5 seconds of receiving a gateway callback — before the customer is returned to the confirmation screen.
- **SC-005**: 99% of checkout attempts on a non-empty cart result in a successfully persisted order with a confirmation number.
- **SC-006**: Order confirmation emails are delivered within 2 minutes of order creation.
- **SC-007**: Admin and Staff can search, filter, and update orders with response times under 2 seconds.
- **SC-008**: Zero duplicate payment records are created from duplicate gateway webhook deliveries across any volume of traffic.

---

## Scope

### In Scope

- Shopping cart (add / update / remove / clear) for guest and authenticated users
- Guest-to-authenticated cart migration on login
- Order creation (checkout) with delivery address capture
- COD payment flow
- Online payment gateway integration with redirect flow and webhook handling
- Automatic cancellation of unpaid online orders via background job
- Customer order history and order detail view
- Customer-initiated order cancellation (pre-confirmation only)
- Admin / Staff order dashboard with filtering, search, and status management
- Email notifications: order confirmation and status updates
- Full payment attempt audit trail

### Out of Scope (Phase 1)

- Promotional codes, vouchers, or discounts
- In-store pickup / takeaway ordering (delivery address required for all orders)
- Loyalty rewards or points system
- Multi-branch or store selection
- Inventory tracking beyond variant availability flag
- Automated refund API calls (admin marks `Refunded` manually)
- Real-time delivery tracking on a map
- Mobile push notifications
- Invoice / receipt PDF generation
- Subscription or recurring orders

---

## API Endpoints

### Cart

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/cart` | Optional | Fetch current cart (guest via session cookie; authenticated via JWT) |
| `POST` | `/api/v1/cart/items` | Optional | Add a product variant to the cart |
| `PUT` | `/api/v1/cart/items/{variantId}` | Optional | Update quantity of a cart item |
| `DELETE` | `/api/v1/cart/items/{variantId}` | Optional | Remove a specific item |
| `DELETE` | `/api/v1/cart` | Optional | Clear the entire cart |

### Orders — Customer

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/v1/orders` | Customer | Create order from cart (checkout) |
| `GET` | `/api/v1/orders` | Customer | Get authenticated user's orders (paginated) |
| `GET` | `/api/v1/orders/{orderId}` | Customer | Get full detail of a specific order |
| `POST` | `/api/v1/orders/{orderId}/cancel` | Customer | Cancel an order in Pending or AwaitingPayment |

### Payment

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/v1/orders/{orderId}/payment` | Customer | Initiate payment; returns redirect URL for online methods |
| `GET` | `/api/v1/orders/{orderId}/payment/status` | Customer | Poll payment status for an order |
| `GET` | `/api/v1/payments/callback/vnpay` | Public | VNPay return URL (redirect after gateway) |
| `POST` | `/api/v1/payments/webhook/vnpay` | Public | VNPay IPN server-to-server notification |
| `GET` | `/api/v1/payments/callback/momo` | Public | MoMo return URL |
| `POST` | `/api/v1/payments/webhook/momo` | Public | MoMo IPN server-to-server notification |

> Webhook endpoints for unselected gateways can be omitted or stubbed in Phase 1.

### Orders — Admin / Staff

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/admin/orders` | Admin / Staff | All orders with filtering (status, date, search) and pagination |
| `GET` | `/api/v1/admin/orders/{orderId}` | Admin / Staff | Full detail of any order |
| `PUT` | `/api/v1/admin/orders/{orderId}/status` | Admin / Staff | Update order status |

---

## Technology Recommendations

> This section directly addresses the user's request to specify required technologies. It supplements the business specification above.

### Redis
- **Cart storage**: All cart data stored in Redis. Keys: `cart:{userId}` (authenticated), `cart:guest:{sessionId}` (guest). TTL: 7 days (auth), 24 hours (guest).
- **Webhook idempotency**: Processed gateway transaction IDs cached in Redis with 24-hour TTL to detect and discard duplicate webhook deliveries.
- **Integration note**: Redis infrastructure is already planned in `docs/apply-redis.md` — this feature is the primary driver for its adoption.

### Hangfire
- **Auto-cancel job**: Enqueue a delayed job at order creation (fires in 15 minutes). Job checks whether payment succeeded; if not, cancels the order. If already paid, no-op.
- **Email delivery**: Order confirmation and status-update emails dispatched as Hangfire fire-and-forget background jobs, consistent with the existing email pattern in the codebase.
- **Storage backend**: SQL Server (reuses the existing database — no new infrastructure dependency).
- **Dashboard**: Accessible at `/hangfire`, restricted to Admin role.
- **Retry policy**: 3 automatic retries with exponential backoff; dead-letter after max attempts.

### Message Queue
- **Not required for Phase 1.** Hangfire over SQL Server provides sufficient durability and retry semantics at the expected scale.
- **Phase 2 consideration**: If the API scales to multiple instances or cross-service event streaming is needed, introduce RabbitMQ or Azure Service Bus at that point.

### Payment Gateways
- **VNPay**: HTTP redirect flow. Customer redirected to VNPay hosted checkout; VNPay notifies via IPN (server-to-server) and return URL. Requires `vnp_TmnCode` and `vnp_HashSecret`. Widest bank coverage in Vietnam.
- **MoMo**: REST API with HMAC-SHA256 signatures. Same redirect + IPN model. Requires partner code, access key, and secret key. Popular for mobile wallet users.
- **Recommendation**: Implement VNPay for Phase 1 (broader reach). Add MoMo in Phase 2 if demand warrants it.

### Email (existing infrastructure)
- Continue using the existing `BrevoEmailService`. Add two new HTML email templates:
  - `order-confirmation.html` — triggered on order creation.
  - `order-status-update.html` — triggered on status transitions to `Confirmed`, `Delivering`, `Delivered`.

---

## Sub-Task Breakdown

Split into **4 sequential phases**, each independently deliverable:

| Phase | Scope | Dependency | Complexity |
|-------|-------|------------|------------|
| **1 — Cart** | Redis setup, cart CRUD endpoints, guest+auth support, cart migration on login | None | Medium |
| **2 — Orders** | Order aggregate, DB schema, checkout endpoint, delivery address, order number generation, cart clearing, confirmation email | Phase 1 | Medium-High |
| **3 — Payment** | COD flow, VNPay integration, webhook handler, Redis idempotency, auto-cancel Hangfire job, payment status polling | Phase 2 | High |
| **4 — Management** | Customer order history + cancellation, Admin/Staff order dashboard, status updates, status-change email notifications | Phases 2–3 | Low-Medium |

Each phase can be spec'd, planned, and implemented independently via `/speckit.plan` and `/speckit.tasks`.

---

## Assumptions

1. **Stock management**: Variants with `stockQuantity = -1` are unlimited. Cart and checkout validate only variant existence and `isAvailable = true` — no stock decrement.
2. **Shipping fee**: A flat-rate fee (e.g., 30,000 VND) applies to all orders in Phase 1. No address-based shipping rate calculation.
3. **Currency**: Vietnamese Dong (VND) only. All monetary amounts are integers.
4. **Guest checkout**: Guests can build a cart but must authenticate before placing an order.
5. **Single active payment per order**: Only one payment record can be in `Pending` state at a time. Initiating a new payment for the same order cancels the previous pending attempt.
6. **COD bypasses payment window**: COD orders move directly `Pending` → `Confirmed`. The 15-minute auto-cancel applies only to online-payment orders.
7. **Price snapshot at order creation**: `OrderItem` captures prices at checkout time, not cart-add time.
8. **Refunds are manual**: Admin marks a paid cancelled order as `Refunded` via the dashboard. No automated gateway refund API calls in Phase 1.
9. **Delivery only**: All orders require a delivery address. In-store pickup is out of scope.
