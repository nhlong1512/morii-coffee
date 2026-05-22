# Quickstart — Admin Reports

This guide is for engineers validating the phase-1 admin reports feature once implementation begins.

## Goal

Verify that the backend can return and export a complete admin reports view using the agreed phase-1 business rules.

## Preconditions

- The API is running locally.
- The database contains representative data for:
  - users
  - products
  - orders
  - payments
  - refunds
- You have an administrator account that can access protected admin endpoints.

## Core verification flow

1. Sign in as an administrator and obtain a valid access token.
2. Request the dashboard endpoint for a short preset range.
3. Verify the response includes all five sections:
   - summary cards
   - revenue series
   - order status breakdown
   - top products
   - new-user series
4. Verify the `range` block is present and consistent across the response.
5. Verify `activeProducts` is returned as a snapshot metric with no misleading comparison values.
6. Verify `topProducts` returns trustworthy gross sales values.
7. Export the same reporting period as CSV.
8. Verify the exported file contains the same five reporting sections and matches the selected range.

## Example requests

```bash
curl -H "Authorization: Bearer <admin-token>" \
  "http://localhost:5100/api/v1/admin/reports/dashboard?preset=30D&granularity=day&timezone=Asia/Ho_Chi_Minh"

curl -OJ -H "Authorization: Bearer <admin-token>" \
  "http://localhost:5100/api/v1/admin/reports/export?format=csv&preset=30D&granularity=day&timezone=Asia/Ho_Chi_Minh"

dotnet test source/MoriiCoffee.Application.Tests/MoriiCoffee.Application.Tests.csproj -v minimal
```

## Scenario checklist

### Scenario 1: Normal business activity

- Use a date range with payments, refunds, orders, and new users.
- Confirm retained revenue reflects completed refunds.
- Confirm the order status distribution adds up to the total order count.

### Scenario 2: Zero-activity period

- Use a date range with no orders, no payments, and no new users.
- Confirm the dashboard still returns all sections with zero or empty values.
- Confirm export still completes successfully.

### Scenario 3: Comparison edge case

- Use a date range where the comparison period has zero value for at least one metric.
- Confirm the API returns a safe comparison representation instead of misleading math or broken output.

### Scenario 4: Authorization

- Call the dashboard and export endpoints without authentication.
- Call them again as a non-admin user.
- Confirm both endpoints enforce admin-only access.

## Suggested test data profile

For strong verification, seed or select data that includes:

- successful paid orders
- refunded paid orders
- canceled orders
- multiple order statuses
- at least three products with different sales volumes
- at least one inactive product that still appears in historical sales
- at least one new-user spike in a narrow time bucket

## Evidence expected before implementation is considered done

- automated tests covering the main aggregation and authorization paths
- a successful manual dashboard response inspection
- a successful CSV export inspection
- confirmation that zero-data ranges behave correctly
