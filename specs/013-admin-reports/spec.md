# Feature Specification: Admin Reports

**Feature Branch**: `013-admin-reports`  
**Created**: 2026-05-22  
**Status**: Draft  
**Input**: User description: "Read docs/features/report-statistic/report-statistic-spec.md and start the Speckit flow for the report-statistic feature, beginning with specify."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Admin reviews a trusted business snapshot (Priority: P1)

An administrator opens the reports area and sees a trusted summary of current business performance for a selected time range, including revenue, order volume, customer growth, and the current count of active products.

**Why this priority**: The feature has no value unless administrators can quickly understand core business performance without manually combining data from several areas.

**Independent Test**: An administrator selects a supported reporting period and confirms the summary cards show complete values, clear comparison context where supported, and no missing data for the primary dashboard view.

**Acceptance Scenarios**:

1. **Given** an administrator opens reports, **When** the default reporting period is loaded, **Then** the system shows summary metrics for revenue, orders, new users, and active products in one view.
2. **Given** an administrator changes the reporting period, **When** the report refreshes, **Then** the summary metrics update to reflect only the selected period.
3. **Given** a metric supports period-over-period comparison, **When** the report is displayed, **Then** the administrator can see the current value and the comparison context against the immediately preceding period of equal length.
4. **Given** active product count is displayed, **When** the administrator views that metric, **Then** it is presented as a current snapshot rather than a historical trend comparison.

---

### User Story 2 - Admin analyzes trends and operational mix (Priority: P1)

An administrator explores charts and ranked lists in the reports area to understand how revenue changes over time, how orders are distributed across fulfillment states, which products are selling best, and how many new users joined over the selected period.

**Why this priority**: Summary cards alone are not enough for decision-making; administrators need supporting trends and breakdowns to act on performance changes.

**Independent Test**: An administrator loads the reports area for multiple supported date ranges and verifies that the trend sections, status breakdown, top products, and new-user timeline all refresh consistently for the same reporting period.

**Acceptance Scenarios**:

1. **Given** report data exists for the selected period, **When** an administrator views the revenue trend, **Then** the chart shows the period broken into time buckets with revenue values for each bucket.
2. **Given** orders exist in multiple states during the selected period, **When** an administrator views the order status breakdown, **Then** the report shows each included status with its order count and share of the total.
3. **Given** products have been sold during the selected period, **When** an administrator views the product ranking, **Then** the report lists the top-selling products with enough detail to compare sales performance.
4. **Given** new users joined during the selected period, **When** an administrator views the user growth trend, **Then** the report shows how registrations were distributed over time.

---

### User Story 3 - Admin exports reporting data for sharing and follow-up (Priority: P2)

An administrator exports the same reporting view they are inspecting so the data can be shared, archived, or analyzed outside the application without manually rebuilding the report.

**Why this priority**: Export is secondary to on-screen analysis, but it is an important operational workflow for review meetings, offline analysis, and stakeholder sharing.

**Independent Test**: An administrator exports a report for a selected period and confirms the output includes the same metric groups and reflects the same filters as the on-screen report.

**Acceptance Scenarios**:

1. **Given** an administrator has selected a reporting period, **When** they export the report, **Then** the exported file reflects the same period and reporting sections shown on screen.
2. **Given** a report contains no values for one or more sections, **When** the administrator exports it, **Then** the export still completes successfully and clearly represents zero-value sections.

### Edge Cases

- What happens when the selected period has no orders, no payments, and no new users?
- What happens when the immediately preceding comparison period has zero value for a metric that is now greater than zero?
- How does the report behave when refunds occur after the original sale period?
- What happens when an administrator selects the largest supported reporting range?
- How does the system behave when a previously sold product is no longer active but still appears in historical sales rankings?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide an administrator-only reports area for viewing business performance.
- **FR-002**: The system MUST support reports for predefined time ranges that cover recent, medium-term, and annual views.
- **FR-003**: The system MUST allow administrators to request a custom reporting period within an approved maximum range.
- **FR-004**: The system MUST present summary metrics for total revenue, total orders, new users, and active products for the selected reporting period.
- **FR-005**: The system MUST calculate total revenue from completed customer payments minus completed refunds, so the displayed revenue reflects money retained rather than gross order creation alone.
- **FR-006**: The system MUST calculate total orders using orders created during the selected reporting period.
- **FR-007**: The system MUST calculate new users using user accounts created during the selected reporting period.
- **FR-008**: The system MUST calculate active products as the number of products currently available for sale at the time the report is requested.
- **FR-009**: The system MUST show period-over-period comparison values for metrics that can be measured accurately against an immediately preceding period of equal length.
- **FR-010**: The system MUST avoid presenting a misleading comparison for metrics that are only available as a current snapshot.
- **FR-011**: The system MUST present a revenue trend view that breaks the selected period into appropriate time buckets and shows revenue performance across those buckets.
- **FR-012**: The system MUST present an order status breakdown for orders created during the selected reporting period.
- **FR-013**: The system MUST present a ranked list of top-selling products for the selected reporting period using product sales activity recorded in completed orders.
- **FR-014**: The system MUST show product ranking values that remain trustworthy even when refunds cannot be attributed precisely to individual sold items.
- **FR-015**: The system MUST present a new-user growth trend for the selected reporting period using time buckets appropriate to the range selected.
- **FR-016**: The system MUST keep all report sections aligned to the same selected reporting period so administrators do not compare mismatched numbers.
- **FR-017**: The system MUST allow administrators to export the report they are viewing for offline sharing and follow-up.
- **FR-018**: The system MUST ensure exported data contains the same core sections as the on-screen report: summary metrics, revenue trend, order status breakdown, top products, and new-user trend.
- **FR-019**: The system MUST return a complete report response even when one or more sections have zero values for the selected period.
- **FR-020**: The system MUST provide a clear empty-state outcome when the selected period contains no reportable business activity.
- **FR-021**: The system MUST prevent non-administrator users from accessing the reports feature.
- **FR-022**: The system MUST keep the first release focused on revenue, orders, products, and user growth without introducing loyalty analytics, location-level analytics, or sales-channel segmentation.

### Key Entities *(include if feature involves data)*

- **Reporting Period**: The selected date range and comparison context used to scope every metric and chart on the reports page.
- **Summary Metric**: A single business KPI shown in the dashboard header area, including its value and any supported comparison context.
- **Revenue Trend Point**: A time-bucketed measurement used to show how retained revenue changes over the selected period.
- **Order Status Breakdown**: A grouped view of orders created within the selected period, organized by their current fulfillment state.
- **Product Sales Ranking**: A ranked summary of products sold during the selected period, including enough sales detail to compare product performance.
- **User Growth Point**: A time-bucketed count of newly created user accounts within the selected reporting period.
- **Report Export**: A portable representation of the currently selected report view for sharing or offline analysis.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An administrator can open the reports feature and understand the current business snapshot for the default reporting period in under 2 minutes on first use.
- **SC-002**: An administrator can switch between supported reporting periods and receive a fully refreshed report in under 10 seconds for 95% of requests during acceptance testing.
- **SC-003**: 100% of accepted test scenarios show matching reporting periods across summary metrics, trends, rankings, and exported output.
- **SC-004**: 100% of revenue acceptance tests reflect completed refunds in the retained-revenue figures shown to administrators.
- **SC-005**: 100% of role-based acceptance tests confirm that only administrators can access the reports feature.
- **SC-006**: An administrator can export a selected report and confirm that all five core reporting sections are present in the output on the first attempt.
- **SC-007**: In zero-activity acceptance tests, administrators still receive a complete, understandable report view without missing sections or broken visual states.

## Assumptions

- The first release is intended for internal administrators rather than general staff users.
- Revenue reporting is expected to reflect retained money after completed refunds rather than raw order creation totals.
- Active product count is treated as a current business snapshot and not as a historical trend in the first release.
- Product rankings in the first release prioritize trustworthy gross sales performance over item-level net revenue estimates when refund attribution is not exact.
- The reports page already exists conceptually in the admin experience and is ready to consume backend-driven data once this feature is implemented.

## Dependencies

- Existing order, payment, refund, product, and user records remain available and trustworthy enough to support reporting calculations.
- Existing administrator authentication and authorization remain available to protect the reports feature.
- Existing admin experience continues to provide a place where this reporting feature can be surfaced to administrators.

## Out of Scope

- Loyalty point reporting
- Store-by-store reporting
- Sales-channel reporting
- Cohort retention analytics
- Scheduled report delivery
- Real-time streaming updates
- Product-level refund attribution beyond the precision supported by the current business data
