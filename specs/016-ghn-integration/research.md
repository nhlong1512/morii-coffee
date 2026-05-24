# Research: GHN Sandbox Integration

## Decision 1: Keep GHN as a dedicated sandbox integration, not a generic shipping-provider platform

**Decision**: Implement GHN as a dedicated provider-specific slice for the first release, with Morii-native application contracts on top.

**Rationale**:
- The spec explicitly limits the release to sandbox-only GHN.
- The current codebase has no shipping abstraction to preserve, so a generic provider layer would add indirection without immediate value.
- GHN docs have payload quirks and inconsistent headers; isolating those quirks in a focused adapter minimizes blast radius.

**Alternatives considered**:
- Build a generic `IShippingProvider` platform now: rejected because there is only one provider in scope and the abstraction would be premature.
- Call GHN directly from the frontend: rejected because credentials, payload translation, idempotency, and webhook handling belong in the backend.

## Decision 2: Extend current order and delivery-profile models instead of creating parallel checkout objects

**Decision**: Expand `DeliveryInfo`, `UserDeliveryProfile`, and related order DTOs to store structured delivery address fields alongside the human-readable address snapshot.

**Rationale**:
- Current order placement and saved delivery profile flows already persist delivery data and are the narrowest existing extension point.
- Checkout and Stripe finalization both currently rely on `FullName`, `PhoneNumber`, and `Address`; extending those models avoids splitting delivery data across multiple sources.
- Structured address data is needed both for quote creation and for long-lived order history.

**Alternatives considered**:
- Store structured address only in a separate shipment table: rejected because order history would lose an authoritative delivery snapshot.
- Keep free-text address and derive structured codes externally: rejected because GHN quote and shipment calls require stable district/ward resolution.

## Decision 3: Attempt shipment creation immediately after order creation, but do not make order success depend on GHN success

**Decision**: Create the commerce order first, then attempt GHN shipment creation in the backend as part of the post-order flow, recording `FAILED_TO_CREATE` when GHN fails.

**Rationale**:
- The spec requires orders to remain valid even if provider creation fails.
- This matches the repository's current pattern of treating external-provider state separately from order state, as seen in Stripe finalization and webhook reconciliation.
- Staff need a clear operational recovery path via retry, not a full checkout rollback.

**Alternatives considered**:
- Block order creation until GHN confirms shipment: rejected because carrier outages would directly turn into checkout outages.
- Defer shipment creation entirely to an async job: rejected for phase 1 because it adds scheduling complexity and delays customer/admin visibility; immediate backend attempt is simpler and still recoverable.

## Decision 4: Reuse current transaction and idempotency patterns for GHN flows

**Decision**: Reuse `ExecuteInTransactionAsync`, repository upsert patterns, and provider-webhook audit/idempotency patterns already present in the order and Stripe payment flows.

**Rationale**:
- `PlaceOrderCommandHandler` and `StripeCheckoutDraftService` already demonstrate the repository's preferred transaction boundary for order creation and delivery profile persistence.
- `PaymentWebhookController` plus `HandleWebhookEventCommandHandler` already demonstrate how the project handles external callback auditing and duplicate delivery protection.
- Reusing these patterns lowers implementation risk and keeps tests familiar.

**Alternatives considered**:
- Introduce a new event bus or worker-only ingestion design: rejected because it adds infra complexity not required by the current constraints.
- Skip raw webhook audit storage: rejected because the spec requires traceability and safe replay/debug behavior.

## Decision 5: Store GHN master data locally with refreshable cache semantics

**Decision**: Persist province, district, and ward master data in local tables and serve checkout reads from local storage, with refresh operations able to resync from GHN sandbox.

**Rationale**:
- Checkout should not depend on repeated live calls to GHN for static master data.
- Local persistence supports predictable reads, easier validation, and admin/debug tooling.
- This fits the existing PostgreSQL-backed application model better than inventing a new external cache dependency.

**Alternatives considered**:
- Query GHN live on every province/district/ward request: rejected due to latency, instability, and unnecessary provider coupling.
- Keep master data only in in-memory cache: rejected because data should survive restarts and support deterministic validation.

## Decision 6: Introduce package metrics as a backend-owned shipping input snapshot

**Decision**: Calculate package metrics inside the backend using order/cart contents and persist the effective package metrics used for quoting and shipment creation.

**Rationale**:
- Quote validity depends on package metrics and the frontend should not own those calculations.
- Backend-owned package metrics create a single source of truth for requote, retry, and audit scenarios.
- The spec documents package metrics as a key upstream dependency, so the design needs an explicit place for them.

**Alternatives considered**:
- Let the frontend send parcel dimensions: rejected because it would be user-controlled and inconsistent with server-side order truth.
- Hardcode one global parcel size forever: rejected because it weakens quote accuracy and does not scale with varied cart contents.

## Decision 7: Keep payment status, order status, and shipment status separate

**Decision**: Add a dedicated shipment status model that enriches order views without collapsing into either `OrderStatus` or `PaymentStatus`.

**Rationale**:
- The current order aggregate already keeps payment state separate from fulfilment state.
- GHN delivery progress and failure semantics do not align one-to-one with commerce order state.
- This separation prevents webhook updates from accidentally mutating payment outcomes or invalidating existing admin order rules.

**Alternatives considered**:
- Reuse only `OrderStatus`: rejected because shipment transitions such as `PICKING` or `RETURNING` are more granular than the current fulfilment model.
- Mutate payment state from shipment changes: rejected because shipment movement does not imply payment settlement or refund.

## Decision 8: Use explicit public/admin/webhook contracts instead of leaking GHN payloads

**Decision**: Expose Morii-native REST contracts for master data, quote creation, shipment summary reads, admin shipment actions, and webhook ingestion.

**Rationale**:
- The current controller layer already presents stable DTO-based contracts to frontend consumers.
- GHN payloads are inconsistent and can change independently of Morii frontend needs.
- Morii-native contracts make unit tests and future provider migration easier.

**Alternatives considered**:
- Pass through GHN payloads as-is: rejected because it exposes provider quirks and weakens backend ownership.
- Hide shipment completely until production: rejected because the spec explicitly includes customer/admin shipment visibility in phase scope.
