# Contract: Admin Reports Export

## Purpose

Export the currently selected admin report view into a portable offline format for sharing and analysis.

## Endpoint

- **Method**: `GET`
- **Path**: `/api/v1/admin/reports/export`
- **Auth**: Administrator only

## Query Parameters

| Name | Type | Required | Notes |
|---|---|---|---|
| `format` | string | yes | Phase 1 supports `csv`. |
| `preset` | string | no | Supported reporting preset. |
| `from` | date | conditional | Required when exporting a custom range. |
| `to` | date | conditional | Required when exporting a custom range. |
| `granularity` | string | no | Optional bucket override when supported. |
| `timezone` | string | no | Optional timezone identifier. |

## Successful Response

- **Status**: `200`
- **Content-Type**: `text/csv`
- **Disposition**: attachment download with a deterministic file name

## Export Content Requirements

The exported file must include the same five reporting sections as the on-screen report:

1. summary metrics
2. revenue trend
3. order status breakdown
4. top products
5. new-user trend

## CSV Expectations

- The file must clearly identify the selected reporting period.
- Zero-value sections must still appear in the file.
- Exported numbers must match the same query inputs used for the on-screen report.

## Error Responses

### Invalid export format

- **Status**: `400`
- Returned when the caller requests an unsupported export format.

### Invalid range

- **Status**: `400`
- Returned when the selected reporting period is invalid.

### Unauthorized

- **Status**: `401`

### Forbidden

- **Status**: `403`
