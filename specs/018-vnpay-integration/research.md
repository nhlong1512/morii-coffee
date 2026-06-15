# Research: VNPAY Integration

## Decision 1: Complete the provider-neutral payment migration before adding VNPAY

**Decision**: Rename and extend the existing Stripe-specific payment identities, checkout draft, webhook audit, refund identity, order payment identifiers, repositories, DTOs, and handlers into provider-neutral equivalents before introducing VNPAY commands.

**Rationale**:
- Current payment history, refund routing, repository lookups, and finalization use Stripe-named fields.
- Storing VNPAY references in Stripe fields would make support, refunds, and future reconciliation ambiguous.
- A provider discriminator plus provider-scoped unique indexes preserves ownership and prevents cross-provider identifier collisions.

**Alternatives considered**:
- Store VNPAY in existing Stripe fields: rejected because it corrupts domain meaning and frontend contracts.
- Build a separate VNPAY payment subsystem: rejected because it duplicates finalization, history, refund, and audit behavior.

## Decision 2: Keep one provider-neutral gateway contract with a resolver

**Decision**: Extend `IPaymentGateway` into a provider-neutral hosted-payment contract and add `IPaymentGatewayResolver.Resolve(provider)` for checkout, reconcile, status, and refund operations.

**Rationale**:
- Existing handlers inject one Stripe gateway directly, which cannot safely route multi-provider payments.
- Persisted provider ownership is the correct routing source for refunds and reconciliation.
- Resolver-based routing keeps provider-specific SDK/protocol details in Infrastructure.

**Alternatives considered**:
- Inject both concrete gateways into each handler: rejected because it spreads provider branching.
- Add VNPAY-only application interfaces alongside the current gateway: rejected because refund/reconcile/history would remain fragmented.

## Decision 3: Treat VNPAY IPN as authoritative and browser return as read-only

**Decision**: Verified IPN and verified QueryDR reconciliation may finalize payment; the return endpoint verifies authenticity only to produce a sanitized frontend redirect.

**Rationale**:
- Customers can close the browser or replay return parameters.
- IPN is the provider-to-server confirmation path and supports retries.
- This matches the existing payment-first principle that orders are finalized after provider confirmation.

**Alternatives considered**:
- Mark paid on successful return: rejected because browser redirects are replayable and non-authoritative.
- Require return before finalization: rejected because valid payments must complete when the customer closes the browser.

## Decision 4: Normalize provider events before application dispatch

**Decision**: Add an internal payment event kind (`PaymentSucceeded`, `PaymentFailed`, `PaymentExpired`, `RefundSucceeded`) and have Stripe/VNPAY adapters map provider-specific semantics into it.

**Rationale**:
- `HandleWebhookEventCommandHandler` currently switches on Stripe event names despite using a provider-oriented envelope.
- VNPAY IPN has status fields rather than Stripe-style event names.
- Normalized event kinds let finalization and audit behavior remain shared.

**Alternatives considered**:
- Add VNPAY strings to the existing Stripe switch: rejected because application logic would remain provider-specific.
- Give VNPAY a separate finalizer: rejected because it duplicates the most sensitive transaction path.

## Decision 5: Use one canonicalization/signature implementation for create, IPN, and return

**Decision**: Implement shared ordinal sorting, consistent URL encoding, HMAC-SHA512 signing, constant-time verification, and golden-vector tests in the VNPAY Infrastructure adapter.

**Rationale**:
- Inconsistent encoding between create and verification causes production-only signature failures.
- Cryptographic and provider protocol behavior belongs outside controllers and handlers.
- Golden vectors provide deterministic regression coverage independent of live sandbox access.

**Alternatives considered**:
- Build query strings in controllers: rejected because it leaks protocol/security logic into Presentation.
- Use the JavaScript VNPAY package: rejected because it is server-side Node.js software and secrets must not enter the browser.

## Decision 6: Use GMT+7 through a testable clock abstraction

**Decision**: Generate VNPAY timestamps through a provider clock abstraction that converts from UTC to GMT+7 and formats provider timestamps deterministically.

**Rationale**:
- VNPAY requires GMT+7 timestamps and exact formats.
- A testable clock avoids flaky signature, expiry, QueryDR, and refund tests.
- Domain/application state remains UTC while the adapter performs protocol conversion.

**Alternatives considered**:
- Use local machine time directly: rejected because deployment timezone can differ.
- Store all payment timestamps as GMT+7: rejected because repository conventions use UTC.

## Decision 7: Preserve VND as authoritative local amount and multiply by 100 only at the VNPAY boundary

**Decision**: Keep local cart/order/payment amounts in VND and convert to/from VNPAY's `amount × 100` representation exactly once in the gateway.

**Rationale**:
- Existing Stripe VND handling is zero-decimal and must not change.
- Boundary conversion prevents accidental double multiplication and makes amount validation explicit.
- Checked arithmetic protects against overflow.

**Alternatives considered**:
- Store provider-scaled amount locally: rejected because it would conflict with all existing monetary values.
- Convert in handlers: rejected because provider quirks belong in Infrastructure.

## Decision 8: Use database uniqueness as the idempotency authority

**Decision**: Back VNPAY IPN idempotency with provider-scoped unique audit identities and provider-scoped payment references, while keeping finalization inside the existing transaction boundary.

**Rationale**:
- VNPAY retries and concurrent delivery can race.
- The existing Stripe flow already relies on database uniqueness for callback idempotency.
- A deterministic VNPAY event identity can be derived from transaction reference, provider transaction number, response code, and transaction status.

**Alternatives considered**:
- In-memory or distributed locks only: rejected because locks do not provide durable audit/idempotency guarantees.
- Ignore duplicate callbacks without audit: rejected because support and forensic traceability are required.

## Decision 9: Use QueryDR only for reconciliation and verify its response

**Decision**: Query local finalized state first; use signed QueryDR only for pending attempts and verify the response before applying transaction status.

**Rationale**:
- QueryDR is recovery/support tooling, not the primary confirmation path.
- Provider API response success does not itself mean the transaction succeeded.
- Local-first reads reduce unnecessary provider calls and return stable finalized state quickly.

**Alternatives considered**:
- Query VNPAY on every return: rejected because it adds latency and provider dependency.
- Trust unsigned or merely HTTP-success QueryDR results: rejected for payment integrity.

## Decision 10: Gate VNPAY refunds by merchant capability

**Decision**: Route the existing admin refund/reconcile workflow by persisted provider, but reject VNPAY refunds clearly until the merchant capability is enabled; preserve asynchronous refund states.

**Rationale**:
- Sandbox refund access may be restricted.
- An accepted refund request may not be settled by the customer's bank yet.
- Provider-neutral refund routing is required to avoid sending VNPAY payments to Stripe.

**Alternatives considered**:
- Exclude refunds entirely: rejected because the feature explicitly includes them when enabled.
- Treat accepted request as settled: rejected because it creates inaccurate financial state.

## Decision 11: Deliver the frontend handoff after backend verification

**Decision**: Generate `docs/features/vnpay-integration/FRONTEND_HANDOFF.md` during implementation after contracts are verified.

**Rationale**:
- The frontend is outside this repository's implementation scope.
- The handoff must reflect the final verified contracts and payment state behavior.
- Frontend must never sign VNPAY data or trust return status alone.

**Alternatives considered**:
- Write the handoff now during planning: rejected because implementation details may change before verification.
