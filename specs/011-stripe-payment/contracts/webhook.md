# `POST /api/v1/payments/webhook`

**Auth**: **Anonymous**. Authentication is performed via Stripe's HMAC-SHA256 signature in the `Stripe-Signature` header — verified with `Stripe.net`'s `EventUtility.ConstructEvent`.

**Purpose**: Receive out-of-band Stripe events that update payment state on the corresponding Order.

## Request

```http
POST /api/v1/payments/webhook HTTP/1.1
Content-Type: application/json
Stripe-Signature: t=1631234567,v1=<hex>,v0=<hex>

{
  "id": "evt_1Mw...",
  "object": "event",
  "type": "checkout.session.completed",
  "data": { "object": { ... full Stripe Session object ... } }
}
```

The body is **JSON** but the action **must** read the **raw bytes** (not the JSON-deserialised model) because Stripe signs the bytes verbatim. Re-serialisation by ASP.NET would break the signature.

### Controller-level constraints

- `[AllowAnonymous]`
- `[ApiController]`
- `[Consumes("application/json")]`
- The action must use `using var reader = new StreamReader(Request.Body); var rawBody = await reader.ReadToEndAsync();` to grab raw text.
- Must accept large bodies (Stripe sometimes sends > 32 KB for event types with many line items). Configure `[RequestSizeLimit(64_000)]` to be safe.

## Responses

| HTTP | When | Body | Stripe behaviour |
|---|---|---|---|
| 200 | Event processed, OR duplicate (idempotent no-op), OR unknown event type (we ignore politely), OR order not found (we log and accept). | `""` (empty body or `{ "received": true }`) | Stripe stops retrying. |
| 400 | Body is unparseable as a Stripe event (malformed JSON, missing fields). | `{ "received": false, "reason": "malformed" }` | Stripe retries with backoff. |
| 422 | Signature header missing OR `EventUtility.ConstructEvent` throws. | `{ "received": false, "reason": "signature_invalid" }` | Stripe retries (rare — usually a misconfiguration). |
| 500 | Unhandled server exception during processing. | standard envelope | Stripe retries. |

> Returning `200` for "unknown event type" prevents Stripe from spam-retrying events we don't care about. The audit row is still written with `ProcessingResult = UnhandledEventType` so an operator can see what we ignored.

## Idempotency contract

The handler MUST process the same `event.id` at most once. Implementation:

1. Verify signature → if fails, return 422 and write an audit row with `ProcessingResult = SignatureInvalid`.
2. Compute `payloadFingerprint = sha256(rawBody)`.
3. Attempt `INSERT INTO PaymentWebhookEvents (..., StripeEventId, ...) VALUES (..., @id, ...)`. The UNIQUE constraint on `StripeEventId` is the lock.
4. If `INSERT` fails on the unique constraint:
   - Look up the existing row.
   - If `ProcessingResult == Processed`, return 200 immediately (true duplicate).
   - If `ProcessingResult == Failed`, retry processing (this is the second-chance path).
5. If `INSERT` succeeds, dispatch by `event.type`:
   - `checkout.session.completed` → `Payment.MarkSucceeded(pi, charge)` + `Order.MarkPaymentPaid(pi, charge)`.
   - `checkout.session.expired` → `Payment.MarkExpired()` + `Order.MarkPaymentFailed()`.
   - `payment_intent.payment_failed` → `Payment.MarkFailed(reason)` + `Order.MarkPaymentFailed()` (only if not already in a terminal state).
   - `charge.refunded` → for each `RefundRecord` matching `event.data.object.refunds[*].id`, call `MarkSucceeded()`; then `Order.ApplyRefund(cumulative)`.
   - any other event type → log + audit `UnhandledEventType`.
6. On success: update the audit row to `ProcessingResult = Processed`, set `ProcessedAt = utcNow`.
7. On exception inside step 5: update audit row to `ProcessingResult = Failed` + `ErrorMessage`, then rethrow so the HTTP response is 500 and Stripe retries.

## Domain rules enforced

- Order must exist → if not, audit `OrderNotFound`, return 200 (we don't want Stripe to retry forever).
- `Order.MarkPaymentPaid` is idempotent at the aggregate level — calling it twice with the same `paymentIntentId` is a no-op. The webhook layer's idempotency is the primary defence; the domain-level idempotency is belt-and-braces.
- Refund webhook must reconcile against the running cumulative refunded amount on the `Payment` aggregate.

## Stripe SDK call (signature verification)

```csharp
Event stripeEvent;
try
{
    stripeEvent = EventUtility.ConstructEvent(rawBody, signatureHeader, settings.WebhookSigningSecret);
}
catch (StripeException)
{
    // signature invalid — audit + 422
}
```
