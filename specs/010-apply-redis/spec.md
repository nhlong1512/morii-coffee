# Feature Specification: Redis-Backed Core Flows

**Feature Branch**: `010-apply-redis`  
**Created**: 2026-04-24  
**Status**: Draft  
**Input**: User description: "Review docs/features/apply-redis and implement the apply-redis feature for this project; start with specify first"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse an up-to-date menu quickly (Priority: P1)

As a customer, I want the product catalog to load quickly while still reflecting recent product changes so that I can browse and decide what to order without delay or confusion.

**Why this priority**: The catalog is the highest-traffic customer journey and directly affects discovery, conversion, and trust in displayed pricing and availability.

**Independent Test**: Can be fully tested by retrieving product lists and product details repeatedly, then updating a product and confirming refreshed results are shown without prolonged stale data.

**Acceptance Scenarios**:

1. **Given** the catalog has active products, **When** a customer opens the product list, **Then** the system returns the list within the expected response time and shows the current published product data.
2. **Given** a product was recently updated by an authorized staff member, **When** a customer opens that product detail or a related product list, **Then** the updated product information is shown without requiring manual cache clearing or system restart.
3. **Given** the high-speed data layer is temporarily unavailable, **When** a customer requests the product list or product detail, **Then** the system still returns catalog data from the primary data source instead of failing the request.

---

### User Story 2 - Keep an active shopping cart between sessions (Priority: P2)

As an authenticated customer, I want my shopping cart to remain available across requests and short gaps in activity so that I can continue building an order without re-adding items.

**Why this priority**: Cart continuity directly affects order completion and is more valuable than generic storage because cart contents are short-lived, frequently updated, and must survive normal app restarts.

**Independent Test**: Can be fully tested by adding items to a signed-in cart, changing quantities, restarting the API, and confirming the same cart remains available until it expires or is cleared.

**Acceptance Scenarios**:

1. **Given** an authenticated customer has no active cart, **When** they add an item, **Then** the system creates a cart for that customer and stores the selected item details and quantity.
2. **Given** an authenticated customer adds the same product option again, **When** the add action succeeds, **Then** the system increases the quantity of the existing cart line instead of creating a duplicate line.
3. **Given** an authenticated customer has an active cart, **When** they update quantity, remove an item, or clear the cart, **Then** the cart reflects the latest change immediately.
4. **Given** an authenticated customer returns after a short interruption, **When** they reopen the cart before it expires, **Then** the previously stored cart contents are still available.

---

### User Story 3 - Complete password reset through a short-lived reset session (Priority: P3)

As a user who forgot a password, I want a reset link that works once and expires quickly so that I can recover my account safely without exposing sensitive reset data to the client.

**Why this priority**: Password reset is lower traffic than catalog and cart, but it is security-sensitive and benefits from stronger control over expiration and one-time usage.

**Independent Test**: Can be fully tested by requesting a reset, using the issued reset link successfully once, and confirming expired or previously used links are rejected with a clear message.

**Acceptance Scenarios**:

1. **Given** a user requests a password reset for an existing account, **When** the request is accepted, **Then** the system issues a short-lived reset session and sends a reset link without exposing internal reset credentials directly.
2. **Given** a user opens a valid reset link within the allowed time window, **When** they submit a compliant new password, **Then** the password is updated and the reset session becomes unusable immediately afterward.
3. **Given** a reset link is expired, invalid, or already consumed, **When** the user attempts to reset the password, **Then** the system rejects the action and provides a clear recovery path.

### Edge Cases

- What happens when catalog data changes while customers are actively browsing cached product pages? The system must refresh affected customer-facing views quickly enough that stale product information does not persist beyond a brief transition window.
- How does the system handle an unavailable high-speed data layer during catalog reads? The system must fall back to the primary data source for catalog requests rather than failing customer reads.
- What happens when a cart expires before checkout? The system must return an empty cart state and require the customer to rebuild the cart rather than restoring unknown or partial data.
- How does the system handle an item that becomes unavailable or changes price after it was added to the cart? The customer must be warned during checkout review and required to confirm the updated cart before order submission.
- What happens when a password reset link is reused or requested multiple times? Only the most recent valid reset session should remain usable, and consumed or expired sessions must not succeed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST accelerate customer-facing product list and product detail retrieval for repeated reads while preserving correct published catalog data.
- **FR-002**: The system MUST refresh affected product list and product detail results after successful product, variant, pricing, or image changes so customers do not continue seeing outdated catalog information.
- **FR-003**: The system MUST continue serving catalog reads from the primary data source when the high-speed data layer is unavailable.
- **FR-004**: The system MUST provide one active cart per authenticated customer.
- **FR-005**: The system MUST store cart line items with enough snapshotted product information for the customer to review the cart without reloading every item from the catalog on each request.
- **FR-006**: The system MUST increase quantity for an existing cart line when the same product option is added again by the same authenticated customer.
- **FR-007**: The system MUST allow an authenticated customer to view the current cart, change line quantities, remove individual lines, and clear the full cart.
- **FR-008**: The system MUST keep an active cart available across requests and normal application restarts until the cart is cleared, converted to an order, or expires due to inactivity.
- **FR-009**: The system MUST treat cart contents as time-limited session data and extend the activity window after each successful cart change.
- **FR-010**: The system MUST remove the active cart after a successful checkout so stale cart contents are not reused accidentally.
- **FR-011**: The system MUST issue password reset sessions that are short-lived, opaque to the client, and usable only once.
- **FR-012**: The system MUST invalidate any password reset session immediately after a successful password reset.
- **FR-013**: The system MUST reject expired, invalid, or previously consumed password reset sessions with a clear user-facing outcome.
- **FR-014**: The system MUST preserve the existing privacy behavior of password reset requests by avoiding account enumeration in public-facing responses.
- **FR-015**: The system MUST record operational events for catalog refreshes, cart lifecycle changes, and password reset session usage so support and engineering teams can diagnose failures and abnormal behavior.

### Key Entities *(include if feature involves data)*

- **Catalog View**: A customer-facing representation of product lists, product details, variant availability, and related merchandising information that must remain fast to read and reasonably fresh after updates.
- **Customer Cart**: A time-limited order-in-progress for one authenticated customer containing selected items, snapshotted item details, quantities, totals, and last-activity timestamp.
- **Cart Line**: A single selected product option within a cart, including the chosen item, quantity, displayed price at selection time, and display metadata required for review.
- **Password Reset Session**: A short-lived, one-time recovery record associated with a user account that authorizes exactly one password reset attempt within a limited validity window.

## Assumptions

- The first delivery phase covers authenticated customer carts only; guest carts and cart merge behavior remain out of scope.
- Catalog acceleration is limited to high-value read flows for product lists and product details; broader system-wide caching is not part of this feature.
- Checkout remains responsible for final validation of current price and availability before order creation.
- Password reset continues to rely on the existing account recovery experience and notification channels, with stronger control over expiration and one-time use.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of repeated product list and product detail requests complete within 1 second under normal operating conditions.
- **SC-002**: After an authorized catalog change is completed, affected customer-facing product views reflect the change on the next successful read in at least 99% of observed cases.
- **SC-003**: During a temporary outage of the high-speed data layer, 100% of baseline catalog read flows remain available through the primary data source, excluding unrelated upstream failures.
- **SC-004**: At least 90% of authenticated customers who add an item to cart can return to the same cart after an application restart or brief inactivity period, provided the cart has not expired or been checked out.
- **SC-005**: In checkout validation tests, 100% of carts containing stale price or availability data are flagged for customer review before order submission.
- **SC-006**: In password reset tests, 100% of expired or previously used reset links are rejected, and 100% of valid reset links can be completed exactly once within the allowed time window.
