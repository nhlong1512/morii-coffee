# Phase 0 — Research & Decisions

**Feature**: 011-stripe-payment
**Date**: 2026-05-14

Every decision here resolves an unknown raised in `plan.md → Technical Context` or in the feature spec's Assumptions. They are the inputs to Phase 1 (data model, contracts, quickstart).

---

## R-001: SDK choice — `Stripe.net` (official) vs. hand-rolled HTTP

**Decision**: Use the official **`Stripe.net`** NuGet package (latest 47.x as of 2026).

**Rationale**:
- First-party, maintained by Stripe; tracks API versions automatically.
- Built-in support for Checkout Sessions, PaymentIntents, Refunds, and **webhook signature verification** (`EventUtility.ConstructEvent`). Implementing HMAC-SHA256 verification by hand is error-prone.
- Strong typing for Stripe objects — fewer runtime surprises than raw JSON.
- Permissive licence (Apache 2.0), no telemetry beyond Stripe API calls.

**Alternatives considered**:
- *Raw `HttpClient` + manual signature check*: rejected — extra code, no upside, easier to get wrong.
- *Third-party wrappers*: rejected — unmaintained, no advantage.

---

## R-002: Checkout flow style — Stripe-hosted Checkout vs. embedded Payment Element

**Decision**: Use **Stripe-hosted Checkout Sessions** (redirect to `checkout.stripe.com`, back to storefront).

**Rationale**:
- Qualifies the merchant for the lowest PCI-DSS questionnaire (SAQ-A) — no card data ever touches Morii Coffee's servers or front-end.
- One backend endpoint and one redirect; no card-element JS to integrate in the frontend.
- Localized, mobile-optimised, supports 3-D Secure out of the box.
- The feature spec's `A-002` explicitly chose hosted.
- Beginner-friendly: the user (a Stripe newcomer per the request) can ship correctly with far less surface area.

**Alternatives considered**:
- *Embedded Payment Element*: rejected for MVP — requires Stripe.js + custom React work + matching server-side `PaymentIntent` confirm step + more risk for first-time integrators. Open path for a later iteration.
- *Stripe Checkout (Embedded Mode)*: rejected for MVP — usable but requires more frontend integration than the simple redirect.

**Implication for spec**: Frontend changes (new "Pay with Card" button → POST `/payments/checkout-session` → redirect to returned URL) are minimal and documented in `quickstart.md`; the frontend repo's actual edits are out of scope for this backend feature.

---

## R-003: Currency support — VND zero-decimal

**Decision**: Charge in **VND** (Vietnamese đồng) using Stripe's zero-decimal currency convention.

**Rationale**:
- All cart totals in the app are already in VND.
- Stripe lists `vnd` as a zero-decimal currency — the integer amount sent to Stripe equals the đồng amount (no `* 100` multiplication, unlike USD/EUR).
- This avoids a common rounding bug.

**Alternatives considered**:
- *Convert to USD on the fly*: rejected — exposes the customer to FX and adds compliance complexity. Not part of the business request.

**Code consequence**: `StripePaymentGateway.CreateCheckoutSession` will send `unit_amount = (long)order.Total` directly, never multiplied by 100. A unit test pins this.

---

## R-004: Webhook events to subscribe to

**Decision**: Subscribe to and handle exactly four event types in v1:

| Stripe event | Trigger | Effect on Order |
|---|---|---|
| `checkout.session.completed` | Customer successfully completed Checkout (card charged). | `PaymentStatus → Paid`, persist `StripePaymentIntentId` + `StripeChargeId`. |
| `checkout.session.expired` | Customer abandoned/expired Checkout (24 h Stripe default). | `PaymentStatus → Failed` (allows retry/cancel). |
| `payment_intent.payment_failed` | Card declined after Checkout completed (e.g., 3DS reject). | `PaymentStatus → Failed`. |
| `charge.refunded` | Refund settled at Stripe. | `PaymentStatus → Refunded` or `PartiallyRefunded` depending on `amount_refunded`. |

**Rationale**:
- These four cover happy path, abandonment, async failure, and refund — the minimum to keep `PaymentStatus` accurate.
- We do **not** subscribe to `payment_intent.succeeded` because `checkout.session.completed` is the authoritative event for the hosted-Checkout flow (Stripe sends both; subscribing to one only avoids double-handling).

**Alternatives considered**:
- *Subscribe to "all events"*: rejected — noisy, increases the surface area to verify.
- *Subscribe to `payment_intent.succeeded` instead of `checkout.session.completed`*: rejected — for hosted Checkout, the session-completed event arrives with both the session and PI ids and is easier to correlate back to the Order via session metadata.

---

## R-005: Idempotency strategy for webhooks

**Decision**: Persist a `PaymentWebhookEvents` table keyed by Stripe `event.id` (UNIQUE constraint). The webhook handler:

1. Verifies the Stripe signature using `EventUtility.ConstructEvent(rawBody, signatureHeader, signingSecret)`.
2. Attempts to insert a `PaymentWebhookEvent` row with `event.id` and `event.type`. If the insert fails on the UNIQUE constraint, the event has already been processed — return `200 OK` immediately (idempotent no-op).
3. If insert succeeds, processes the event inside the same DB transaction. On success, updates the row's `ProcessingResult = Processed`. On unrecoverable failure, sets `ProcessingResult = Failed` and rethrows so Stripe retries.

**Rationale**:
- Stripe's docs explicitly recommend storing event ids and treating duplicates as no-ops.
- A UNIQUE constraint plus `INSERT … ON CONFLICT DO NOTHING` (Npgsql idiom) gives us atomic idempotency without any application-level locking.
- The audit row also serves diagnostics (FR-019).

**Alternatives considered**:
- *In-memory cache of recent event ids*: rejected — does not survive restarts, vulnerable to multi-instance deployments.
- *Redis SET NX on event id*: rejected — extra dependency for a problem the DB already solves, and we don't have a clear TTL story for the cache.

---

## R-006: Linking a Stripe Checkout Session back to an Order

**Decision**: Pass `OrderId` and `PaymentId` (our internal Payment row id) in the Stripe `metadata` map on the Checkout Session. Also set `client_reference_id = OrderId` for human readability in the Stripe dashboard.

**Rationale**:
- Stripe's `metadata` is delivered untouched on the webhook payload — perfect place to stash internal foreign keys.
- `client_reference_id` shows up in the Stripe dashboard search box — operators can paste an Order id and find the corresponding session.
- The webhook handler reads `event.data.object.metadata["orderId"]` (or the `Payment.Id` for direct lookup) to find the Order without doing a Stripe `Sessions.Get(...)` round-trip.

**Alternatives considered**:
- *Look up by Stripe `payment_intent.id` only*: works but requires a `Sessions.Get(...)` call to first resolve session → PI → order, which costs an extra Stripe API call per event.
- *Store the Stripe session id on `Order` and query by that*: implicitly done already (via `Payment.StripeSessionId`), but `metadata` is the simpler entry point.

---

## R-007: Where to extend the `Order` aggregate

**Decision**: Add one new property `PaymentStatus` (enum `EPaymentStatus`) to `Order` with these transitions:

```
NotRequired (set immediately for COD)
Pending (set immediately for Stripe orders, until webhook arrives)
  ├── Paid (on checkout.session.completed)
  ├── Failed (on checkout.session.expired OR payment_intent.payment_failed)
  └── (no transition out of Pending without webhook)
Paid
  ├── PartiallyRefunded (when refund < remaining balance)
  └── Refunded (when cumulative refund == original total)
PartiallyRefunded
  └── Refunded
Failed
  └── (terminal — customer must cancel order and re-place)
Refunded (terminal)
NotRequired (terminal)
```

Add three domain methods on `Order`:
- `MarkPaymentPaid(string stripePaymentIntentId, string stripeChargeId)` — guards: only allowed when `PaymentStatus == Pending`. Idempotent: a second call with the same PI id is a no-op.
- `MarkPaymentFailed()` — guards: only allowed when `PaymentStatus == Pending`.
- `ApplyRefund(decimal refundedAmount)` — adjusts `PaymentStatus` to `Refunded` or `PartiallyRefunded` based on running total.

Add one guard on fulfilment-status transitions: `Order.Confirm()` throws if `PaymentStatus == Pending` for non-COD orders (FR-013).

**Rationale**: The fulfilment state machine already lives on `Order`; payment state must too because invariants cross both (e.g., can't confirm an unpaid online order). Keeping it on `Order` honours the single-aggregate-root rule.

**Alternatives considered**:
- *Move all payment state to the `Payment` aggregate and leave `Order` agnostic*: rejected — fulfilment decisions need to query payment state, so cross-aggregate references would proliferate. The pragmatic minimal-impact choice is a single field on `Order` that mirrors the latest payment state.

---

## R-008: Refund flow

**Decision**: Admin endpoint `POST /api/v1/payments/{orderId}/refund` accepts `{ amount?, reason? }`.

- If `amount` is null/zero → full refund of remaining unrefunded balance.
- If `amount` > remaining balance → 400 BadRequest.
- Calls `IPaymentGateway.RefundAsync(stripePaymentIntentId, amount)`.
- Stripe returns a `Refund` object → persist a `RefundRecord` row immediately (`Pending`).
- The actual `PaymentStatus` update happens later when the `charge.refunded` webhook arrives. This ensures the system's source of truth is always Stripe, not optimistic local state.

**Rationale**: Matches Stripe's async refund semantics. The admin sees "Refund initiated" → Stripe webhook arrives within seconds → final status flips. SC-006's 30-second budget is comfortable.

**Alternatives considered**:
- *Mark `Refunded` immediately in the request handler*: rejected — Stripe may reverse a refund (rare but possible). Webhook is authoritative.

---

## R-009: Configuration secrets — where they live

**Decision**: Add a `Stripe` section to `appsettings.json` with **placeholder empty strings** (so the section is discoverable in code) and **real values supplied via environment variables** at runtime (ASP.NET Core's default env-var binding maps `Stripe__SecretKey` → `Stripe:SecretKey`).

| Setting | Source |
|---|---|
| `Stripe:SecretKey` | env var `Stripe__SecretKey` (e.g., `sk_test_…` in dev, `sk_live_…` in prod) |
| `Stripe:PublishableKey` | env var `Stripe__PublishableKey` |
| `Stripe:WebhookSigningSecret` | env var `Stripe__WebhookSigningSecret` (e.g., `whsec_…`) |
| `Stripe:Currency` | static `"vnd"` in appsettings.json |
| `Stripe:SuccessUrl` | composed from `EmailSettings:StorefrontUrl` + `/checkout/success?session_id={CHECKOUT_SESSION_ID}` |
| `Stripe:CancelUrl` | composed from `EmailSettings:StorefrontUrl` + `/checkout/cancel` |
| `Stripe:IsLiveMode` | derived at startup: `true` if `SecretKey.StartsWith("sk_live_")` else `false`; logged at startup |

**Rationale**:
- Keeping placeholders in `appsettings.json` makes the section schema visible without leaking secrets.
- Environment variables match the existing pattern for other secrets in this repo (e.g., `Brevo:ApiKey` is currently in the file but should be moved the same way — out of scope for this feature, but the new section sets the right precedent).
- `IsLiveMode` derivation prevents the "live secret + test publishable" mismatch (FR-020 / Edge case "Test vs live mode confusion").

**Alternatives considered**:
- *AWS Secrets Manager / Azure Key Vault*: out of scope for this feature; the env-var pattern is the project's current baseline.
- *.NET User Secrets in dev*: viable for local dev, documented as an option in `quickstart.md`. Not enforced.

---

## R-010: Webhook endpoint security beyond signature verification

**Decision**: The webhook endpoint is **anonymous** (no JWT — Stripe doesn't authenticate with bearer tokens), but:
- Rejects any request whose `Stripe-Signature` header fails `EventUtility.ConstructEvent`.
- Reads the **raw** request body (not JSON-deserialised by ASP.NET) — Stripe signs the raw bytes; any reformatting breaks signature verification. This requires `[DisableRequestSizeLimit]`-style raw-read handling on the action (see `contracts/webhook.md` for the controller skeleton).
- Logs every rejected request with the signature header truncated for forensic review.

**Rationale**: Stripe's documented and only recommended authentication mechanism for webhooks.

**Alternatives considered**:
- *IP allowlist for Stripe's known IPs*: viable defence-in-depth, but Stripe's IPs change without notice. Signature verification is the primary control.

---

## R-011: Testing strategy for Stripe integration

**Decision**: Three layers of tests:

1. **Unit tests** (this feature ships all of these): cover every command/query handler with `Mock<IPaymentGateway>` and `Mock<IUnitOfWork>` — these prove the **business rules** (idempotency, state-transition guards, refund-amount validation, signature-failure handling).
2. **Quickstart manual test** (documented but not automated): use Stripe CLI (`stripe listen`) + test card `4242 4242 4242 4242` to exercise the real Stripe API end-to-end. Documented in `quickstart.md`.
3. **Webhook replay test** (unit, simulated): a unit test that calls the webhook handler twice with the same simulated `Event` object and asserts the second call is an idempotent no-op (SC-004).

Out of scope for this feature: integration tests that boot the whole app and hit a Stripe sandbox over the network. Those belong to a CI test ring we don't currently run.

**Rationale**: The constitution says "verification before done"; unit tests on every code path satisfy SC-009 while integration tests would require a real Stripe sandbox key in CI (which we don't have yet).

---

## R-012: Where the `_orderId` lookup lives during webhook handling

**Decision**: The webhook handler reads `metadata["orderId"]` from the Stripe event payload, parses it as a `Guid`, and queries `IPaymentRepository.GetBySessionIdAsync(sessionId)` to load both the `Payment` row and (via include) its linked `Order`. The lookup uses `Payment.StripeSessionId` as the natural join key, NOT raw `OrderId`, because one Order may have several Payment rows if earlier attempts failed.

**Rationale**: Multiple payment attempts per order are normal (failed card → retry with another card). The Payment row created at session-creation time is the authoritative pointer back to which order and which attempt this webhook belongs to. Using `metadata["paymentId"]` (our internal id) makes the lookup explicit and 1:1.

---

## R-013: Logging volume + PII

**Decision**: Log at `Information` level: payment attempt created, webhook received (event type + event id), webhook processed (result), refund issued. Log at `Warning`: signature verification failed, unknown event type, order-not-found for an event. Log at `Error`: handler threw an unexpected exception. **Never** log raw event bodies, customer card data, or email addresses inside payment logs.

**Rationale**: Aligns with FR-019 (operability) and FR-004/SC-005 (no card data leaks).

---

## Open questions explicitly resolved here

| Spec ambiguity | Resolution |
|---|---|
| "payment provider" name | **Stripe** (confirmed in the user request and feature title). |
| "out-of-band notifications" mechanism | **Stripe webhooks** with HMAC-SHA256 signature verification via `Stripe.net` `EventUtility`. |
| Refund initiation party | **Admin only** (per A-006 and FR-017). |
| Idempotency token | **`event.id`** from Stripe, persisted in `PaymentWebhookEvents.StripeEventId` UNIQUE. |
| Currency conversion | **None** — single-currency VND, zero-decimal. |

---

## Summary

All NEEDS-CLARIFICATION items resolved. Phase 1 (data-model, contracts, quickstart) proceeds from these decisions.
