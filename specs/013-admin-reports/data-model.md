# Phase 1 — Data Model

**Feature**: 013-admin-reports  
**Date**: 2026-05-22

This feature is read-only in phase 1. The data model below describes the reporting view model and the existing operational entities it depends on. No new persistence tables are required for the first release.

---

## Reporting view overview

```text
ReportingPeriod
├── SummaryMetricCard[]
├── RevenueSeries
│   └── RevenuePoint[]
├── OrderStatusBreakdown
│   └── OrderStatusBreakdownItem[]
├── TopProducts
│   └── TopProduct[]
└── NewUsersSeries
    └── NewUserPoint[]

Operational sources
├── Users
├── Products
├── Orders
├── OrderItems
├── Payments
└── Refunds
```

---

## 1. ReportingPeriod

Represents the normalized reporting window requested by the admin and the matching comparison window.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `Preset` | enum/string | no | Requested preset such as 7-day, 30-day, 90-day, annual, or custom. |
| `From` | date | yes | Inclusive start date of the current reporting window. |
| `To` | date | yes | Inclusive end date of the current reporting window. |
| `Granularity` | enum/string | yes | Bucket size used for time-series sections. |
| `Timezone` | string | yes | Timezone used to derive day/week/month buckets. |
| `ComparisonFrom` | date | yes | Inclusive start date of the immediately preceding comparison window. |
| `ComparisonTo` | date | yes | Inclusive end date of the immediately preceding comparison window. |

### Validation rules

- `From` must be on or before `To`.
- Custom ranges must stay within the maximum supported reporting window.
- `Granularity` must be normalized to a supported value before downstream calculations run.

---

## 2. SummaryMetricCard

Represents one KPI shown in the dashboard header area.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `MetricKey` | string | yes | Stable business identifier such as total revenue or total orders. |
| `CurrentValue` | decimal/integer | yes | Value for the selected reporting period or current snapshot. |
| `PreviousValue` | decimal/integer/null | no | Value for the comparison period when comparison is supported. |
| `ChangePercent` | decimal/null | no | Percentage delta when comparison is supported and meaningful. |
| `ChangeDirection` | enum/string/null | no | Up, down, flat, or special zero-baseline state. |
| `ComparisonSupported` | boolean | yes | Indicates whether the metric should display period-over-period comparison. |

### Special rule

- `activeProducts` is the only phase-1 summary metric where `ComparisonSupported = false`.

---

## 3. RevenueSeries

Represents the retained-revenue trend for the reporting period.

### Summary fields

| Field | Type | Required | Description |
|---|---|---|---|
| `GrossRevenue` | decimal | yes | Sum of completed payment amounts in the selected reporting period. |
| `RefundAmount` | decimal | yes | Sum of completed refund amounts in the selected reporting period. |
| `NetRevenue` | decimal | yes | Gross revenue minus refund amount. |
| `PaidOrders` | integer | yes | Count of paid orders contributing to gross revenue. |
| `AverageOrderValue` | decimal | yes | Average retained or paid order amount according to final query semantics. |
| `Currency` | string | yes | Business currency for display. |

### RevenuePoint

| Field | Type | Required | Description |
|---|---|---|---|
| `BucketStart` | datetime/date | yes | Start of the bucket. |
| `BucketEnd` | datetime/date | yes | End of the bucket. |
| `Label` | string | yes | Human-readable label for charts and exports. |
| `GrossRevenue` | decimal | yes | Completed payments recognized in this bucket. |
| `RefundAmount` | decimal | yes | Completed refunds recognized in this bucket. |
| `NetRevenue` | decimal | yes | Bucket gross minus refunds. |
| `PaidOrders` | integer | yes | Count of paid orders represented in the bucket. |

### Source relationships

- `Payments` contribute to gross revenue only when the payment attempt is completed successfully.
- `Refunds` contribute to refund amount only when the refund is completed successfully.

---

## 4. OrderStatusBreakdown

Represents the current-status mix for orders created during the selected period.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `TotalOrders` | integer | yes | Total orders created during the selected period. |
| `Items` | collection | yes | Breakdown rows grouped by current order status. |

### OrderStatusBreakdownItem

| Field | Type | Required | Description |
|---|---|---|---|
| `Status` | enum/string | yes | Current order fulfillment state. |
| `Count` | integer | yes | Number of orders currently in that state. |
| `Percentage` | decimal | yes | Share of total period orders represented by that status. |

### Source relationships

- Derived from `Orders` created during the selected period.
- Uses each order's current/latest status only.

---

## 5. TopProduct

Represents one ranked product in the top-selling list.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `ProductId` | identifier | yes | Product reference for drill-in or display linking. |
| `ProductName` | string | yes | Product name captured for display. |
| `ThumbnailUrl` | string/null | no | Optional product image for the admin UI. |
| `UnitsSold` | integer | yes | Sum of sold units in the selected period. |
| `OrderCount` | integer | yes | Number of distinct orders contributing to sales. |
| `GrossRevenue` | decimal | yes | Sum of item line totals contributing to the ranking. |

### Validation / business rules

- Rankings are based on trustworthy sold-item data only.
- Item-level net revenue is intentionally excluded from phase 1.

### Source relationships

- Derived from `OrderItems` joined back to their parent `Orders`.
- Uses `Products` only for current display metadata such as thumbnail URL.

---

## 6. NewUsersSeries

Represents user registration growth across the reporting period.

### Summary fields

| Field | Type | Required | Description |
|---|---|---|---|
| `TotalNewUsers` | integer | yes | Total users created during the reporting period. |
| `Points` | collection | yes | Time-bucketed registration points. |

### NewUserPoint

| Field | Type | Required | Description |
|---|---|---|---|
| `BucketStart` | datetime/date | yes | Start of the bucket. |
| `BucketEnd` | datetime/date | yes | End of the bucket. |
| `Label` | string | yes | Human-readable bucket label. |
| `Users` | integer | yes | Count of newly created users in the bucket. |

### Source relationships

- Derived from `Users` based on account creation time.

---

## 7. Existing operational entities used as inputs

### Order

Used for:

- total order count
- order status breakdown
- date scoping for top products

Relevant fields:

- order identifier
- created timestamp
- current status
- payment method/status for validity filters where needed

### OrderItem

Used for:

- top product ranking
- units sold
- gross product revenue

Relevant fields:

- order identifier
- product identifier
- product name snapshot
- unit price
- quantity
- line total

### Payment

Used for:

- gross revenue
- paid-order counting
- payment-based truth for revenue analytics

Relevant fields:

- order identifier
- amount
- transaction status
- created timestamp

### Refund

Used for:

- refund amount
- retained revenue calculations

Relevant fields:

- payment identifier
- amount
- refund status
- created timestamp

### Product

Used for:

- active product snapshot count
- top-product display metadata

Relevant fields:

- product identifier
- status
- thumbnail metadata

### User

Used for:

- total new users
- new-user growth series

Relevant fields:

- user identifier
- created timestamp

---

## 8. Derived invariants for phase 1

- Every dashboard section must use the same normalized reporting period.
- Comparison periods must be equal in length to the selected reporting period.
- A zero-activity range still produces a structurally complete report with zero values.
- Product ranking remains gross-sales based until refund attribution becomes available at item level.
- Active products remain snapshot-only until historical product-status data exists.

---

## 9. Persistence impact

Phase 1 requires **no schema migration** if the existing operational schema remains unchanged.

Potential future persistence work, explicitly deferred:

- product status history for active-product trend comparison
- order status history for timeline/funnel analytics
- item-level refund attribution for net product revenue
- precomputed daily fact tables or materialized views for larger data volume

---

## 10. Testing implications

The design implies the following test categories:

- query normalization and range validation tests
- comparison-period calculation tests
- revenue aggregation tests with successful payments and successful refunds
- top-product aggregation tests that verify gross revenue only
- zero-data response tests
- admin-only authorization tests
