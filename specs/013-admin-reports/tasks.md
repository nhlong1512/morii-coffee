# Tasks: Admin Reports

**Input**: Design documents from `/specs/013-admin-reports/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Include focused backend tests because the plan and quickstart explicitly require query verification, authorization verification, and endpoint-level validation for the reports feature.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no blocking dependency on another incomplete task)
- **[Story]**: Which user story this task belongs to (`[US1]`, `[US2]`, etc.)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the reports module scaffolding and shared feature folders

- [X] T001 Create reports feature folder structure under `source/MoriiCoffee.Application/Queries/Report/`, `source/MoriiCoffee.Application/SeedWork/DTOs/Report/`, `source/MoriiCoffee.Application/Services/Reports/`, `source/MoriiCoffee.Infrastructure.Persistence/Repositories/`, and `source/MoriiCoffee.Application.Tests/Queries/Report/`
- [X] T002 Create the admin reports contracts folder under `specs/013-admin-reports/contracts/` and confirm contract filenames stay aligned with `admin-reports-dashboard.md` and `admin-reports-export.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core read-side reporting infrastructure that MUST exist before any user story can be implemented

**⚠️ CRITICAL**: No user story work should begin until this phase is complete

- [X] T003 [P] Add shared report DTOs in `source/MoriiCoffee.Application/SeedWork/DTOs/Report/ReportRangeDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Report/ReportMetricCardDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Report/RevenueSeriesDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Report/OrderStatusBreakdownDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Report/TopProductDto.cs`, and `source/MoriiCoffee.Application/SeedWork/DTOs/Report/NewUsersSeriesDto.cs`
- [X] T004 [P] Add the dashboard/export wrapper DTOs in `source/MoriiCoffee.Application/SeedWork/DTOs/Report/AdminReportsDashboardDto.cs` and `source/MoriiCoffee.Application/SeedWork/DTOs/Report/AdminReportsExportDto.cs`
- [X] T005 [P] Add the reports read repository contract in `source/MoriiCoffee.Domain/Repositories/IAdminReportsReadRepository.cs`
- [X] T006 [P] Implement reporting range normalization in `source/MoriiCoffee.Application/Services/Reports/ReportQueryNormalizer.cs`
- [X] T007 [P] Implement comparison-period calculation in `source/MoriiCoffee.Application/Services/Reports/ComparisonPeriodResolver.cs`
- [X] T008 Implement the EF-backed read repository in `source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs`
- [X] T009 Update `source/MoriiCoffee.Domain/SeedWork/Persistence/IUnitOfWork.cs` and `source/MoriiCoffee.Infrastructure.Persistence/SeedWork/UnitOfWork/UnitOfWork.cs` to expose `IAdminReportsReadRepository`
- [X] T010 Update dependency registration in `source/MoriiCoffee.Infrastructure/Configurations/MediatRConfiguration.cs` and/or the relevant infrastructure DI file to register reports services and `IAdminReportsReadRepository`

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 - Admin reviews a trusted business snapshot (Priority: P1) 🎯 MVP

**Goal**: Let administrators load a complete dashboard snapshot with trusted summary cards for a selected reporting period

**Independent Test**: As `ADMIN`, request the dashboard for a supported range and confirm the response returns range metadata plus complete summary cards, with active products presented as snapshot-only

### Tests for User Story 1

- [X] T011 [P] [US1] Add query/service tests for range normalization and comparison handling in `source/MoriiCoffee.Application.Tests/Queries/Report/ReportQueryNormalizerTests.cs` and `source/MoriiCoffee.Application.Tests/Queries/Report/ComparisonPeriodResolverTests.cs`
- [X] T012 [P] [US1] Add dashboard query handler tests for summary-card behavior in `source/MoriiCoffee.Application.Tests/Queries/Report/GetAdminReportsDashboardQueryHandlerTests.cs`

### Implementation for User Story 1

- [X] T013 [P] [US1] Implement the dashboard query contract in `source/MoriiCoffee.Application/Queries/Report/GetAdminReportsDashboard/GetAdminReportsDashboardQuery.cs`
- [X] T014 [US1] Implement the dashboard query handler summary-card flow in `source/MoriiCoffee.Application/Queries/Report/GetAdminReportsDashboard/GetAdminReportsDashboardQueryHandler.cs`
- [X] T015 [US1] Add the admin reports controller shell and dashboard endpoint in `source/MoriiCoffee.Presentation/Controllers/AdminReportsController.cs`
- [X] T016 [US1] Add authorization attribute verification for the reports controller in `source/MoriiCoffee.Application.Tests/Presentation/AdminReportsAuthorizationTests.cs`

**Checkpoint**: User Story 1 is functional when an admin can load a trusted dashboard snapshot with correct comparison semantics for supported metrics

---

## Phase 4: User Story 2 - Admin analyzes trends and operational mix (Priority: P1)

**Goal**: Populate the dashboard with revenue trend, order status breakdown, top products, and new-user trend sections for the same reporting period

**Independent Test**: As `ADMIN`, request the dashboard for multiple supported ranges and confirm revenue series, order status breakdown, top products, and new-user trend all refresh consistently for the same selected period

### Tests for User Story 2

- [X] T017 [P] [US2] Add revenue and order aggregation tests in `source/MoriiCoffee.Application.Tests/Queries/Report/GetAdminReportsDashboardRevenueTests.cs` and `source/MoriiCoffee.Application.Tests/Queries/Report/GetAdminReportsDashboardOrderStatusTests.cs`
- [X] T018 [P] [US2] Add top-products and new-users aggregation tests in `source/MoriiCoffee.Application.Tests/Queries/Report/GetAdminReportsDashboardTopProductsTests.cs` and `source/MoriiCoffee.Application.Tests/Queries/Report/GetAdminReportsDashboardNewUsersTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Implement retained-revenue aggregation and time-bucket logic in `source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs`
- [X] T020 [US2] Implement order-status breakdown aggregation in `source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs`
- [X] T021 [US2] Implement top-products gross-sales aggregation in `source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs`
- [X] T022 [US2] Implement new-user trend aggregation in `source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs`
- [X] T023 [US2] Extend `source/MoriiCoffee.Application/Queries/Report/GetAdminReportsDashboard/GetAdminReportsDashboardQueryHandler.cs` to compose all analytical sections into the dashboard response

**Checkpoint**: User Story 2 is functional when all analytical sections return truthful, period-aligned data from the backend

---

## Phase 5: User Story 3 - Admin exports reporting data for sharing and follow-up (Priority: P2)

**Goal**: Allow administrators to export the same reporting view they are inspecting as CSV

**Independent Test**: As `ADMIN`, export a selected reporting period and confirm the CSV contains the same five sections and range context as the dashboard response

### Tests for User Story 3

- [X] T024 [P] [US3] Add export query tests for CSV content structure in `source/MoriiCoffee.Application.Tests/Queries/Report/ExportAdminReportsQueryHandlerTests.cs`
- [X] T025 [P] [US3] Add controller/export authorization coverage in `source/MoriiCoffee.Application.Tests/Presentation/AdminReportsAuthorizationTests.cs`

### Implementation for User Story 3

- [X] T026 [P] [US3] Implement the export query contract in `source/MoriiCoffee.Application/Queries/Report/ExportAdminReports/ExportAdminReportsQuery.cs`
- [X] T027 [US3] Implement CSV generation for report exports in `source/MoriiCoffee.Application/Queries/Report/ExportAdminReports/ExportAdminReportsQueryHandler.cs`
- [X] T028 [US3] Add the export endpoint to `source/MoriiCoffee.Presentation/Controllers/AdminReportsController.cs`

**Checkpoint**: User Story 3 is functional when an admin can export a CSV that matches the current dashboard view

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Complete documentation, endpoint polish, and end-to-end verification across all stories

- [X] T029 [P] Add Swagger summaries, response annotations, and query parameter binding polish in `source/MoriiCoffee.Presentation/Controllers/AdminReportsController.cs`
- [X] T030 [P] Update `specs/013-admin-reports/quickstart.md` with any endpoint or payload adjustments discovered during implementation
- [X] T031 [P] Add edge-case coverage for zero-activity ranges and zero-baseline comparisons in `source/MoriiCoffee.Application.Tests/Queries/Report/GetAdminReportsDashboardEdgeCaseTests.cs`
- [X] T032 Run the full verification workflow from `specs/013-admin-reports/quickstart.md` and record any required implementation follow-ups in `specs/013-admin-reports/tasks.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup**: No dependencies
- **Phase 2: Foundational**: Depends on Setup completion and blocks all user stories
- **Phase 3: User Story 1**: Depends on Foundational completion
- **Phase 4: User Story 2**: Depends on User Story 1 dashboard skeleton and Foundational completion
- **Phase 5: User Story 3**: Depends on User Story 1 controller surface and User Story 2 complete dashboard composition
- **Phase 6: Polish**: Depends on all desired user stories being implemented

### User Story Dependencies

- **US1**: Can start immediately after Foundational and is the suggested MVP slice
- **US2**: Depends on the dashboard query/controller skeleton from US1 but is otherwise independent of export work
- **US3**: Depends on the dashboard composition from US2 so export can mirror the same reporting sections and range semantics

### Within Each User Story

- Shared normalization/comparison services before story handlers
- Query contracts before handlers
- Handlers before controller wiring
- Story-specific tests before final verification of that story

### Parallel Opportunities

- T003, T004, T005, T006, and T007 can run in parallel
- T011 and T012 can run in parallel within US1
- T017 and T018 can run in parallel within US2
- T024 and T025 can run in parallel within US3
- Polish tasks T029, T030, and T031 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Parallel test/design work for dashboard foundations
Task: "Add query/service tests for range normalization and comparison handling in source/MoriiCoffee.Application.Tests/Queries/Report/"
Task: "Add dashboard query handler tests for summary-card behavior in source/MoriiCoffee.Application.Tests/Queries/Report/"

# Parallel implementation prep after foundations exist
Task: "Implement the dashboard query contract in source/MoriiCoffee.Application/Queries/Report/GetAdminReportsDashboard/GetAdminReportsDashboardQuery.cs"
Task: "Add authorization attribute verification for the reports controller in source/MoriiCoffee.Application.Tests/Presentation/AdminReportsAuthorizationTests.cs"
```

---

## Parallel Example: User Story 2

```bash
# Parallel analytical aggregation work inside the read repository
Task: "Implement retained-revenue aggregation and time-bucket logic in source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs"
Task: "Implement order-status breakdown aggregation in source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs"
Task: "Implement top-products gross-sales aggregation in source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs"
Task: "Implement new-user trend aggregation in source/MoriiCoffee.Infrastructure.Persistence/Repositories/AdminReportsReadRepository.cs"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: confirm the admin dashboard snapshot works independently

### Recommended Incremental Delivery

1. Ship **US1** first to replace the summary-card mock with backend truth
2. Add **US2** to fill the analytical sections
3. Add **US3** to support CSV export
4. Finish polish and verification

### Suggested MVP Scope

The thinnest meaningful MVP is **User Story 1**.  
The first production-credible increment is **User Stories 1 + 2** together.

## Notes

- All tasks use explicit file paths or target directories
- `[P]` means the task can be worked on independently without waiting on another incomplete task in the same phase
- The `setup-tasks.sh` script emitted a non-blocking shell warning about `feature_json_matches_feature_dir`, but still returned valid context for this feature
