# Data Model: GHN Sandbox Integration

## 1. DeliveryAddressSnapshot

Represents the structured delivery address stored on a checkout draft, saved delivery profile, and final order snapshot.

### Fields

| Field | Type | Notes |
|---|---|---|
| `fullName` | string | Required, max 100 |
| `phoneNumber` | string | Required, max 15 |
| `addressLine` | string | Required, max 500 |
| `provinceId` | integer | Required for GHN delivery |
| `provinceName` | string | Required for GHN delivery |
| `districtId` | integer | Required for GHN delivery |
| `districtName` | string | Required for GHN delivery |
| `wardCode` | string | Required for GHN delivery |
| `wardName` | string | Required for GHN delivery |

### Validation Rules

- Required for `GHN_DELIVERY` orders and checkout quotes.
- Pickup orders may omit administrative code fields but still keep recipient snapshot fields if needed by existing flows.
- `addressLine` stores the local street/building detail while province/district/ward fields provide carrier routing precision.

### Relationships

- Embedded into `Order`.
- Reused by `UserDeliveryProfile`.
- Referenced by quote creation and shipment creation requests.

## 2. UserDeliveryProfile

Extends the existing saved delivery profile so future checkout flows can prefill structured shipping data.

### Fields

| Field | Type | Notes |
|---|---|---|
| `userId` | guid | Primary key, one profile per user |
| `fullName` | string | Required |
| `phoneNumber` | string | Required |
| `addressLine` | string | Required |
| `provinceId` | integer? | Nullable until profile is upgraded |
| `provinceName` | string? | Nullable until profile is upgraded |
| `districtId` | integer? | Nullable until profile is upgraded |
| `districtName` | string? | Nullable until profile is upgraded |
| `wardCode` | string? | Nullable until profile is upgraded |
| `wardName` | string? | Nullable until profile is upgraded |
| `updatedAt` | datetime | Existing entity tracking |

### Validation Rules

- Saving a profile from a GHN delivery checkout must persist all structured fields.
- Legacy flat profiles remain readable but must be requoted or completed before GHN delivery can proceed.

## 3. ShippingQuoteSnapshot

A normalized quote result returned by the backend and persisted on order creation for GHN delivery.

### Fields

| Field | Type | Notes |
|---|---|---|
| `quoteFingerprint` | string | Required, idempotent quote reference |
| `provider` | enum | `GHN` |
| `environment` | enum | `Sandbox` |
| `deliveryMethod` | enum | `GHN_DELIVERY` |
| `serviceId` | integer | Required |
| `serviceTypeId` | integer? | Optional GHN service type |
| `serviceLabel` | string | Display label used by frontend |
| `feeTotal` | decimal | Required |
| `feeBreakdown` | json/value object | Main fee plus provider-specific raw details |
| `estimatedDeliveryAt` | datetime? | Optional |
| `quoteExpiresAt` | datetime | Required |
| `packageMetrics` | value object | Required for audit and requote |

### Validation Rules

- Must be regenerated if the cart, delivery route, chosen service, or expiry window changes.
- Must be persisted to the order when an order is created from a valid GHN quote.

## 4. PackageMetrics

Backend-owned parcel sizing information used for quote and shipment creation.

### Fields

| Field | Type | Notes |
|---|---|---|
| `totalWeightGrams` | integer | Required, > 0 |
| `lengthCm` | integer | Required, > 0 |
| `widthCm` | integer | Required, > 0 |
| `heightCm` | integer | Required, > 0 |
| `insuranceValue` | decimal | Optional or derived |
| `itemCount` | integer | Required, > 0 |

### Validation Rules

- Derived by backend from cart or order items.
- Used consistently for quote, requote, and shipment creation.

## 5. Shipment

Represents the carrier-side fulfilment record linked to a Morii order.

### Fields

| Field | Type | Notes |
|---|---|---|
| `id` | guid | Primary key |
| `orderId` | guid | Unique one-to-one link to order |
| `provider` | enum | `GHN` |
| `providerEnvironment` | enum | `Sandbox` |
| `status` | enum | Morii shipment status |
| `statusLabel` | string | Display-friendly status text |
| `clientOrderCode` | string | Morii-generated idempotency key |
| `providerOrderCode` | string? | GHN order code after creation |
| `shopId` | integer? | GHN sandbox shop |
| `serviceId` | integer? | Chosen service |
| `serviceTypeId` | integer? | Optional |
| `codAmount` | decimal | `0` for prepaid online orders |
| `feeTotal` | decimal? | Final provider fee snapshot |
| `expectedDeliveryAt` | datetime? | Provider ETA |
| `trackingUrl` | string? | Optional customer/admin link |
| `failureReasonCode` | string? | Provider failure/cancel code |
| `failureReason` | string? | Human-readable summary |
| `lastRawDetailPayload` | json? | Last synced provider detail summary |
| `lastSyncedAt` | datetime? | Last successful sync |
| `createdAt` | datetime | Audit |
| `updatedAt` | datetime | Audit |

### State Transitions

| From | To | Notes |
|---|---|---|
| `QUOTE_PENDING` | `QUOTED` | Valid quote returned |
| `QUOTED` | `CREATE_PENDING` | Order accepted and shipment creation started |
| `CREATE_PENDING` | `CREATED` | Provider accepted create request |
| `CREATE_PENDING` | `FAILED_TO_CREATE` | Provider creation failed |
| `CREATED` | `READY_TO_PICK` / `PICKING` / `PICKED` / `STORING` / `TRANSPORTING` / `SORTING` / `DELIVERING` | GHN progress states |
| any active state | `CANCELLED` | Cancel accepted |
| `DELIVERING` or later provider failure | `DELIVERY_FAILED` | Delivery failure |
| failure or return flow | `RETURNING` | Return in motion |
| `RETURNING` | `RETURNED` | Return completed |
| any stale state | `SYNC_ERROR` | Manual or automatic sync failed without changing business truth |

### Validation Rules

- At most one shipment per order.
- Duplicate create attempts must reuse the existing shipment or no-op safely.
- Shipment updates cannot mutate payment state directly.

## 6. ShipmentWebhookEvent

Audit row for GHN callback deliveries and manual sync traces.

### Fields

| Field | Type | Notes |
|---|---|---|
| `id` | guid | Primary key |
| `provider` | enum | `GHN` |
| `providerEventId` | string? | Optional if GHN provides stable event id |
| `providerOrderCode` | string? | Lookup key |
| `clientOrderCode` | string? | Lookup key |
| `eventType` | string | Raw callback event name or synthetic sync event |
| `rawPayload` | string/json | Required for audit |
| `signatureVerified` | boolean | If webhook authenticity is supported |
| `processingResult` | enum/string | Processed, duplicate, ignored, failed |
| `receivedAt` | datetime | Audit |
| `processedAt` | datetime? | Audit |

### Validation Rules

- Duplicate or repeated provider deliveries must be safe.
- Out-of-order events must not blindly regress the local shipment state.

## 7. ShippingMasterData

Normalized local cache of GHN administrative address data.

### Province

| Field | Type | Notes |
|---|---|---|
| `provinceId` | integer | Primary key from GHN |
| `provinceName` | string | Required |
| `code` | string? | Optional provider code |
| `isActive` | boolean | Optional local flag |
| `lastSyncedAt` | datetime | Audit |

### District

| Field | Type | Notes |
|---|---|---|
| `districtId` | integer | Primary key from GHN |
| `provinceId` | integer | Parent link |
| `districtName` | string | Required |
| `supportType` | integer? | Optional provider metadata |
| `isActive` | boolean | Optional local flag |
| `lastSyncedAt` | datetime | Audit |

### Ward

| Field | Type | Notes |
|---|---|---|
| `wardCode` | string | Primary key from GHN |
| `districtId` | integer | Parent link |
| `wardName` | string | Required |
| `isActive` | boolean | Optional local flag |
| `lastSyncedAt` | datetime | Audit |

### Relationships

- Province 1:N District
- District 1:N Ward
- Delivery address snapshot references one province, district, and ward logically by stored ids/codes
