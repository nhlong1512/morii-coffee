# Contract: Admin Reports Dashboard

## Purpose

Return the full admin reports dashboard payload for a selected reporting period in one response.

## Endpoint

- **Method**: `GET`
- **Path**: `/api/v1/admin/reports/dashboard`
- **Auth**: Administrator only

## Query Parameters

| Name | Type | Required | Notes |
|---|---|---|---|
| `preset` | string | no | Supported values are the agreed report presets for phase 1. |
| `from` | date | conditional | Required when the caller requests a custom range. |
| `to` | date | conditional | Required when the caller requests a custom range. |
| `granularity` | string | no | Optional override for bucket size when the requested range allows it. |
| `timezone` | string | no | Optional timezone identifier. Defaults to the configured business/admin timezone. |

## Successful Response

### Envelope

```json
{
  "statusCode": 200,
  "message": "Retrieved successfully",
  "data": {}
}
```

### Data Shape

```json
{
  "range": {
    "from": "2026-05-01",
    "to": "2026-05-31",
    "preset": "30D",
    "granularity": "day",
    "timezone": "Asia/Ho_Chi_Minh",
    "comparisonFrom": "2026-04-01",
    "comparisonTo": "2026-04-30"
  },
  "cards": {
    "totalRevenue": {
      "value": 12500000,
      "previousValue": 11800000,
      "changePercent": 5.93,
      "changeDirection": "up",
      "comparisonSupported": true
    },
    "totalOrders": {
      "value": 420,
      "previousValue": 398,
      "changePercent": 5.53,
      "changeDirection": "up",
      "comparisonSupported": true
    },
    "newUsers": {
      "value": 91,
      "previousValue": 74,
      "changePercent": 22.97,
      "changeDirection": "up",
      "comparisonSupported": true
    },
    "activeProducts": {
      "value": 37,
      "previousValue": null,
      "changePercent": null,
      "changeDirection": null,
      "comparisonSupported": false
    }
  },
  "revenueSeries": {
    "summary": {
      "grossRevenue": 13200000,
      "refundAmount": 700000,
      "netRevenue": 12500000,
      "paidOrders": 401,
      "averageOrderValue": 31172.07,
      "currency": "VND"
    },
    "points": [
      {
        "bucketStart": "2026-05-01",
        "bucketEnd": "2026-05-01",
        "label": "May 1",
        "grossRevenue": 450000,
        "refundAmount": 0,
        "netRevenue": 450000,
        "paidOrders": 14
      }
    ]
  },
  "ordersByStatus": {
    "totalOrders": 420,
    "items": [
      {
        "status": "DELIVERED",
        "count": 250,
        "percentage": 59.52
      }
    ]
  },
  "topProducts": {
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000000",
        "productName": "Iced Americano",
        "thumbnailUrl": "https://cdn.example.com/products/americano.jpg",
        "unitsSold": 120,
        "orderCount": 90,
        "grossRevenue": 4800000
      }
    ]
  },
  "newUsersSeries": {
    "totalNewUsers": 91,
    "points": [
      {
        "bucketStart": "2026-05-01",
        "bucketEnd": "2026-05-01",
        "label": "May 1",
        "users": 4
      }
    ]
  }
}
```

## Contract Rules

- All sections must use the same normalized reporting period returned in `range`.
- `activeProducts` must be returned as snapshot-only in phase 1.
- `topProducts` must expose trustworthy gross sales values only.
- Zero-data periods must still return all sections with zero/empty values instead of omitting sections.

## Error Responses

### Invalid range

- **Status**: `400`
- Returned when the query range is invalid or exceeds the supported limit.

### Unauthorized

- **Status**: `401`
- Returned when the caller is not authenticated.

### Forbidden

- **Status**: `403`
- Returned when the caller is authenticated but is not an administrator.
