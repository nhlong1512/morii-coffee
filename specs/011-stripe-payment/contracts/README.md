# API Contracts — Stripe Payment

**Feature**: 011-stripe-payment

Every new HTTP endpoint exposed by this feature, documented in the same format that the rest of the Morii Coffee API uses (Swashbuckle XML comments + `ApiOkResponse` / `ApiCreatedResponse` envelopes).

| File | Method + Path | Auth | Purpose |
|---|---|---|---|
| [create-checkout-session.md](./create-checkout-session.md) | `POST /api/v1/payments/checkout-session` | Authenticated customer | Customer initiates a Stripe-hosted checkout for an existing pending-payment order. |
| [webhook.md](./webhook.md) | `POST /api/v1/payments/webhook` | **Anonymous** (Stripe signature) | Receives Stripe webhook events; idempotent. |
| [get-payment-by-order.md](./get-payment-by-order.md) | `GET /api/v1/payments/by-order/{orderId}` | Authenticated customer (owner) or admin | Fetch payment + refund history for an order. |
| [refund.md](./refund.md) | `POST /api/v1/payments/{orderId}/refund` | Admin only | Issue a full or partial refund. |

## Response envelopes

All controller responses (except the webhook endpoint, which returns a bare `200 OK` to satisfy Stripe) use the existing project envelopes:

- `200 OK` → `ApiOkResponse(data)` — `{ "statusCode": 200, "message": "Retrieved successfully", "data": { ... } }`
- `201 Created` → `ApiCreatedResponse(data)`
- `4xx/5xx` → `ApiErrorResponse(message)` via the existing exception middleware.

## Error codes used by these endpoints

| HTTP | Meaning |
|---|---|
| 400 | Validation failure (bad amount, order in wrong state, etc.) |
| 401 | Not authenticated |
| 403 | Authenticated but not authorised (e.g., customer trying to call refund) |
| 404 | Order or payment not found, or not owned by caller |
| 409 | Idempotency conflict (rare — only when the same checkout session is created twice in a tiny window) |
| 422 | Webhook signature invalid |
| 500 | Stripe API call failed for an unexpected reason; client should retry |

Webhook endpoint behaves differently: it returns `200 OK` even for duplicate / unknown-order events so that Stripe stops retrying. Only a true server bug returns `500` to trigger Stripe retry.
