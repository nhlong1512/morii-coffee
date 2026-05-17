# Feature Specification: Stripe Online Payment for Cart Checkout

**Feature Branch**: `011-stripe-payment`
**Created**: 2026-05-14
**Status**: Draft
**Input**: User description: "Integrate Stripe payment so customers can pay for their orders online during checkout. Currently the project only has cart and order functionality, but no payment. Provide a complete beginner-friendly guide, then implement the integration end-to-end with thorough comments and unit tests."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pay for an order with a credit/debit card at checkout (Priority: P1)

A customer has filled their cart with coffee and food items and proceeds to checkout. They choose to pay online with a card instead of paying cash on delivery. The system takes them to a secure card-entry experience hosted by the payment provider, processes the charge, and brings them back to a confirmation page that shows their order number and a successful payment status. The order is now confirmed and the cart is empty.

**Why this priority**: This is the core capability that justifies the entire feature. Without it, no online revenue can be collected. It is the minimum viable slice — once it works, the business can accept online payments.

**Independent Test**: Place a test order from the storefront using Stripe's official test card `4242 4242 4242 4242`. After the redirect (or modal confirmation) completes successfully, verify:
1. The order appears in the customer's history with status reflecting "paid / awaiting fulfilment".
2. The cart is empty.
3. The transaction is visible in Stripe's test-mode dashboard for the same amount and currency.

**Acceptance Scenarios**:

1. **Given** a customer with a non-empty cart and a valid delivery address, **When** they choose "Pay online with card" and submit their card details, **Then** the system charges the exact cart total, creates the order in a "payment pending" state, and after the provider confirms the charge marks it "paid", clears the cart, and shows a success page within 10 seconds in the happy path.
2. **Given** a customer enters a card that is declined (e.g., insufficient funds), **When** they submit payment, **Then** the system shows a clear failure message, leaves the cart intact so they can retry with another card, and does **not** mark any order as paid.
3. **Given** a customer abandons the payment page midway (closes the tab), **When** they return to the cart later, **Then** their cart contents are still present and no duplicate or "ghost" paid order has been confirmed.

---

### User Story 2 - Reliable order status sync after payment (Priority: P1)

When a payment finally settles (or fails) at the payment provider, the order's payment status inside Morii Coffee must match — even if the customer closed their browser, lost connectivity, or the payment took several seconds to clear (some cards require 3-D Secure / bank confirmation). Staff seeing the order in the admin dashboard must be able to trust the payment state shown.

**Why this priority**: Without this, the business risks shipping unpaid orders or refusing paid ones because the UI says "pending". The whole purpose of accepting online payment depends on this fidelity. It is co-equal in priority with Story 1.

**Independent Test**: Trigger a delayed-success test payment via Stripe's test tooling (a 3-D Secure required card). Close the browser before the confirmation page loads. Within 1 minute, check the order in the admin dashboard and confirm it is marked as paid without any human action.

**Acceptance Scenarios**:

1. **Given** a payment that succeeds at the provider, **When** the customer's browser never reaches the success page (closed tab, offline, etc.), **Then** the order is still automatically updated to "paid" status using out-of-band notifications from the provider, within 60 seconds of the payment settling.
2. **Given** a payment that fails at the provider after initial acceptance (e.g., bank reverses it), **When** the failure is signalled by the provider, **Then** the order's payment status moves to "failed" and is **not** confirmed for fulfilment.
3. **Given** the system receives the same payment notification twice (provider retries), **When** it processes both, **Then** the order state is updated exactly once with no duplicate charges, refunds, or status changes.

---

### User Story 3 - Admin refunds a customer (Priority: P2)

An admin needs to fully refund a paid order that the customer cancelled or that the shop cannot fulfil. From the admin order detail screen, the admin clicks "Refund" on a paid order, confirms the amount, and the system reverses the charge at the payment provider while marking the order as refunded.

**Why this priority**: Refunds are essential for customer service and Vietnamese consumer-protection norms, but the business can launch and operate for a short window without an in-app refund button (admins could refund manually in the Stripe dashboard as a fallback). So this is the next-most-valuable slice but not blocking the MVP.

**Independent Test**: With a previously paid test order, an admin issues a refund through the admin UI. Verify the refund appears in the Stripe dashboard with matching amount and reason, the order status in Morii Coffee changes to refunded, and the customer can see the updated status in their order history.

**Acceptance Scenarios**:

1. **Given** a paid order in any non-delivered status, **When** an admin issues a refund for the full amount, **Then** the refund is processed at the provider, the order's payment status becomes "refunded", and an audit record of the refund is stored against the order.
2. **Given** an order is partially fulfilled and the admin wants to refund only part of the total, **When** the admin issues a partial refund with a specific amount, **Then** only that amount is refunded and the order's payment status reflects "partially refunded".
3. **Given** a non-admin attempts to call the refund endpoint, **When** the request is made, **Then** it is rejected with an authorisation error and no refund is initiated.

---

### User Story 4 - Customer keeps the existing cash-on-delivery option (Priority: P2)

Customers who don't want to pay online (no card, prefer cash) can still place an order using cash on delivery exactly as they could before this feature. The new online payment option does **not** replace COD — it is an additional choice presented at checkout.

**Why this priority**: A breaking change to the existing COD flow would block existing customers. This story ensures backward compatibility. It is mostly a non-regression guarantee.

**Independent Test**: Place a test order choosing COD. Verify the order is created exactly as it was before the feature (no payment-provider API calls made, order goes directly into the existing pending-confirmation state, cart is cleared).

**Acceptance Scenarios**:

1. **Given** a customer selects COD at checkout, **When** they place the order, **Then** the order is created in the existing pending state with no interaction with the online payment provider, and the cart is cleared.
2. **Given** a customer flipped between card and COD payment options multiple times before submitting, **When** they finally submit with COD, **Then** the system uses only the final selection and does not pre-authorise a card.

---

### Edge Cases

- **Provider downtime**: If the payment provider is unreachable when the customer clicks "Pay", the system shows a clear retry message and does not create a confirmed/paid order. The customer's cart is preserved.
- **Currency**: All charges are in Vietnamese đồng (VND); the displayed cart total and the charged amount must match exactly (no rounding mismatches).
- **Replayed webhooks**: The provider may resend the same notification multiple times if the system is slow to acknowledge. Each notification must be idempotent.
- **Forged / spoofed webhooks**: An attacker could attempt to send fake "payment succeeded" notifications. The system must verify the cryptographic signature on every incoming notification and reject unsigned or invalid ones.
- **Race condition between success redirect and webhook**: The customer's browser may arrive at the success page before the out-of-band notification arrives, or vice versa. Both paths must converge on the same final order state without double-updates.
- **Payment for already-cancelled order**: If a payment notification arrives for an order whose status has already moved past "pending payment" (e.g., cancelled by customer in another tab), the system logs the inconsistency for admin review and does not silently mark a cancelled order as paid.
- **Customer pays from outside the app**: The system rejects payment intents that do not match a known order or that have a different amount/currency from the order they reference.
- **Refund larger than charge**: The system rejects refund requests larger than the remaining unrefunded amount.
- **Test vs. live mode confusion**: It must be impossible to charge a real card in development or to use a test card in production. Environment configuration must make the active mode unambiguous to operators.

## Requirements *(mandatory)*

### Functional Requirements

#### Payment initiation
- **FR-001**: Customers MUST be offered an "online card payment" option alongside the existing cash-on-delivery option at checkout.
- **FR-002**: When a customer selects online card payment, the system MUST create a payment session at the payment provider whose amount and currency exactly match the cart total being checked out.
- **FR-003**: The system MUST associate every payment session with the order it pays for, so that incoming notifications can be matched to the correct order.
- **FR-004**: Card details (PAN, CVV, expiry) MUST NEVER be received, stored, logged, or transmitted by Morii Coffee's own servers — they are only ever handled by the payment provider's hosted experience.
- **FR-005**: The system MUST treat the order as **not** confirmed for fulfilment until the payment is confirmed successful by the provider, regardless of what the customer's browser displays.

#### Payment confirmation (webhooks / out-of-band)
- **FR-006**: The system MUST expose an endpoint that receives out-of-band payment notifications from the provider.
- **FR-007**: Every incoming notification MUST be cryptographically verified using a shared signing secret; notifications failing verification MUST be rejected and logged.
- **FR-008**: Notification handling MUST be idempotent: receiving the same notification more than once MUST NOT cause duplicate state changes, double charges, or duplicate refunds.
- **FR-009**: On a successful payment notification, the system MUST mark the corresponding order's payment as paid and persist the provider's transaction identifier against the order.
- **FR-010**: On a failed payment notification, the system MUST mark the corresponding order's payment as failed and leave the order in a state where the customer (or admin) can retry payment or cancel.
- **FR-011**: Notifications that cannot be matched to a known order MUST be logged for administrator investigation but MUST NOT crash the endpoint or block subsequent notifications.

#### Order lifecycle
- **FR-012**: A new payment status concept MUST exist on each order, separate from the existing order-fulfilment status, with at minimum the values: not required (for COD), pending, paid, failed, refunded, partially refunded.
- **FR-013**: Order-fulfilment status transitions that require payment (e.g., admin confirming the order) MUST be blocked when the payment status is "pending" or "failed" for online-paid orders.
- **FR-014**: For cash-on-delivery orders, the payment status MUST be "not required" and the existing fulfilment workflow MUST be unchanged.

#### Refunds
- **FR-015**: Administrators MUST be able to issue a full refund for any paid order via the admin interface.
- **FR-016**: Administrators MUST be able to issue a partial refund for any paid order, with the refund amount validated against the unrefunded balance.
- **FR-017**: Refund actions MUST be restricted to authenticated administrators; customers MUST NOT be able to issue refunds on their own.
- **FR-018**: Every refund MUST be recorded against the order with amount, timestamp, the administrator who performed it, and an optional reason note.

#### Observability and operations
- **FR-019**: Every payment attempt, success, failure, and refund MUST be logged with enough context (order id, amount, provider reference) for an operator to trace it end-to-end in the application logs.
- **FR-020**: The configuration mechanism MUST clearly distinguish between provider test mode and live mode, and a deployment MUST be unable to silently mix the two (e.g., using a live secret against a test publishable key, or vice versa).
- **FR-021**: Provider secrets (API key, webhook signing secret) MUST be supplied via environment configuration and MUST NOT be checked into source control.

#### Documentation
- **FR-022**: A beginner-oriented setup-and-integration guide MUST be produced explaining: what the payment provider is, how to register an account, how to obtain test and live credentials, how to configure the local development environment, how to test payments using sandbox cards, how the moving parts fit together, and how to deploy to production safely.
- **FR-023**: A summary document MUST be produced listing every file added or changed by the feature, the business rule each one enforces, and the verification steps an engineer should run after pulling the branch.

### Key Entities *(include if feature involves data)*

- **Order** *(existing, extended)*: Gains a payment status concept and a foreign reference to the payment provider's transaction id for paid online orders. Continues to be the aggregate root for fulfilment lifecycle.
- **Payment Transaction** *(new)*: Represents a single attempted, completed, failed, or refunded payment for an order. Holds the provider's session/intent identifier, charge identifier, amount, currency, status, and timestamps. One order may have multiple payment transactions over its lifetime (e.g., a failed attempt followed by a successful retry).
- **Refund Record** *(new)*: Captures a refund against a paid transaction — amount, timestamp, admin user id, optional reason, provider refund identifier. Multiple refund records per transaction are supported (multiple partial refunds).
- **Webhook Event Log** *(new)*: Audit trail of every incoming notification from the provider — provider event id, type, signature verification result, processing outcome. Used for replay protection (idempotency) and for diagnosing payment incidents.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time customer can complete a card payment from "view cart" to "payment success page" in under 90 seconds on a standard broadband connection, including the time to fill the card form.
- **SC-002**: 99% of successful payments are reflected as "paid" in the order detail within 60 seconds of payment settlement at the provider, even when the customer closes the browser immediately after authorising the charge.
- **SC-003**: 0 cases of an order being marked "paid" without a verified payment notification from the provider (i.e., no spoofed or replayed notifications cause state changes), measured continuously in staging tests and reviewed weekly in production logs.
- **SC-004**: 0 cases of duplicate charges, duplicate refunds, or out-of-sequence status updates when the same provider notification is delivered more than once, validated by automated replay tests in the test suite.
- **SC-005**: 100% of card data (PAN, CVV, expiry) is handled exclusively by the provider's hosted interface; no card data ever appears in Morii Coffee's logs, database, or application code, verified by code review and log scanning.
- **SC-006**: An administrator can issue a full or partial refund and see the order's payment status reflect the change in under 30 seconds, with the refund visible in the provider's dashboard within the same window.
- **SC-007**: A new engineer following only the beginner guide produced by this feature can configure a local environment, complete a test payment, and explain how the webhook idempotency works in under 90 minutes, without asking another team member for help.
- **SC-008**: All existing cash-on-delivery test scenarios continue to pass unchanged — no regression in the COD checkout flow.
- **SC-009**: 100% of the new payment, refund, and webhook-handling code paths are covered by automated unit tests that pass on every CI build.

## Assumptions

The following defaults were chosen because no business constraint suggested otherwise; they should be confirmed during planning if any is incorrect:

- **A-001**: Currency is **VND** (Vietnamese đồng), single-currency, matching the rest of the storefront. The chosen provider supports VND as a zero-decimal currency.
- **A-002**: The integration uses the payment provider's **hosted checkout experience** (provider-hosted card form) rather than building a custom card form. This keeps Morii Coffee out of the strictest PCI-DSS scope and is the recommended path for small/medium merchants.
- **A-003**: The MVP supports **single full charges**; recurring/subscription payments, saved cards, digital wallets, and split tenders are **out of scope** for this feature.
- **A-004**: The customer is **redirected** to the provider-hosted page and back to a success/cancel URL on the Morii Coffee storefront. An embedded card-element flow may be considered in a follow-up.
- **A-005**: The **storefront URL** for success/cancel redirects is the existing `StorefrontUrl` from configuration (e.g., `https://morii-coffee-fe.vercel.app` in production, `http://localhost:3000` in development).
- **A-006**: **Refunds initiate from the admin dashboard only**; there is no customer-initiated refund flow in this iteration.
- **A-007**: A **single merchant account** is used; multi-tenant or marketplace flows are out of scope.

## Dependencies

- A payment-provider account (free to register) with access to both test mode (for development and CI) and live mode (for production).
- A publicly reachable HTTPS URL for the production webhook endpoint (a local tunnelling tool is sufficient for local development).
- The existing cart, order, and user-authentication features must remain functional; this feature **extends** them, it does not replace them.

## Out of Scope

The following are explicitly **not** part of this feature; they may be tackled in follow-up features:

- Subscriptions, recurring billing, or saved payment methods on the customer's account.
- Digital wallets (Apple Pay, Google Pay, Stripe Link, etc.) or any non-card payment method beyond the existing COD.
- Customer-initiated refund requests (only admin-initiated refunds in scope).
- Multi-currency support.
- Marketplace / split-payout flows to sellers.
- Anti-fraud rules customisation (provider's default fraud rules are accepted as-is).
- Receipt PDF generation by Morii Coffee (the provider's emailed receipts are sufficient for MVP).
