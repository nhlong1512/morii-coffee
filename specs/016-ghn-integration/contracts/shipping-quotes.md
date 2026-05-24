# Contract: Shipping Quotes

## Purpose

Allow checkout and admin operations to request normalized GHN shipping quotes and requotes.

## Shared Types

### Shipping Address

| Field | Type | Notes |
|---|---|---|
| `fullName` | string | Required |
| `phoneNumber` | string | Required |
| `addressLine` | string | Required |
| `provinceId` | integer | Required for GHN delivery |
| `provinceName` | string | Required for GHN delivery |
| `districtId` | integer | Required for GHN delivery |
| `districtName` | string | Required for GHN delivery |
| `wardCode` | string | Required for GHN delivery |
| `wardName` | string | Required for GHN delivery |

### Package Metrics

| Field | Type | Notes |
|---|---|---|
| `totalWeightGrams` | integer | Backend-derived |
| `lengthCm` | integer | Backend-derived |
| `widthCm` | integer | Backend-derived |
| `heightCm` | integer | Backend-derived |
| `insuranceValue` | decimal | Backend-derived |
| `itemCount` | integer | Backend-derived |

### Shipping Service Option

| Field | Type | Notes |
|---|---|---|
| `serviceId` | integer | Required |
| `serviceTypeId` | integer? | Optional |
| `shortName` | string | Display label |
| `displayName` | string | Display label |
| `estimatedLeadTime` | datetime? | Optional |
| `fee` | decimal? | Optional per service |
| `isRecommended` | boolean | Backend recommendation flag |

### Shipping Quote Response

| Field | Type | Notes |
|---|---|---|
| `provider` | string | `GHN` |
| `environment` | string | `sandbox` |
| `address` | object | Structured shipping address |
| `packageMetrics` | object | Backend package snapshot |
| `service` | object | Selected service |
| `availableServices` | array | Valid service choices |
| `feeBreakdown` | object | Normalized fee detail |
| `leadTime` | object | Optional ETA payload |
| `quoteExpiresAt` | datetime | Quote validity deadline |
| `quoteFingerprint` | string | Required for order placement |

## `POST /api/v1/shipping/quotes`

### Request

| Field | Type | Notes |
|---|---|---|
| `deliveryMethod` | string | `PICKUP` or `GHN_DELIVERY` |
| `paymentMethod` | string | Existing Morii payment methods |
| `address` | object | Structured address |
| `selectedServiceId` | integer? | Optional caller preference |

### Behavior

- If `deliveryMethod = PICKUP`, the endpoint returns no carrier quote payload.
- If the cart is empty, the request is rejected.
- If the route is unsupported or GHN quote fails, the request returns a typed validation/business error.

## `POST /api/v1/admin/orders/{id}/shipment/requote`

### Purpose

Refresh the quote for an existing order using the latest address and package rules.

### Response

| Field | Type | Notes |
|---|---|---|
| `quote` | object | Shipping quote response |
| `shipment` | object? | Existing shipment summary if one exists |
