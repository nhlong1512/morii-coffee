# Quickstart: GHN Sandbox Integration

## Goal

Verify the GHN sandbox delivery flow end-to-end in the Morii backend before implementation is considered complete.

## Preconditions

- Application configuration contains GHN sandbox credentials and shop/store configuration.
- `Ghn:SkipTlsCertificateValidation` may be `true` in local development only when the host cannot validate the GHN sandbox certificate chain; keep it `false` outside development.
- Database migrations for structured delivery data, shipments, master data, and webhook audit tables have been applied.
- At least one user, cart, and eligible delivery product set exists in the local environment.
- GHN master data has been synchronized at least once.

## Verification Steps

### 1. Build and automated checks

1. Run the backend build.
2. Run the application test suite that covers:
   - structured delivery profile validation
   - quote creation and expiry validation
   - order placement with GHN delivery
   - shipment creation retry/idempotency
   - webhook/manual sync status updates
   - admin authorization for shipment operations

### 2. Master data verification

1. Request the province list endpoint.
2. Request districts for a valid province.
3. Request wards for a valid district.
4. Confirm responses are non-empty, stable, and use Morii-native DTOs instead of raw GHN payloads.

### 3. Quote verification

1. Authenticate as a customer with a populated cart.
2. Create a GHN delivery quote using a valid structured address.
3. Confirm the response includes:
   - provider/environment metadata
   - at least one service option
   - total fee and fee breakdown
   - estimated delivery time
   - quote expiry and quote fingerprint
4. Repeat with an invalid or unsupported route and confirm the request is rejected with a user-actionable error.

### 4. Order placement verification

1. Place a COD order using a valid GHN quote.
2. Confirm the created order stores:
   - delivery method
   - structured delivery snapshot
   - selected service
   - shipping fee
   - shipment summary or `FAILED_TO_CREATE`
3. Place a Stripe checkout flow using the payment-first path and confirm the final order preserves the same structured delivery/shipping semantics.

### 5. Shipment lifecycle verification

1. For an order with successful automatic shipment creation, fetch the shipment summary and confirm provider codes and initial status are present.
2. For an order forced into shipment creation failure, use the admin retry endpoint and confirm:
   - the retry succeeds or reuses the existing shipment
   - no duplicate shipment is created
3. Use the admin requote and sync endpoints and confirm shipment/order views reflect the latest accepted state.
4. If GHN allows the current state to be cancelled, use the admin cancel endpoint and confirm the shipment status changes without altering payment state incorrectly.

### 5a. Live sandbox evidence captured on 2026-05-24

1. `available-services` returned `service_id = 53320` for route `1461 -> 1461`.
2. `fee` returned a successful quote with `total = 20900`.
3. A sandbox shipment was created successfully and returned a provider order code.
4. `detail` confirmed the created shipment entered `ready_to_pick`.
5. `update` succeeded for shipment note changes.
6. `cancel` succeeded and a final `detail` read confirmed provider status `cancel`.

This evidence validates the current GHN sandbox shop/origin configuration and proves the provider-side lifecycle used by the backend gateway is reachable from the current environment.

### 6. Webhook verification

1. Deliver a representative GHN webhook payload to the webhook endpoint.
2. Confirm the request is audited and linked to the correct shipment.
3. Deliver the same payload again and confirm the duplicate is handled safely.
4. Deliver an out-of-order or stale payload and confirm the shipment does not regress improperly.

### 7. Read model verification

1. Fetch customer order detail for:
   - a pickup order
   - a GHN delivery order without shipment
   - a GHN delivery order with active shipment
2. Confirm each response exposes the expected shipment summary semantics for that case.
3. Fetch the admin order view and confirm shipment actions are visible only to admins.

## Evidence Checklist

- Build succeeds.
- Relevant automated tests pass.
- Quote flow verified for both success and failure cases.
- Order placement verified for COD and Stripe-compatible delivery flows.
- Shipment retry and sync verified without duplicates.
- Webhook audit and idempotency verified.
- Customer/admin order reads verified.
