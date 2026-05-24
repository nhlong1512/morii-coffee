# Tasks: GHN Sandbox Integration

**Input**: Design documents from `/specs/016-ghn-integration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Unit and authorization tests are required for this feature because the user explicitly requested new test coverage before shipping.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g. US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare feature scaffolding, configuration, and documentation anchors used by every story

- [X] T001 Add GHN sandbox configuration models and option binding in `source/MoriiCoffee.Domain.Shared/Settings/GhnSettings.cs` and `source/MoriiCoffee.Infrastructure/Configurations/OptionsConfiguration.cs`
- [X] T002 [P] Register shipping module dependencies and HTTP client placeholders in `source/MoriiCoffee.Infrastructure/DependencyInjection.cs` and `source/MoriiCoffee.Infrastructure.Persistence/DependencyInjection.cs`
- [X] T003 [P] Create shipping DTO and enum folders for the new feature in `source/MoriiCoffee.Application/SeedWork/DTOs/Shipping/` and `source/MoriiCoffee.Domain.Shared/Enums/Shipping/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain, persistence, and adapter infrastructure that MUST be complete before any user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Extend structured GHN address fields in `source/MoriiCoffee.Domain.Aggregates/OrderAggregate/ValueObjects/DeliveryInfo.cs` and `source/MoriiCoffee.Domain.Aggregates/UserAggregate/Entities/UserDeliveryProfile.cs`
- [X] T005 [P] Add shipping delivery and shipment status enums in `source/MoriiCoffee.Domain.Shared/Enums/Shipping/EDeliveryMethod.cs`, `source/MoriiCoffee.Domain.Shared/Enums/Shipping/EShippingProvider.cs`, and `source/MoriiCoffee.Domain.Shared/Enums/Shipping/EShipmentStatus.cs`
- [X] T006 [P] Create shipping master-data, shipment, and shipment webhook audit entities in `source/MoriiCoffee.Domain/Aggregates/ShippingAggregate/`
- [X] T007 Create repository contracts for shipments, shipping master data, and shipment webhook audit rows in `source/MoriiCoffee.Domain/Repositories/IShipmentRepository.cs`, `source/MoriiCoffee.Domain/Repositories/IShippingMasterDataRepository.cs`, and `source/MoriiCoffee.Domain/Repositories/IShipmentWebhookEventRepository.cs`
- [X] T008 Implement EF Core configurations, repositories, and `ApplicationDbContext`/`UnitOfWork` wiring for the shipping entities in `source/MoriiCoffee.Infrastructure.Persistence/Configurations/`, `source/MoriiCoffee.Infrastructure.Persistence/Repositories/`, `source/MoriiCoffee.Infrastructure.Persistence/Data/ApplicationDbContext.cs`, and `source/MoriiCoffee.Infrastructure.Persistence/SeedWork/UnitOfWork/UnitOfWork.cs`
- [X] T009 Create and review the migration for structured delivery, shipping master data, shipments, and webhook audit persistence in `source/MoriiCoffee.Infrastructure.Persistence/Migrations/`
- [X] T010 [P] Create GHN client abstractions, request/response models, and payload mappers in `source/MoriiCoffee.Application/SeedWork/Abstractions/IShippingGateway.cs` and `source/MoriiCoffee.Infrastructure/Services/Shipping/`
- [X] T011 [P] Implement shared shipping application services for package metrics, quote validation, status mapping, and client-order-code generation in `source/MoriiCoffee.Application/Services/Shipping/`
- [X] T012 [P] Add foundational tests for the new enums, value objects, and shipment entity state transitions in `source/MoriiCoffee.Application.Tests/Commands/Shipping/` and `source/MoriiCoffee.Application.Tests/Queries/Shipping/`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Checkout With GHN Delivery (Priority: P1) 🎯 MVP

**Goal**: Enable structured GHN delivery address capture, master-data lookup, quote generation, and order placement with saved shipping snapshots

**Independent Test**: Authenticate with a cart, fetch master data, request a GHN quote for a valid address, then place COD and Stripe-backed delivery orders that persist structured delivery and quote data correctly.

### Tests for User Story 1

- [X] T013 [P] [US1] Add validator and handler tests for structured delivery profile save/update flows in `source/MoriiCoffee.Application.Tests/Commands/User/SaveDeliveryProfile/`
- [X] T014 [P] [US1] Add quote generation and quote validation tests in `source/MoriiCoffee.Application.Tests/Commands/Shipping/CreateShippingQuote/` and `source/MoriiCoffee.Application.Tests/Queries/Shipping/`
- [X] T015 [P] [US1] Add order placement tests covering GHN COD orders and Stripe checkout draft finalization with structured delivery fields in `source/MoriiCoffee.Application.Tests/Commands/Order/PlaceOrder/` and `source/MoriiCoffee.Application.Tests/Commands/Payment/`

### Implementation for User Story 1

- [X] T016 [P] [US1] Extend delivery profile DTOs, commands, validators, and handlers for structured address persistence in `source/MoriiCoffee.Application/SeedWork/DTOs/User/DeliveryProfileDto.cs` and `source/MoriiCoffee.Application/Commands/User/SaveDeliveryProfile/`
- [X] T017 [P] [US1] Implement shipping master-data sync/read models, queries, and handlers in `source/MoriiCoffee.Application/SeedWork/DTOs/Shipping/`, `source/MoriiCoffee.Application/Queries/Shipping/`, and `source/MoriiCoffee.Application/Commands/Shipping/SyncShippingMasterData/`
- [X] T018 [US1] Add public shipping master-data endpoints in `source/MoriiCoffee.Presentation/Controllers/ShippingController.cs`
- [X] T019 [US1] Implement shipping quote request/response DTOs, validators, and handlers in `source/MoriiCoffee.Application/Commands/Shipping/CreateShippingQuote/` and `source/MoriiCoffee.Application/SeedWork/DTOs/Shipping/`
- [X] T020 [US1] Add the public quote endpoint and error mapping in `source/MoriiCoffee.Presentation/Controllers/ShippingController.cs`
- [X] T021 [US1] Extend order DTOs, `PlaceOrderDto`, `PlaceOrderCommand`, and validators for `deliveryMethod`, structured address, and quote references in `source/MoriiCoffee.Application/SeedWork/DTOs/Order/`, `source/MoriiCoffee.Application/Commands/Order/PlaceOrder/`, and `source/MoriiCoffee.Presentation/Controllers/OrdersController.cs`
- [X] T022 [US1] Persist GHN delivery snapshots, selected service, and quoted shipping totals for COD orders in `source/MoriiCoffee.Application/Commands/Order/PlaceOrder/PlaceOrderCommandHandler.cs`
- [X] T023 [US1] Preserve structured GHN delivery and quote snapshots during payment-first order creation in `source/MoriiCoffee.Application/Services/StripeCheckoutDraftService.cs` and `source/MoriiCoffee.Application/SeedWork/DTOs/Payment/`

**Checkpoint**: User Story 1 should now support structured delivery profile data, master-data reads, quotes, and delivery order creation independently

---

## Phase 4: User Story 2 - Fulfill And Manage Shipment (Priority: P2)

**Goal**: Automatically create GHN shipments for eligible delivery orders and give admins safe retry, sync, requote, note update, and cancel capabilities

**Independent Test**: Place a GHN delivery order, confirm shipment creation or `FAILED_TO_CREATE`, then use admin endpoints to retry creation, requote, sync, update note, and cancel without creating duplicates.

### Tests for User Story 2

- [X] T024 [P] [US2] Add shipment creation and idempotent retry tests in `source/MoriiCoffee.Application.Tests/Commands/Shipping/CreateShipment/`
- [X] T025 [P] [US2] Add admin shipment action tests for requote, sync, update note, and cancel flows in `source/MoriiCoffee.Application.Tests/Commands/Shipping/`
- [X] T026 [P] [US2] Add controller authorization tests for admin shipment actions in `source/MoriiCoffee.Application.Tests/Presentation/ShippingAuthorizationTests.cs`

### Implementation for User Story 2

- [X] T027 [P] [US2] Implement shipment creation, retry, and requote commands plus DTOs in `source/MoriiCoffee.Application/Commands/Shipping/CreateShipment/`, `source/MoriiCoffee.Application/Commands/Shipping/RequoteShipment/`, and `source/MoriiCoffee.Application/SeedWork/DTOs/Shipping/`
- [X] T028 [P] [US2] Implement shipment sync, update-note, and cancel commands plus DTOs in `source/MoriiCoffee.Application/Commands/Shipping/SyncShipment/`, `source/MoriiCoffee.Application/Commands/Shipping/UpdateShipmentNote/`, and `source/MoriiCoffee.Application/Commands/Shipping/CancelShipment/`
- [X] T029 [US2] Add automatic shipment creation orchestration in `source/MoriiCoffee.Application/Commands/Order/PlaceOrder/PlaceOrderCommandHandler.cs` and `source/MoriiCoffee.Application/Services/StripeCheckoutDraftService.cs`
- [X] T030 [US2] Add admin shipment management endpoints in `source/MoriiCoffee.Presentation/Controllers/ShippingController.cs` and/or `source/MoriiCoffee.Presentation/Controllers/OrdersController.cs`
- [X] T031 [US2] Add shipment summary mapping into order/application DTOs for staff workflows in `source/MoriiCoffee.Application/SeedWork/DTOs/Order/OrderDto.cs` and `source/MoriiCoffee.Application/SeedWork/DTOs/Shipping/`

**Checkpoint**: User Story 2 should now support operational shipment creation and admin recovery/management flows independently

---

## Phase 5: User Story 3 - Track Shipment Progress (Priority: P3)

**Goal**: Surface shipment state on order reads and keep it updated through webhook and manual sync flows

**Independent Test**: Fetch pickup and GHN delivery orders before and after webhook/manual sync events and verify customer/admin reads show the correct shipment summary without corrupting payment state.

### Tests for User Story 3

- [X] T032 [P] [US3] Add shipment read-model tests for pickup, pending-shipment, and active-shipment order detail cases in `source/MoriiCoffee.Application.Tests/Queries/Order/` and `source/MoriiCoffee.Application.Tests/Queries/Shipping/`
- [X] T033 [P] [US3] Add webhook audit and idempotent status-update tests in `source/MoriiCoffee.Application.Tests/Commands/Shipping/HandleShippingWebhookEvent/`
- [X] T034 [P] [US3] Add controller tests for customer shipment reads and webhook endpoint behavior in `source/MoriiCoffee.Application.Tests/Presentation/ShippingAuthorizationTests.cs`

### Implementation for User Story 3

- [X] T035 [P] [US3] Implement shipment summary queries and mapping for customer/admin order reads in `source/MoriiCoffee.Application/Queries/Shipping/GetShipmentByOrderId/`, `source/MoriiCoffee.Application/Queries/Order/GetOrderById/`, and `source/MoriiCoffee.Application/Queries/Order/GetMyOrders/`
- [X] T036 [P] [US3] Implement GHN webhook parsing, audit persistence, and idempotent status mapping in `source/MoriiCoffee.Application/Commands/Shipping/HandleShippingWebhookEvent/` and `source/MoriiCoffee.Infrastructure/Services/Shipping/`
- [X] T037 [US3] Add shipment read and webhook endpoints in `source/MoriiCoffee.Presentation/Controllers/ShippingController.cs` and `source/MoriiCoffee.Presentation/Controllers/ShippingWebhookController.cs`
- [X] T038 [US3] Include normalized shipment summaries in customer/admin order response DTOs and mappings in `source/MoriiCoffee.Application/SeedWork/DTOs/Order/` and `source/MoriiCoffee.Application/Queries/Order/`

**Checkpoint**: All user stories should now be independently functional, including shipment visibility and asynchronous status updates

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, cleanup, and release readiness across all stories

- [X] T039 [P] Refresh feature documentation and API annotations for GHN shipping endpoints in `specs/016-ghn-integration/quickstart.md`, `specs/016-ghn-integration/contracts/`, and `source/MoriiCoffee.Presentation/Controllers/`
- [X] T040 Validate sandbox configuration examples and failure logging paths in `source/MoriiCoffee.Domain.Shared/Settings/GhnSettings.cs`, `source/MoriiCoffee.Infrastructure/Services/Shipping/`, and `source/MoriiCoffee.Presentation/appsettings*.json`
- [X] T041 Run the full build and targeted backend test suite for GHN delivery, shipment lifecycle, and webhook coverage using the verification steps in `specs/016-ghn-integration/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies, can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all story work
- **Phase 3 (US1)**: Depends on Phase 2 and is the MVP
- **Phase 4 (US2)**: Depends on Phase 2 and on US1 order/quote structures being in place
- **Phase 5 (US3)**: Depends on Phase 2 and benefits from US2 shipment orchestration being complete
- **Phase 6 (Polish)**: Depends on all targeted stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational and has no dependency on other stories
- **User Story 2 (P2)**: Starts after Foundational but requires US1 delivery/quote/order payload extensions to be present
- **User Story 3 (P3)**: Starts after Foundational and requires shipment entities plus webhook/read-model infrastructure; most useful after US2

### Within Each User Story

- Tests should be written before or alongside implementation and must fail before the corresponding implementation is considered complete
- DTOs/models before handlers/services
- Handlers/services before controllers
- Order/query mapping updates before final verification

### Suggested Execution Order

1. Finish Setup and Foundational phases
2. Deliver **US1** as the MVP for structured GHN checkout and delivery order creation
3. Deliver **US2** for shipment orchestration and admin operations
4. Deliver **US3** for read models and webhook-driven tracking
5. Finish Polish and execute the quickstart verification

---

## Parallel Opportunities

- `T002` and `T003` can run in parallel after `T001`
- `T005`, `T006`, `T010`, `T011`, and `T012` can run in parallel once `T004` is defined
- `T013`, `T014`, and `T015` can run in parallel for US1 tests
- `T016` and `T017` can run in parallel before the controller/handler wiring tasks of US1
- `T024`, `T025`, and `T026` can run in parallel for US2 tests
- `T027` and `T028` can run in parallel before orchestration wiring in `T029`
- `T032`, `T033`, and `T034` can run in parallel for US3 tests
- `T035` and `T036` can run in parallel before endpoint wiring in `T037`

---

## Parallel Example: User Story 1

```bash
# Parallel test work for US1
Task: "T013 Add validator and handler tests for structured delivery profile save/update flows in source/MoriiCoffee.Application.Tests/Commands/User/SaveDeliveryProfile/"
Task: "T014 Add quote generation and quote validation tests in source/MoriiCoffee.Application.Tests/Commands/Shipping/CreateShippingQuote/ and source/MoriiCoffee.Application.Tests/Queries/Shipping/"
Task: "T015 Add order placement tests covering GHN COD orders and Stripe checkout draft finalization with structured delivery fields in source/MoriiCoffee.Application.Tests/Commands/Order/PlaceOrder/ and source/MoriiCoffee.Application.Tests/Commands/Payment/"

# Parallel model/service work for US1
Task: "T016 Extend delivery profile DTOs, commands, validators, and handlers for structured address persistence in source/MoriiCoffee.Application/SeedWork/DTOs/User/DeliveryProfileDto.cs and source/MoriiCoffee.Application/Commands/User/SaveDeliveryProfile/"
Task: "T017 Implement shipping master-data sync/read models, queries, and handlers in source/MoriiCoffee.Application/SeedWork/DTOs/Shipping/, source/MoriiCoffee.Application/Queries/Shipping/, and source/MoriiCoffee.Application/Commands/Shipping/SyncShippingMasterData/"
```

---

## Parallel Example: User Story 2

```bash
# Parallel test work for US2
Task: "T024 Add shipment creation and idempotent retry tests in source/MoriiCoffee.Application.Tests/Commands/Shipping/CreateShipment/"
Task: "T025 Add admin shipment action tests for requote, sync, update note, and cancel flows in source/MoriiCoffee.Application.Tests/Commands/Shipping/"
Task: "T026 Add controller authorization tests for admin shipment actions in source/MoriiCoffee.Application.Tests/Presentation/ShippingAuthorizationTests.cs"

# Parallel handler work for US2
Task: "T027 Implement shipment creation, retry, and requote commands plus DTOs in source/MoriiCoffee.Application/Commands/Shipping/CreateShipment/, source/MoriiCoffee.Application/Commands/Shipping/RequoteShipment/, and source/MoriiCoffee.Application/SeedWork/DTOs/Shipping/"
Task: "T028 Implement shipment sync, update-note, and cancel commands plus DTOs in source/MoriiCoffee.Application/Commands/Shipping/SyncShipment/, source/MoriiCoffee.Application/Commands/Shipping/UpdateShipmentNote/, and source/MoriiCoffee.Application/Commands/Shipping/CancelShipment/"
```

---

## Parallel Example: User Story 3

```bash
# Parallel test work for US3
Task: "T032 Add shipment read-model tests for pickup, pending-shipment, and active-shipment order detail cases in source/MoriiCoffee.Application.Tests/Queries/Order/ and source/MoriiCoffee.Application.Tests/Queries/Shipping/"
Task: "T033 Add webhook audit and idempotent status-update tests in source/MoriiCoffee.Application.Tests/Commands/Shipping/HandleShippingWebhookEvent/"
Task: "T034 Add controller tests for customer shipment reads and webhook endpoint behavior in source/MoriiCoffee.Application.Tests/Presentation/ShippingAuthorizationTests.cs"

# Parallel implementation work for US3
Task: "T035 Implement shipment summary queries and mapping for customer/admin order reads in source/MoriiCoffee.Application/Queries/Shipping/GetShipmentByOrderId/, source/MoriiCoffee.Application/Queries/Order/GetOrderById/, and source/MoriiCoffee.Application/Queries/Order/GetMyOrders/"
Task: "T036 Implement GHN webhook parsing, audit persistence, and idempotent status mapping in source/MoriiCoffee.Application/Commands/Shipping/HandleShippingWebhookEvent/ and source/MoriiCoffee.Infrastructure/Services/Shipping/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (US1)
3. Run the US1-relevant tests and quickstart checks for master data, quotes, and order placement
4. Stop and validate the feature as a shippable structured GHN checkout MVP

### Incremental Delivery

1. Deliver US1 for structured checkout and order creation
2. Add US2 for shipment orchestration and admin recovery actions
3. Add US3 for shipment read models and webhook-driven tracking
4. Finish with cross-cutting verification and documentation

### Suggested MVP Scope

The recommended MVP is **User Story 1 only**: structured delivery profile data, GHN master-data reads, quote generation, and GHN-aware order creation for COD and Stripe-compatible flows.

---

## Notes

- Every task follows the required checklist format with task ID, optional parallel marker, required story label for story phases, and exact file paths.
- The `setup-tasks.sh` helper returned a non-blocking shell warning but still provided the correct `FEATURE_DIR` and template, so task generation proceeded normally.
- Keep commits small and verification-driven once implementation begins.
