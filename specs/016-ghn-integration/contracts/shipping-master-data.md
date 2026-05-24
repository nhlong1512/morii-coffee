# Contract: Shipping Master Data

## Purpose

Provide structured province, district, and ward data for GHN delivery checkout flows.

## Shared Response Shape

All endpoints return the repository's standard API envelope with a typed data payload.

## `GET /api/v1/shipping/ghn/provinces`

### Response Data

| Field | Type | Notes |
|---|---|---|
| `provinceId` | integer | GHN province identifier |
| `provinceName` | string | Display name |
| `code` | string? | Optional provider code |

## `GET /api/v1/shipping/ghn/districts?provinceId={provinceId}`

### Request Rules

- `provinceId` is required.
- Unknown or unsupported province ids return a validation error.

### Response Data

| Field | Type | Notes |
|---|---|---|
| `districtId` | integer | GHN district identifier |
| `provinceId` | integer | Parent province identifier |
| `districtName` | string | Display name |
| `supportType` | integer? | Optional provider routing metadata |

## `GET /api/v1/shipping/ghn/wards?districtId={districtId}`

### Request Rules

- `districtId` is required.
- Unknown or unsupported district ids return a validation error.

### Response Data

| Field | Type | Notes |
|---|---|---|
| `wardCode` | string | GHN ward code |
| `districtId` | integer | Parent district identifier |
| `wardName` | string | Display name |

## Behavioral Notes

- Responses come from Morii-managed local master data, not raw pass-through GHN responses.
- Response ordering should be stable for deterministic frontend selection behavior.
