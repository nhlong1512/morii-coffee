# Feature Specification: VNPAY Integration

**Feature Branch**: `[018-vnpay-integration]`  
**Created**: 2026-06-15  
**Status**: Draft  
**Input**: User description: "Specify a VNPAY integration based on the existing VNPAY integration guide, including payment checkout, authoritative confirmation, reconciliation, refunds where enabled, unit and regression testing expectations, build readiness, and a frontend handoff document. This step is specification only; implementation follows in later plan and task phases."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Complete Checkout With VNPAY (Priority: P1)

As an authenticated customer, I want to choose VNPAY during checkout and complete payment on the hosted VNPAY payment page so that my order is finalized after the payment is securely confirmed.

**Why this priority**: This is the primary customer and business outcome of the integration.

**Independent Test**: Can be fully tested by starting with a valid cart and delivery selection, choosing VNPAY, completing a successful payment, and confirming that exactly one paid order is created for the correct amount.

**Acceptance Scenarios**:

1. **Given** an authenticated customer has a valid cart, delivery information, and shipping selection, **When** the customer starts a VNPAY checkout, **Then** the system provides a time-limited hosted payment destination and records the pending checkout attempt.
2. **Given** a pending VNPAY checkout, **When** VNPAY authoritatively confirms a successful payment for the expected merchant, reference, and amount, **Then** the system creates exactly one paid order and records the provider transaction details.
3. **Given** a pending VNPAY checkout, **When** VNPAY reports a failed or suspicious terminal payment outcome, **Then** the system does not create a paid order and records a diagnosable failure state.

---

### User Story 2 - Return Safely From VNPAY (Priority: P1)

As a customer returning from VNPAY, I want to see a clear payment state and finalized order when available so that I know whether to wait, retry, or continue to my order.

**Why this priority**: Customers need clear feedback after leaving the storefront, while payment integrity must not depend on the browser return path.

**Independent Test**: Can be fully tested by returning from successful, pending, failed, and invalid payment attempts and verifying that the customer sees the correct state without the return action itself marking an order as paid.

**Acceptance Scenarios**:

1. **Given** a customer returns from VNPAY with authentic return information, **When** the authoritative payment confirmation has already completed, **Then** the customer can see the paid state and navigate to the finalized order.
2. **Given** a customer returns before authoritative confirmation completes, **When** the customer views the return state, **Then** the system shows a pending state and can refresh the authoritative status.
3. **Given** return information is invalid, tampered with, or replayed, **When** it is processed, **Then** no payment or order is marked paid and the customer sees a safe, non-success result.

---

### User Story 3 - Recover Missing Or Delayed Confirmation (Priority: P2)

As a customer or authorized administrator, I want pending VNPAY payments to be reconciled when confirmation is delayed or missing so that successful payments are not stranded and failed payments remain actionable.

**Why this priority**: Reconciliation provides operational recovery from delivery delays, outages, and interrupted customer sessions.

**Independent Test**: Can be fully tested by reconciling a locally pending attempt against successful, failed, and still-pending provider states and verifying that only authentic provider results change local state.

**Acceptance Scenarios**:

1. **Given** a locally pending payment that VNPAY reports as successful, **When** an authorized reconciliation occurs, **Then** the system finalizes the payment and order exactly once.
2. **Given** a locally pending payment that VNPAY still reports as pending, **When** reconciliation occurs, **Then** the payment remains pending and no order is incorrectly finalized.
3. **Given** a customer attempts to reconcile another customer's checkout, **When** authorization is evaluated, **Then** the request is rejected without revealing or changing the payment.

---

### User Story 4 - Operate And Support VNPAY Payments (Priority: P2)

As an administrator, I want to view VNPAY payment attempts, provider transaction details, failures, and refund progress so that I can support customers and reconcile financial operations.

**Why this priority**: Payment operations require traceability and consistent support workflows across payment providers.

**Independent Test**: Can be fully tested by viewing payment history for Stripe and VNPAY orders, identifying the owning provider and transaction details, and processing or reconciling an eligible refund.

**Acceptance Scenarios**:

1. **Given** payments from multiple providers, **When** an authorized user views payment history, **Then** each attempt clearly identifies its provider, provider-owned references, amount, status, and relevant diagnostic details.
2. **Given** a paid VNPAY order and enabled merchant refund capability, **When** an administrator requests a valid full or partial refund, **Then** the system records the request and tracks it until a terminal refund outcome.
3. **Given** VNPAY refund capability is not enabled for the merchant, **When** an administrator requests a refund, **Then** the system rejects the operation clearly without corrupting local payment state.

---

### User Story 5 - Continue Frontend Delivery From A Stable Handoff (Priority: P3)

As a frontend developer, I want a complete handoff describing supported VNPAY customer flows, payment states, contracts, and security boundaries so that I can implement the storefront and admin experiences without exposing secrets or guessing backend behavior.

**Why this priority**: The customer-facing flow spans backend and frontend repositories and requires an explicit integration boundary.

**Independent Test**: Can be fully tested by reviewing the handoff and confirming that a frontend developer can identify all required checkout, return, reconciliation, payment display, error, and test scenarios without undocumented assumptions.

**Acceptance Scenarios**:

1. **Given** the backend feature is complete, **When** the frontend handoff is reviewed, **Then** it describes required customer and admin interactions, request and response data, payment states, security rules, and expected frontend tests.
2. **Given** a frontend developer follows the handoff, **When** implementing the return experience, **Then** the frontend does not treat browser return information alone as authoritative payment success.

### Edge Cases

- What happens when a customer closes the browser before returning from VNPAY?
- How does the system handle invalid signatures, incorrect merchant identity, unknown transaction references, or mismatched payment amounts?
- What happens when the same payment confirmation is delivered repeatedly or concurrently?
- How does the system handle a successful browser return received before authoritative payment confirmation?
- What happens when a later failure, reversal, or suspicious result arrives for an already-paid transaction?
- How does the system handle expired checkout attempts and transactions that remain pending for an extended period?
- What happens when reconciliation is unavailable, times out, or returns information that cannot be authenticated?
- How are full and partial refunds handled when accepted but not yet settled by the customer's bank?
- How does VNPAY remain classified as prepaid so delivery charges are not incorrectly collected as cash on delivery?
- How are existing Stripe and cash-on-delivery checkouts protected from regressions during the payment model changes?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow authenticated customers with a valid cart and valid delivery selection to choose VNPAY as an online prepaid payment method.
- **FR-002**: The system MUST calculate the payable amount from authoritative cart and validated shipping information rather than accepting a customer-supplied payment amount.
- **FR-003**: The system MUST record a pending checkout attempt before directing the customer to VNPAY.
- **FR-004**: The system MUST assign a unique merchant transaction reference to each VNPAY checkout attempt.
- **FR-005**: The system MUST ensure payment signing secrets and transaction-signing operations remain inaccessible to frontend consumers.
- **FR-006**: The system MUST authenticate VNPAY payment notifications and return information before using them for business decisions.
- **FR-007**: The system MUST verify merchant identity, transaction reference, expected amount, and all required success indicators before marking a VNPAY payment as paid.
- **FR-008**: The system MUST treat authoritative provider confirmation, rather than the customer browser return, as the primary path for finalizing payment.
- **FR-009**: The system MUST finalize the order and successful payment as one consistent outcome.
- **FR-010**: The system MUST process repeated and concurrent payment confirmations idempotently so one checkout produces at most one finalized order and one successful payment transition.
- **FR-011**: The system MUST keep a verifiable audit history of payment notifications and their processing outcomes without exposing secrets or sensitive signed payloads.
- **FR-012**: The system MUST show customers a sanitized pending, paid, failed, expired, or invalid result after they return from VNPAY.
- **FR-013**: The browser return flow MUST NOT independently mark an order or payment as paid.
- **FR-014**: The system MUST allow an authenticated customer to reconcile a VNPAY checkout they own and MUST allow authorized administrators to reconcile supported payment attempts.
- **FR-015**: The system MUST authenticate provider reconciliation results before applying them to local payment or order state.
- **FR-016**: The system MUST distinguish the customer's payment method from the external provider that owns a payment attempt.
- **FR-017**: The system MUST represent payment attempts, provider references, payment outcomes, response details, and refund records consistently across supported payment providers.
- **FR-018**: The system MUST allow authorized administrators to view VNPAY transaction identifiers, payment state, response details, bank or card information when available, and refund history.
- **FR-019**: The system MUST support full and partial VNPAY refunds only when the merchant's refund capability is enabled.
- **FR-020**: The system MUST preserve pending, succeeded, and failed refund states because an accepted refund request may settle asynchronously.
- **FR-021**: The system MUST identify reversed, suspicious, and refund-in-progress outcomes without incorrectly fulfilling an order.
- **FR-022**: The system MUST preserve existing Stripe and cash-on-delivery customer and administrative behavior.
- **FR-023**: The system MUST provide clear, actionable failure information for customers and administrators without exposing credentials or complete signed payment data.
- **FR-024**: The completed implementation MUST include automated coverage for payment signing and verification, amount integrity, status mapping, idempotency, authorization, reconciliation, refunds, and existing payment-flow regressions.
- **FR-025**: The completed implementation MUST pass the repository's build and automated test suite before it is considered ready for delivery.
- **FR-026**: The completed implementation MUST include a frontend handoff document covering customer and admin flows, payment states, integration data, security boundaries, and frontend testing expectations.
- **FR-027**: The first release MUST support VNPAY PAY and MUST exclude token, installment, and recurring payment products.

### Key Entities *(include if feature involves data)*

- **Checkout Draft**: A temporary authoritative snapshot of the customer, cart, delivery selection, shipping charge, payable amount, selected payment method, owning provider, payment state, and expiry before an order exists.
- **Payment Attempt**: A provider-owned payment transaction associated with a finalized order, including provider references, amount, currency, status, response details, payment time, bank information, and card type when available.
- **Payment Notification Audit**: An idempotent record of an incoming provider notification, its deterministic identity, integrity fingerprint, processing result, and diagnostic outcome.
- **Order**: The finalized customer purchase created only after authoritative successful confirmation for VNPAY checkout.
- **Refund Record**: A full or partial refund request linked to a successful payment attempt, including provider reference, requested amount, initiating administrator, lifecycle state, and failure details.

### Assumptions

- The first release uses the VNPAY sandbox environment; production activation follows successful sandbox acceptance and merchant provisioning.
- VNPAY PAY is the only VNPAY product in scope; token, installment, and recurring products are excluded.
- VNPAY notifications may be delayed, repeated, or delivered after the customer closes the browser.
- Provider confirmation is the source of truth for paid status, while customer return information is used only to guide the user experience.
- VNPAY refund and transaction-query capabilities depend on merchant enablement and may be restricted in sandbox.
- The frontend implementation is delivered in a separate repository after the backend handoff is complete.
- Existing payment records must retain their provider ownership and transaction history during any provider-neutral migration.
- Sandbox acceptance testing requires publicly reachable secure callback endpoints and valid sandbox merchant credentials.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of customers with valid checkout information receive a VNPAY payment destination within 5 seconds under normal operating conditions.
- **SC-002**: 100% of valid successful VNPAY confirmations create exactly one paid order for the authoritative checkout amount.
- **SC-003**: 100% of invalid, unknown-reference, wrong-merchant, or amount-mismatch confirmations create no paid order.
- **SC-004**: Repeated and concurrent confirmations for the same VNPAY transaction produce zero duplicate orders and zero duplicate successful payment transitions.
- **SC-005**: Customers can retrieve an authoritative pending or final payment state within 10 seconds after returning from VNPAY under normal provider conditions.
- **SC-006**: Authorized administrators can identify the provider, transaction reference, amount, status, and support-relevant outcome for 100% of recorded VNPAY attempts.
- **SC-007**: 100% of VNPAY refund requests preserve an accurate pending, succeeded, or failed state until settlement is known.
- **SC-008**: Existing Stripe and cash-on-delivery automated regression scenarios continue to pass without behavioral changes.
- **SC-009**: The completed backend implementation builds successfully and all required automated tests pass with zero failures before handoff.
- **SC-010**: A frontend developer can implement the documented VNPAY checkout, return, reconciliation, and payment-history experiences without requiring provider secrets or undocumented backend behavior.
