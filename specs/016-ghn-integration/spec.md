# Feature Specification: GHN Sandbox Integration

**Feature Branch**: `[016-ghn-integration]`  
**Created**: 2026-05-24  
**Status**: Draft  
**Input**: User description: "Read the GHN integration feature documents and start the implementation flow from specify for a sandbox-only GHN integration that covers structured delivery addresses, shipping quotes, shipment creation, shipment tracking, admin shipment management, tests, and readiness for implementation."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Checkout With GHN Delivery (Priority: P1)

As a customer, I want to choose home delivery, enter a structured delivery address, and receive a shipping quote before placing my order so I can complete checkout with an accurate delivery cost and expected arrival time.

**Why this priority**: Without a valid delivery quote and address structure, the business cannot support GHN-based delivery orders reliably.

**Independent Test**: Can be fully tested by selecting GHN delivery during checkout, choosing a valid destination, receiving a quote and lead-time estimate, and placing an order that preserves the chosen shipping details.

**Acceptance Scenarios**:

1. **Given** a customer has shippable items in the cart and selects GHN delivery, **When** the customer provides a valid province, district, ward, address line, recipient name, and phone number, **Then** the system returns at least one delivery option with shipping fee, estimated delivery time, and quote validity information.
2. **Given** a customer is using GHN delivery, **When** the address or checkout data becomes invalid or unsupported for shipping, **Then** the system prevents order placement for that delivery method and shows a clear reason the quote cannot be used.
3. **Given** a customer receives a valid shipping quote, **When** the customer places the order before the quote expires and without changing the quoted inputs, **Then** the order stores the selected delivery method, delivery address snapshot, chosen service, and quoted shipping charge.

---

### User Story 2 - Fulfill And Manage Shipment (Priority: P2)

As an operations staff member, I want shipment records to be created and managed from the order lifecycle so I can fulfill delivery orders, retry failed shipment creation, sync the latest carrier state, and cancel eligible shipments when needed.

**Why this priority**: Delivery orders are not operationally complete unless the business can create and manage the outbound shipment after the order exists.

**Independent Test**: Can be fully tested by placing a delivery order, creating or retrying shipment creation, viewing shipment details from the admin order view, syncing the latest status, and cancelling a shipment when the carrier still allows it.

**Acceptance Scenarios**:

1. **Given** a newly placed delivery order that is eligible for carrier fulfillment, **When** the system processes shipment creation, **Then** it creates at most one carrier shipment for that order and records the carrier shipment identifiers and current shipment status.
2. **Given** a delivery order whose shipment could not be created, **When** an admin retries shipment creation, **Then** the system either creates the missing shipment or safely returns the existing shipment without creating a duplicate.
3. **Given** an existing shipment, **When** an admin requests a manual sync or cancellation, **Then** the system refreshes or updates the shipment state and returns the resulting status and any relevant failure or cancellation information.

---

### User Story 3 - Track Shipment Progress (Priority: P3)

As a customer or admin, I want to see the shipment state alongside the order so I can understand whether delivery is pending, in transit, delivered, cancelled, or needs attention.

**Why this priority**: Shipment visibility reduces ambiguity after checkout and gives staff a single place to monitor delivery progress.

**Independent Test**: Can be fully tested by opening order detail for pickup and delivery orders and verifying that the system displays the correct shipment summary, including no-shipment states, pending creation states, and active carrier states.

**Acceptance Scenarios**:

1. **Given** an order that does not require carrier delivery, **When** the order detail is viewed, **Then** the system shows that no shipment is required.
2. **Given** an order with a created shipment, **When** the order detail is viewed, **Then** the system shows the shipping provider, shipment status, carrier reference codes, fee snapshot, and expected delivery timing when available.
3. **Given** a carrier status update is received or manually synced, **When** the order detail is viewed afterwards, **Then** the shipment summary reflects the latest accepted shipment state without changing unrelated payment information.

### Edge Cases

- What happens when a customer changes the address, cart contents, or selected delivery option after receiving a quote but before placing the order?
- How does the system handle unsupported delivery routes, missing carrier services, or temporary carrier-side outages during quoting?
- What happens when the order is created successfully but shipment creation fails afterwards?
- How does the system prevent duplicate shipment creation when the same order is retried or processed more than once?
- How does the system handle repeated or out-of-order shipment status updates from the carrier?
- What happens when a prepaid order is shipped but no cash should be collected on delivery?
- How does the system behave when a shipment can no longer be cancelled at the carrier even though a user attempts the action?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support a delivery method that represents GHN home delivery separately from non-shipping fulfillment methods.
- **FR-002**: The system MUST let customers provide a structured delivery address containing recipient name, phone number, address line, province, district, and ward information for carrier-delivered orders.
- **FR-003**: The system MUST reject carrier-delivered checkout attempts when the delivery address is incomplete, invalid for delivery, or cannot be resolved to a supported route.
- **FR-004**: The system MUST provide a shipping quote for eligible carrier-delivered checkout requests that includes the selected service, shipping charge, expected delivery timing, and quote validity information.
- **FR-005**: The system MUST treat shipping quotes as temporary and MUST require a refreshed quote when quoted inputs materially change or the quote is no longer valid.
- **FR-006**: The system MUST allow pickup or other non-carrier orders to continue without requiring shipping quote data.
- **FR-007**: The system MUST persist a delivery snapshot on each carrier-delivered order that includes the structured address, chosen delivery service, quoted shipping charge, and quote reference used at order placement.
- **FR-008**: The system MUST attempt to create a carrier shipment for shipment-eligible delivery orders after the commerce order has been created.
- **FR-009**: The system MUST preserve the commerce order even if shipment creation fails and MUST mark the shipment state so staff can identify and retry the failure.
- **FR-010**: The system MUST enforce idempotent shipment creation so the same order cannot create duplicate carrier shipments.
- **FR-011**: The system MUST store both Morii order identifiers and carrier shipment identifiers so shipment records can be retrieved, synced, and audited reliably.
- **FR-012**: The system MUST expose shipment summary information on order detail views for both customers and admins, with more operational controls available only to authorized staff.
- **FR-013**: The system MUST allow authorized staff to retry shipment creation, refresh the latest shipment state, refresh the shipping quote, update shipment notes, and cancel eligible shipments.
- **FR-014**: The system MUST accept asynchronous shipment status updates from the carrier, record the incoming payload for audit purposes, and update the normalized shipment state idempotently.
- **FR-015**: The system MUST keep shipment state separate from payment state so shipment updates do not incorrectly change payment outcomes.
- **FR-016**: The system MUST support saved delivery profile data that can prefill structured delivery information for future checkout sessions.
- **FR-017**: The system MUST show clear user-facing reasons when carrier delivery cannot be quoted, created, or updated.
- **FR-018**: The system MUST operate this feature in a sandbox carrier environment only for the first release.

### Key Entities *(include if feature involves data)*

- **Shipping Address**: A normalized delivery destination snapshot containing recipient identity, contact information, address line, and administrative area identifiers and labels.
- **Shipping Quote**: A temporary pricing and lead-time result for a specific delivery request, including selected service, fee details, validity window, and a quote reference.
- **Shipment**: A carrier fulfillment record linked to a Morii order, containing carrier identifiers, service selection, current shipment state, fee snapshot, expected delivery timing, and failure details when applicable.
- **Shipment Event Audit**: A retained record of incoming carrier updates and processing outcomes used for traceability and debugging.
- **Delivery Profile**: A reusable customer delivery preference record used to prefill structured shipping data during checkout.

### Assumptions

- The first release is limited to GHN sandbox usage and does not include production carrier activation.
- Pickup and carrier delivery remain separate fulfillment paths, and only carrier delivery orders participate in shipment orchestration.
- Shipment creation is attempted immediately after order creation for eligible delivery orders, while allowing the order itself to succeed even if shipment creation fails.
- The business accepts a normalized shipment status model that maps carrier-specific status values into Morii-facing shipment states.
- The business is willing to capture and display structured address data instead of relying on a single free-text address field for carrier-delivered orders.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of valid GHN delivery checkout attempts return a delivery quote with fee and estimated arrival information within 5 seconds.
- **SC-002**: 100% of carrier-delivered orders store a complete delivery snapshot and either a created shipment record or an explicit shipment creation failure state.
- **SC-003**: 100% of duplicate shipment creation attempts for the same order resolve without producing more than one carrier shipment record.
- **SC-004**: 95% of shipment status updates that are accepted by the system appear on the related order view within 1 minute of receipt or manual sync.
- **SC-005**: At least 90% of successful GHN delivery checkouts complete without requiring staff intervention after order placement.
- **SC-006**: Authorized staff can complete retry, sync, or cancellation actions for an eligible shipment in under 2 minutes end-to-end.
