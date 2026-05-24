# Contract: Order and Shipment Lifecycle

## Purpose

Describe the public and admin contracts required to create GHN delivery orders, read shipment state, manage shipments, and receive GHN webhook updates.

## Create Order Request Changes

### `POST /api/v1/orders`

#### Request Data Additions

| Field | Type | Notes |
|---|---|---|
| `deliveryMethod` | string | `PICKUP` or `GHN_DELIVERY` |
| `deliveryAddress` | object | Structured address payload |
| `paymentMethod` | string | Existing Morii payment methods |
| `notes` | string? | Optional |
| `saveDeliveryProfile` | boolean | Existing behavior retained |
| `shippingQuoteFingerprint` | string? | Required for GHN delivery |
| `shippingServiceId` | integer? | Selected quote service |

### Stripe Checkout Request Changes

The Stripe payment-first flow must capture the same delivery method, structured address, and shipping quote fields so final order creation preserves identical delivery semantics.

## Order Response Additions

### Order Detail

| Field | Type | Notes |
|---|---|---|
| `deliveryMethod` | string | Fulfilment mode |
| `deliveryAddress` | object? | Structured address snapshot |
| `shipment` | object? | Shipment summary or null |

### Shipment Summary

| Field | Type | Notes |
|---|---|---|
| `provider` | string | `GHN` |
| `environment` | string | `sandbox` |
| `status` | string | Normalized Morii shipment status |
| `statusLabel` | string | Display label |
| `clientOrderCode` | string? | Morii-generated shipment reference |
| `providerOrderCode` | string? | GHN order code |
| `shopId` | integer? | Sandbox shop |
| `serviceId` | integer? | Chosen service |
| `serviceTypeId` | integer? | Optional |
| `feeTotal` | decimal? | Shipment fee snapshot |
| `codAmount` | decimal? | Cash to collect |
| `expectedDeliveryAt` | datetime? | ETA |
| `trackingUrl` | string? | Optional tracking link |
| `lastSyncedAt` | datetime? | Last successful sync |
| `failureReasonCode` | string? | Optional provider reason code |
| `failureReason` | string? | Optional human-readable reason |

## Shipment Read Endpoint

### `GET /api/v1/orders/{id}/shipment`

- Available to order owner or admin.
- Returns `null` for orders that do not require shipment.

## Admin Shipment Actions

### `POST /api/v1/admin/orders/{id}/shipment/create`

#### Response

| Field | Type | Notes |
|---|---|---|
| `shipment` | object | Shipment summary |
| `created` | boolean | Whether a new shipment was created |
| `idempotentReuse` | boolean | Whether an existing shipment was reused |

### `POST /api/v1/admin/orders/{id}/shipment/sync`

#### Response

| Field | Type | Notes |
|---|---|---|
| `shipment` | object | Shipment summary |
| `syncedAt` | datetime | Timestamp of sync |

### `POST /api/v1/admin/orders/{id}/shipment/cancel`

#### Request

| Field | Type | Notes |
|---|---|---|
| `reason` | string? | Optional admin reason |

#### Response

| Field | Type | Notes |
|---|---|---|
| `shipment` | object | Shipment summary |
| `providerAccepted` | boolean | Whether GHN accepted the cancel |

### `POST /api/v1/admin/orders/{id}/shipment/update-note`

#### Request

| Field | Type | Notes |
|---|---|---|
| `note` | string | Required shipment note |

#### Response

| Field | Type | Notes |
|---|---|---|
| `shipment` | object | Updated shipment summary |

## Webhook Contract

### `POST /api/v1/webhooks/ghn/order-status`

### Behavior

- Accept raw GHN webhook payloads.
- Persist raw payload for audit.
- Identify shipment by provider order code or client order code.
- Apply idempotent status mapping updates.
- Return `200 OK` once accepted or safely ignored.

### Operational Rules

- Duplicate deliveries must be safe.
- Stale or out-of-order updates must not blindly regress the local shipment state.
- Shipment updates must not mutate payment outcomes directly.
