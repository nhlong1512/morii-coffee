# Quickstart — Stripe Payment Integration

**Feature**: 011-stripe-payment

This is the **engineer onboarding** doc for the feature. It complements the customer-facing user guides in `docs/stripe-integration-guide-{ENG,VN}.md` (which target the project owner / beginner). This file is short, terminal-focused, and assumes you already cloned the repo.

## 1. Prerequisites

- Stripe account (free) — create at <https://dashboard.stripe.com/register>. Stay in **Test mode** (top-right toggle).
- Stripe CLI installed — `brew install stripe/stripe-cli/stripe`.
- The repo running locally per the existing `deploy/run-docker-development.sh`.

## 2. Get your test credentials (one-off)

From the Stripe dashboard (Test mode):

| Stripe location | Env var to set |
|---|---|
| Developers → API keys → Secret key (`sk_test_…`) | `Stripe__SecretKey` |
| Developers → API keys → Publishable key (`pk_test_…`) | `Stripe__PublishableKey` |
| Developers → Webhooks → "Add endpoint" → "Listen to local events" → after `stripe listen` runs, copy `whsec_…` it prints | `Stripe__WebhookSigningSecret` |

Add to your dev environment (e.g., `~/.zshrc`, or `.env` consumed by Docker Compose):

```bash
export Stripe__SecretKey=sk_test_...
export Stripe__PublishableKey=pk_test_...
export Stripe__WebhookSigningSecret=whsec_...
```

> Live keys (`sk_live_…`, `whsec_…` from a Live-mode endpoint) only ever go on the production server, never your laptop or CI.

## 3. Run the backend

```bash
cd deploy && bash run-docker-development.sh
```

The API now listens on `http://localhost:8002`.

## 4. Forward Stripe events to your laptop

In a second terminal:

```bash
stripe listen --forward-to http://localhost:8002/api/v1/payments/webhook
```

The CLI prints a `whsec_…` that you must use as `Stripe__WebhookSigningSecret` (step 2). Restart the API after setting it.

## 5. End-to-end test in 60 seconds

```bash
# 1. Login as a regular customer (any seeded user; see DbMigrator).
curl -s -X POST http://localhost:8002/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"customer1@moriicoffee.com","password":"Customer@123456"}' | jq -r .data.accessToken > /tmp/jwt

# 2. Add a product to the cart (sub a real productId from /api/v1/products).
curl -s -X POST http://localhost:8002/api/v1/cart/items \
  -H "Authorization: Bearer $(cat /tmp/jwt)" \
  -H "Content-Type: application/json" \
  -d '{"productId":"<id>","quantity":1}'

# 3. Place a STRIPE order.
ORDER=$(curl -s -X POST http://localhost:8002/api/v1/orders \
  -H "Authorization: Bearer $(cat /tmp/jwt)" \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Test","phoneNumber":"0900000000","address":"1 Test St","paymentMethod":"STRIPE","saveDeliveryProfile":false}')
ORDER_ID=$(echo $ORDER | jq -r .data.id)

# 4. Create a Checkout Session.
SESSION=$(curl -s -X POST http://localhost:8002/api/v1/payments/checkout-session \
  -H "Authorization: Bearer $(cat /tmp/jwt)" \
  -H "Content-Type: application/json" \
  -d "{\"orderId\":\"$ORDER_ID\"}")
echo "Open this URL in a browser and pay with 4242 4242 4242 4242:"
echo $SESSION | jq -r .data.checkoutUrl
```

After paying, in the terminal running `stripe listen` you should see `checkout.session.completed` → forwarded → `200 OK`. Then:

```bash
curl -s "http://localhost:8002/api/v1/payments/by-order/$ORDER_ID" \
  -H "Authorization: Bearer $(cat /tmp/jwt)" | jq
# → paymentStatus: "Paid", payments[0].status: "Succeeded"
```

## 6. Useful Stripe test cards

| Card | Behaviour |
|---|---|
| `4242 4242 4242 4242` | Always succeeds. |
| `4000 0027 6000 3184` | 3-D Secure challenge — emulates the "browser closes before redirect" edge case. |
| `4000 0000 0000 9995` | Insufficient funds — payment fails. |
| `4000 0000 0000 0341` | Card attached succeeds, later async dispute. |

Any CVV (e.g. `123`) and any future expiry (e.g. `12/34`) work in test mode.

## 7. Replay a webhook (idempotency check)

```bash
stripe events resend evt_xxxxxxxx   # paste an event id from `stripe listen` output
```

The handler must respond `200 OK` and the order state must not change. The audit row in `PaymentWebhookEvents` will show the second event with `ProcessingResult = Duplicate`.

## 8. Issue a refund

```bash
# Login as admin instead.
curl -s -X POST http://localhost:8002/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@moriicoffee.com","password":"Admin@123456"}' | jq -r .data.accessToken > /tmp/admin-jwt

# Full refund:
curl -s -X POST http://localhost:8002/api/v1/payments/$ORDER_ID/refund \
  -H "Authorization: Bearer $(cat /tmp/admin-jwt)" \
  -H "Content-Type: application/json" \
  -d '{"reason":"Customer changed mind"}'
```

Watch `stripe listen` for `charge.refunded`. The order's `paymentStatus` flips to `Refunded`.

## 9. Run the unit-test suite

```bash
dotnet test source/MoriiCoffee.Application.Tests/MoriiCoffee.Application.Tests.csproj
```

All new tests live under `Commands/Payment/` and `Queries/Payment/`. They cover:

- Happy-path session creation
- Reject COD or non-pending order for session creation
- Webhook signature failure
- Webhook duplicate (replay) → idempotent no-op
- Webhook `checkout.session.completed` → state flips
- Webhook `charge.refunded` → cumulative refund math
- Refund amount validation (> remaining balance rejected)
- Admin-only authorisation on refund endpoint

## 10. Build verification

```bash
dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental
```

Must report `0 Warning(s), 0 Error(s)` before the feature is mergeable (per `clean-architecture-skill §9`).

## 11. Go-live checklist (later, when promoting to prod)

1. In the Stripe dashboard, **flip to Live mode** (top-right).
2. Create a new webhook endpoint pointing to `https://<your-prod-domain>/api/v1/payments/webhook` selecting only the four events from `research.md → R-004`.
3. Copy the **live** `sk_live_…` secret key and the live webhook `whsec_…`.
4. Put both values into the production environment variables (NOT into `appsettings.Production.json`).
5. Smoke-test with a $1 / 25 000 VND real charge, then refund yourself to clean up.
6. Confirm `Stripe:IsLiveMode` logs `true` at startup.

That's it. The user-facing beginner guides have screenshots and more narrative — this file is the engineer's terminal cheat-sheet.
