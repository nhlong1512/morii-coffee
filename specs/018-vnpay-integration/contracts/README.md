# API Contracts: VNPAY Integration

All authenticated endpoints use the existing Morii `ApiOkResponse` / `ApiCreatedResponse` envelopes. VNPAY callbacks use provider-required bare response semantics.

| File | Method + Path | Auth | Purpose |
|---|---|---|---|
| [create-payment-url.md](./create-payment-url.md) | `POST /api/v1/payments/vnpay/payment-url` | Authenticated customer | Create a pending draft and signed hosted payment URL |
| [callbacks.md](./callbacks.md) | `GET /api/v1/payments/vnpay/ipn` | Anonymous, VNPAY signature | Authoritative idempotent payment update |
| [callbacks.md](./callbacks.md) | `GET /api/v1/payments/vnpay/return` | Anonymous, VNPAY signature | Read-only sanitized browser redirect |
| [reconcile.md](./reconcile.md) | `POST /api/v1/payments/vnpay/reconcile` | Owner or admin | Return local state or reconcile pending state through QueryDR |
| [payment-history-refunds.md](./payment-history-refunds.md) | Existing payment history/refund routes | Owner/admin | Provider-neutral history and provider-routed refunds |

## Shared Rules

- Frontend never sends the amount or customer IP for signing.
- Frontend never receives the VNPAY hash secret.
- `result=success` on browser return is not authoritative payment success.
- VNPAY IPN recognized outcomes return HTTP `200` with exact `RspCode` / `Message` property casing.
- Existing Stripe routes remain backward compatible.
