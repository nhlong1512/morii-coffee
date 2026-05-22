# Implementation Plan: Admin Reports

**Branch**: `013-admin-reports` | **Date**: 2026-05-22 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/013-admin-reports/spec.md`

## Summary

Implement an admin-only reports module that replaces the current mock reporting experience with backend-driven metrics for revenue, orders, products, and user growth.

The technical approach is:

- add a dedicated **Admin Reports read module** instead of forcing analytics logic into existing CRUD handlers for orders, payments, products, or users
- keep the first release **read-only** and aggregate directly from the current PostgreSQL operational tables (`Orders`, `OrderItems`, `Payments`, `Refunds`, `Products`, `Users`) without introducing a new analytics database or materialized view
- treat **completed payments minus completed refunds** as the source of truth for retained revenue
- expose a compact **dashboard response** plus a matching **CSV export** contract for the admin frontend
- keep the design honest to the current schema by treating `activeProducts` as a snapshot-only metric and limiting top-product revenue to **gross sales**, not item-level net revenue

This plan creates only design artefacts (research, data model, contracts, quickstart). Implementation tasks are produced by `/speckit-tasks` next.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) across the backend projects  
**Primary Dependencies**: Existing stack only for backend implementation: ASP.NET Core Web API, MediatR, FluentValidation, AutoMapper, EF Core 10, Npgsql, Swashbuckle  
**Storage**: PostgreSQL via EF Core + Npgsql using existing operational tables; no new persistence store required for phase 1  
**Testing**: xUnit + Moq + FluentAssertions in `MoriiCoffee.Application.Tests`, with handler/query coverage and controller-level authorization verification  
**Target Platform**: ASP.NET Core backend service in Docker, consumed by the Morii admin frontend  
**Project Type**: Backend web service with documented frontend integration contracts  
**Performance Goals**:
- dashboard response <= 3 seconds p95 for supported ranges in phase 1
- CSV export generation <= 10 seconds p95 for supported ranges in phase 1
- zero-activity ranges still return a complete response with all sections populated
**Constraints**:
- must preserve current Clean Architecture layering
- must reuse existing `ApiResponse` response envelope and admin authorization patterns
- must not introduce loyalty, store-level, or channel-level analytics in phase 1
- must not present misleading product net revenue or historical active-product comparison not supported by the current schema
**Scale/Scope**:
- expected admin usage is low concurrency but queries may scan high-volume operational tables
- first release covers 5 report sections: summary cards, revenue trend, order status breakdown, top products, new-user trend
- implementation likely touches 20–30 files across Application, Infrastructure.Persistence, Presentation, tests, and docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance check | Status |
|---|---|---|
| **I. Plan Mode Default** | This is a non-trivial, cross-layer reporting feature with aggregate business rules and frontend-facing contracts. Planning is required and now in place. | ✅ |
| **II. Verification Before Done** | Implementation will require query-level tests, controller authorization verification, and manual endpoint checks before completion. Quickstart defines a verification path. | ✅ |
| **III. Simplicity First & Minimal Impact** | The design stays read-only, reuses current tables, and avoids new projections/materialized views for phase 1. Changes are isolated to a dedicated reports module and admin controller surface. | ✅ |
| **IV. Subagent Strategy & Delegation** | No subagents are required for planning. Context was kept focused while using `code-review-graph` for architecture validation. | ✅ |
| **V. Self-Improvement Loop** | Any user correction during later implementation will be captured in `tasks/lessons.md`. | ✅ |
| **VI. Autonomous Execution with Concise Communication** | Planning artefacts are being generated end-to-end without unnecessary user back-and-forth. | ✅ |
| **Tech stack constraints** | The design stays on the current backend stack and does not introduce new infrastructure or frameworks. | ✅ |
| **Minimal impact to existing features** | Orders, payments, products, and users remain the source systems; the feature adds read-side aggregation only and does not alter checkout or fulfillment behavior. | ✅ |

**Result (pre-design)**: No constitutional violations. No entries in *Complexity Tracking*.

### Post-design re-evaluation

After Phase 1 artefacts (`research.md`, `data-model.md`, `contracts/*`, `quickstart.md`) were authored:

| Principle | Re-check finding |
|---|---|
| Simplicity / Minimal impact | Confirmed. Phase 1 reads directly from current operational data and avoids new persistence models, event pipelines, or warehouse patterns. |
| Verification before done | Confirmed. `quickstart.md` defines endpoint-level verification and `data-model.md` enumerates the required test coverage areas. |
| Layering discipline | Confirmed. The design keeps HTTP contracts in Presentation, orchestration in Application query handlers/services, and aggregation queries in Infrastructure.Persistence. |
| Truthfulness of metrics | Confirmed. The design explicitly avoids unsupported calculations such as item-level net revenue and historical active-product comparison. |

No new constitutional violations were introduced by the design.

## Project Structure

### Documentation (this feature)

```text
specs/013-admin-reports/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── README.md
│   ├── admin-reports-dashboard.md
│   └── admin-reports-export.md
├── checklists/
│   └── requirements.md
└── tasks.md                # Phase 2 output — produced by /speckit-tasks
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Application/
│   ├── Queries/
│   │   └── Report/
│   │       ├── GetAdminReportsDashboard/
│   │       └── ExportAdminReports/
│   ├── SeedWork/
│   │   └── DTOs/
│   │       └── Report/
│   │           ├── AdminReportsDashboardDto.cs
│   │           ├── ReportRangeDto.cs
│   │           ├── ReportMetricCardDto.cs
│   │           ├── RevenueSeriesDto.cs
│   │           ├── OrderStatusBreakdownDto.cs
│   │           ├── TopProductDto.cs
│   │           └── NewUsersSeriesDto.cs
│   └── Services/
│       └── Reports/
│           ├── ReportQueryNormalizer.cs
│           └── ComparisonPeriodResolver.cs
│
├── MoriiCoffee.Domain/
│   └── Repositories/
│       └── IAdminReportsReadRepository.cs
│
├── MoriiCoffee.Infrastructure.Persistence/
│   └── Repositories/
│       └── AdminReportsReadRepository.cs
│
├── MoriiCoffee.Presentation/
│   └── Controllers/
│       └── AdminReportsController.cs
│
└── MoriiCoffee.Application.Tests/
    ├── Queries/
    │   └── Report/
    └── Presentation/
        └── AdminReportsAuthorizationTests.cs
```

**Structure Decision**: Introduce a dedicated read-side reports module that follows the repo's existing Clean Architecture split. The feature does not add a new domain aggregate because phase 1 is purely analytical and read-only. Instead, it introduces one read repository abstraction and query handlers dedicated to admin reporting so analytics logic does not leak into existing order/payment/product/user CRUD handlers.

## Complexity Tracking

No constitutional violations to justify. Section intentionally empty.
