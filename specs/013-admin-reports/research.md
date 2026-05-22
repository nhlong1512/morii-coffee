# Phase 0 — Research & Decisions

**Feature**: 013-admin-reports  
**Date**: 2026-05-22

This document resolves the key business and technical decisions required before designing the reports contracts and implementation plan. Decisions were cross-checked against the current backend source and `code-review-graph`.

---

## R-001: Reporting source of truth for revenue

**Decision**: Use **completed payments minus completed refunds** as the source of truth for retained revenue.

**Rationale**:

- The current backend already models `Payment` and `RefundRecord` separately from `Order`.
- `Order.Total` alone cannot distinguish successful payment, failed payment, abandoned payment, or refunded revenue.
- The current payment flow already treats Stripe-confirmed payment state and refund settlement as authoritative business events.

**Alternatives considered**:

- *Use `Order.Total` from orders created in range*: rejected — inflates revenue when payments fail or later refunds happen.
- *Use only successful payment totals without subtracting refunds*: rejected — does not reflect retained money.

---

## R-002: Revenue time bucketing

**Decision**: Bucket revenue using the timestamps of completed payments and completed refunds, not just order creation time.

**Rationale**:

- The metric is intended to reflect money retained over time, not merely order creation velocity.
- Refunds may happen in a later period than the original purchase and should impact the period in which the money is actually given back.

**Alternatives considered**:

- *Bucket by `Order.CreatedAt` only*: rejected — simpler but less truthful for retained revenue and cross-period refunds.

---

## R-003: Order count definition

**Decision**: Count **orders created in the selected period**, regardless of their final status.

**Rationale**:

- This aligns with the business question "how many orders entered the system during this period?"
- It keeps the summary card consistent with the order status breakdown, which also starts from orders created in the selected period.

**Alternatives considered**:

- *Count only completed/delivered orders*: rejected — this is a different metric and hides cancellation or failure volume.

---

## R-004: Order status breakdown semantics

**Decision**: Build the status breakdown from **orders created in the selected period**, grouped by their **current/latest status**.

**Rationale**:

- The current schema exposes only the current order status; it does not provide a historical status transition ledger.
- This produces a useful operational picture without inventing unsupported timeline behavior.

**Alternatives considered**:

- *As-of-date status snapshot*: rejected — current schema cannot reconstruct this reliably.
- *Status transition funnel over time*: rejected for phase 1 — requires an order status history projection.

---

## R-005: Active products metric behavior

**Decision**: Treat `activeProducts` as a **snapshot-only metric** in phase 1.

**Rationale**:

- The current schema exposes product status but not historical product status changes.
- A period-over-period change value would be fabricated rather than evidence-based.

**Alternatives considered**:

- *Compute historical comparison from current table*: rejected — inaccurate and misleading.
- *Introduce a product status audit log in phase 1*: rejected — too much scope for a read-only dashboard feature.

---

## R-006: Top products revenue semantics

**Decision**: Report **gross product revenue** in the top-products section, not item-level net revenue.

**Rationale**:

- Refunds are currently recorded at the payment/order level, not allocated down to order items.
- Product-level net revenue cannot be computed exactly without an explicit allocation rule or item-level refund model.
- Gross sales still provide a trustworthy ranking for phase 1.

**Alternatives considered**:

- *Estimate net product revenue by prorating refunds across items*: rejected — false precision and difficult to explain to stakeholders.
- *Hide revenue and rank only by units sold*: rejected — reduces business usefulness when gross sales are still accurate.

---

## R-007: Report scope for phase 1

**Decision**: Keep phase 1 limited to five sections:

1. summary cards  
2. revenue trend  
3. order status breakdown  
4. top products  
5. new-user trend

**Rationale**:

- This exactly matches the current frontend reports direction described in the ideation document.
- The current backend schema does not support loyalty, store-level, or channel-level reporting with trustworthy semantics.

**Alternatives considered**:

- *Add loyalty analytics back in*: rejected — explicitly out of current scope.
- *Add store/channel segmentation now*: rejected — no reliable current data model support.

---

## R-008: API shape strategy

**Decision**: Expose one main **dashboard** endpoint plus one **export** endpoint in phase 1.

**Rationale**:

- The admin reports page needs all sections together and should not require several round-trips for initial load.
- A matching export endpoint keeps frontend and exported numbers aligned.

**Alternatives considered**:

- *Expose only small per-widget endpoints*: rejected for phase 1 — more HTTP chatter and more frontend coordination for little value.
- *Expose dashboard only with no export*: rejected — export is part of the accepted business flow.

---

## R-009: Internal architecture for analytics queries

**Decision**: Introduce a dedicated **reports read repository** behind the Application query handlers.

**Rationale**:

- Aggregation queries are a poor fit for the existing CRUD-style repositories.
- This keeps analytics SQL/EF logic out of controllers and out of unrelated order/payment/product handlers.
- It leaves a clean upgrade path to materialized views or read projections later without breaking application contracts.

**Alternatives considered**:

- *Write all aggregation directly inside query handlers*: rejected — harms testability and separation of concerns.
- *Create a full new aggregate/domain model*: rejected — the feature is read-only and does not need new write-side invariants.

---

## R-010: Caching approach

**Decision**: Support short-lived cacheability at the dashboard query level, but keep caching as an implementation optimization rather than a phase-1 design dependency.

**Rationale**:

- Admin reports are low-frequency and tolerance for slight staleness is acceptable.
- The first release should work correctly without introducing a mandatory cache dependency.

**Alternatives considered**:

- *Require caching from day one*: rejected — adds complexity before actual performance evidence.
- *Never cache*: rejected — leaves no documented path for scaling if queries become expensive.

---

## R-011: Export format

**Decision**: Support **CSV** export in phase 1.

**Rationale**:

- CSV is simple, portable, and good enough for offline sharing and spreadsheet analysis.
- It matches the current admin reporting behavior described in the ideation notes.

**Alternatives considered**:

- *PDF export*: rejected — presentation-heavy and not required for phase 1.
- *Excel-specific workbook*: rejected — extra complexity without a stated need.

---

## R-012: Engineering truthfulness constraints

**Decision**: The phase 1 design explicitly excludes:

- active-product trend comparison
- product-level net revenue
- loyalty metrics
- store/channel segmentation

**Rationale**:

- Each of these would imply a stronger source-of-truth model than the current schema provides.
- Avoiding misleading analytics is more important than filling every dashboard tile.

**Alternatives considered**:

- *Approximate the unsupported metrics*: rejected — business reporting should prefer omission over misleading certainty.

---

## Summary

All major design unknowns are resolved. The resulting phase 1 design is:

- truthful to current business data
- minimal-impact to the existing architecture
- ready for Phase 1 artifacts: data model, contracts, and quickstart
