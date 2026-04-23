# Tasks: Redis-Backed Core Flows

**Input**: Design documents from `/specs/010-apply-redis/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Include automated verification tasks because the implementation plan and quickstart explicitly require unit and flow validation for catalog caching, authenticated carts, and password reset tickets.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Maps the task to a specific user story (`US1`, `US2`, `US3`)
- Every implementation task includes an exact file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the base Redis dependencies and configuration surface required by all stories.

- [X] T001 Add Redis package references to `source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj`
- [X] T002 Add Redis configuration defaults to `source/MoriiCoffee.Presentation/appsettings.json`
- [X] T003 Add the local Redis service and API dependency wiring to `deploy/docker-compose.development.yml`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared Redis settings, dependency injection, and reusable contracts before story work begins.

**⚠️ CRITICAL**: No user story work should start until this phase is complete.

- [X] T004 Create the Redis settings model in `source/MoriiCoffee.Domain.Shared/Settings/RedisSettings.cs`
- [X] T005 [P] Create shared Redis abstractions in `source/MoriiCoffee.Application/SeedWork/Abstractions/IProductCatalogCache.cs`, `source/MoriiCoffee.Application/SeedWork/Abstractions/ICartService.cs`, and `source/MoriiCoffee.Application/SeedWork/Abstractions/IPasswordResetTicketStore.cs`
- [X] T006 [P] Add shared Redis DTOs/models in `source/MoriiCoffee.Application/SeedWork/DTOs/Cart/CartDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Cart/CartItemDto.cs`, and `source/MoriiCoffee.Application/SeedWork/DTOs/Auth/PasswordResetTicketDto.cs`
- [X] T007 Register Redis connectivity, serialization helpers, and service wiring in `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`
- [X] T008 Add Redis startup validation and connectivity logging in `source/MoriiCoffee.Presentation/Program.cs`

**Checkpoint**: Redis foundation is ready and user story implementation can proceed.

---

## Phase 3: User Story 1 - Browse an up-to-date menu quickly (Priority: P1) 🎯 MVP

**Goal**: Cache hot catalog reads, invalidate them correctly on catalog writes, and preserve database fallback when Redis is unavailable.

**Independent Test**: Call `GET /api/v1/products` and `GET /api/v1/products/{id}` repeatedly, update a product or variant, then confirm refreshed data appears and catalog reads still work when Redis is unavailable.

### Tests for User Story 1

- [X] T009 [P] [US1] Extend paginated catalog cache hit/miss/fallback coverage in `source/MoriiCoffee.Application.Tests/Queries/Product/GetPaginatedProductsQueryHandlerTests.cs`
- [X] T010 [P] [US1] Extend product detail cache hit/miss/fallback coverage in `source/MoriiCoffee.Application.Tests/Queries/Product/GetProductByIdQueryHandlerTests.cs`

### Implementation for User Story 1

- [X] T011 [P] [US1] Create Redis-backed catalog cache models and helpers in `source/MoriiCoffee.Infrastructure/Services/Redis/ProductCatalogCacheModels.cs`
- [X] T012 [US1] Implement the Redis catalog cache service in `source/MoriiCoffee.Infrastructure/Services/Redis/ProductCatalogCache.cs`
- [X] T013 [US1] Integrate read-through caching into `source/MoriiCoffee.Application/Queries/Product/GetPaginatedProducts/GetPaginatedProductsQueryHandler.cs` and `source/MoriiCoffee.Application/Queries/Product/GetProductById/GetProductByIdQueryHandler.cs`
- [X] T014 [P] [US1] Add product write-side cache invalidation to `source/MoriiCoffee.Application/Commands/Product/CreateProduct/CreateProductCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/Product/UpdateProduct/UpdateProductCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/Product/DeleteProduct/DeleteProductCommandHandler.cs`
- [X] T015 [P] [US1] Add variant and image write-side cache invalidation to `source/MoriiCoffee.Application/Commands/ProductVariant/CreateProductVariant/CreateProductVariantCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/ProductVariant/UpdateProductVariant/UpdateProductVariantCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/ProductVariant/DeleteProductVariant/DeleteProductVariantCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/Product/UploadProductImages/UploadProductImagesCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/Product/DeleteProductImage/DeleteProductImageCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/Product/ReorderProductImages/ReorderProductImagesCommandHandler.cs`
- [X] T016 [US1] Add catalog cache logging and fallback handling in `source/MoriiCoffee.Infrastructure/Services/Redis/ProductCatalogCache.cs` and `source/MoriiCoffee.Application/Queries/Product/GetPaginatedProducts/GetPaginatedProductsQueryHandler.cs`

**Checkpoint**: User Story 1 should be independently functional and demoable as the MVP.

---

## Phase 4: User Story 2 - Keep an active shopping cart between sessions (Priority: P2)

**Goal**: Add an authenticated-user cart stored in Redis with merge-free quantity updates, TTL refresh, and restart-safe retrieval.

**Independent Test**: Authenticate as a customer, fetch an empty cart, add and update items, restart the API, verify the cart persists, then remove items and clear the cart successfully.

### Tests for User Story 2

- [X] T017 [P] [US2] Add authenticated cart service behavior tests in `source/MoriiCoffee.Application.Tests/Commands/Cart/RedisCartServiceTests.cs`
- [X] T018 [P] [US2] Add cart API flow tests in `source/MoriiCoffee.Application.Tests/Commands/Cart/CartControllerTests.cs`

### Implementation for User Story 2

- [X] T019 [P] [US2] Add cart request DTOs in `source/MoriiCoffee.Application/SeedWork/DTOs/Cart/AddCartItemDto.cs` and `source/MoriiCoffee.Application/SeedWork/DTOs/Cart/UpdateCartItemDto.cs`
- [X] T020 [US2] Implement the Redis cart service in `source/MoriiCoffee.Infrastructure/Services/Redis/RedisCartService.cs`
- [X] T021 [US2] Add cart commands and queries in `source/MoriiCoffee.Application/Commands/Cart/AddCartItem/AddCartItemCommand.cs`, `source/MoriiCoffee.Application/Commands/Cart/UpdateCartItem/UpdateCartItemCommand.cs`, `source/MoriiCoffee.Application/Commands/Cart/RemoveCartItem/RemoveCartItemCommand.cs`, `source/MoriiCoffee.Application/Commands/Cart/ClearCart/ClearCartCommand.cs`, and `source/MoriiCoffee.Application/Queries/Cart/GetCart/GetCartQuery.cs`
- [X] T022 [US2] Implement cart command and query handlers in `source/MoriiCoffee.Application/Commands/Cart/AddCartItem/AddCartItemCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/Cart/UpdateCartItem/UpdateCartItemCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/Cart/RemoveCartItem/RemoveCartItemCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/Cart/ClearCart/ClearCartCommandHandler.cs`, and `source/MoriiCoffee.Application/Queries/Cart/GetCart/GetCartQueryHandler.cs`
- [X] T023 [US2] Expose authenticated cart endpoints in `source/MoriiCoffee.Presentation/Controllers/CartController.cs`
- [X] T024 [US2] Add cart-specific logging and Redis-unavailable handling in `source/MoriiCoffee.Infrastructure/Services/Redis/RedisCartService.cs` and `source/MoriiCoffee.Presentation/Controllers/CartController.cs`

**Checkpoint**: User Story 2 should now work independently on top of the shared Redis foundation.

---

## Phase 5: User Story 3 - Complete password reset through a short-lived reset session (Priority: P3)

**Goal**: Replace exposed reset tokens with opaque one-time Redis tickets while preserving the public forgot/reset API shape and privacy-safe behavior.

**Independent Test**: Request a password reset, verify the email link carries an opaque ticket, reset the password once successfully, and confirm expired or reused tickets are rejected.

### Tests for User Story 3

- [X] T025 [P] [US3] Extend forgot-password ticket issuance tests in `source/MoriiCoffee.Application.Tests/Commands/Auth/ForgotPasswordCommandHandlerTests.cs`
- [X] T026 [P] [US3] Extend reset-password ticket consumption tests in `source/MoriiCoffee.Application.Tests/Commands/Auth/ResetPasswordCommandHandlerTests.cs`

### Implementation for User Story 3

- [X] T027 [US3] Implement the Redis password reset ticket store in `source/MoriiCoffee.Infrastructure/Services/Redis/RedisPasswordResetTicketStore.cs`
- [X] T028 [US3] Update forgot-password flow to issue opaque tickets in `source/MoriiCoffee.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs`
- [X] T029 [US3] Update reset-password flow to consume opaque tickets in `source/MoriiCoffee.Application/Commands/Auth/ResetPassword/ResetPasswordCommandHandler.cs`
- [X] T030 [US3] Update reset request contract compatibility in `source/MoriiCoffee.Application/SeedWork/DTOs/Auth/ResetPasswordDto.cs` and `source/MoriiCoffee.Presentation/Controllers/AuthController.cs`
- [X] T031 [US3] Update password reset email link generation in `source/MoriiCoffee.Infrastructure/Services/Email/BrevoEmailService.cs` and `source/MoriiCoffee.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs`

**Checkpoint**: User Story 3 should be independently functional without regressing current public auth behavior.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish verification, rollout notes, and cross-story quality checks.

- [X] T032 [P] Add Redis implementation notes and local verification guidance to `docs/features/apply-redis/redis-plan.md`
- [X] T033 Run the quickstart smoke-test checklist from `specs/010-apply-redis/quickstart.md` and capture any follow-up fixes in `specs/010-apply-redis/quickstart.md`
- [X] T034 [P] Add final cross-story logging and operational event review in `source/MoriiCoffee.Infrastructure/Services/Redis/ProductCatalogCache.cs`, `source/MoriiCoffee.Infrastructure/Services/Redis/RedisCartService.cs`, and `source/MoriiCoffee.Infrastructure/Services/Redis/RedisPasswordResetTicketStore.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2 only; recommended MVP slice.
- **User Story 2 (Phase 4)**: Depends on Phase 2 only; independent from US1 once Redis foundation exists.
- **User Story 3 (Phase 5)**: Depends on Phase 2 only; independent from US1 and US2 once Redis foundation exists.
- **Polish (Phase 6)**: Depends on completion of all desired user stories.

### User Story Dependencies

- **US1**: No dependency on other user stories.
- **US2**: No dependency on other user stories.
- **US3**: No dependency on other user stories.

### Recommended Execution Order

1. Finish T001-T008.
2. Deliver the MVP by finishing T009-T016.
3. Implement T017-T024 for authenticated cart persistence.
4. Implement T025-T031 for opaque password reset tickets.
5. Finish T032-T034 for verification and rollout polish.

---

## Parallel Opportunities

### Setup / Foundation

- T005 and T006 can run in parallel after T004.
- T007 and T008 can run sequentially once T004-T006 are complete.

### User Story 1

- T009 and T010 can run in parallel.
- T014 and T015 can run in parallel after T012-T013.

### User Story 2

- T017 and T018 can run in parallel.
- T019 can run in parallel with T017-T018.
- T021 can begin once T020 is scoped and the DTOs from T019 exist.

### User Story 3

- T025 and T026 can run in parallel.
- T028 and T031 should be coordinated closely but can be split across different files after T027 starts.

---

## Parallel Example: User Story 1

```bash
Task: "Extend paginated catalog cache hit/miss/fallback coverage in source/MoriiCoffee.Application.Tests/Queries/Product/GetPaginatedProductsQueryHandlerTests.cs"
Task: "Extend product detail cache hit/miss/fallback coverage in source/MoriiCoffee.Application.Tests/Queries/Product/GetProductByIdQueryHandlerTests.cs"

Task: "Add product write-side cache invalidation to source/MoriiCoffee.Application/Commands/Product/CreateProduct/CreateProductCommandHandler.cs, source/MoriiCoffee.Application/Commands/Product/UpdateProduct/UpdateProductCommandHandler.cs, and source/MoriiCoffee.Application/Commands/Product/DeleteProduct/DeleteProductCommandHandler.cs"
Task: "Add variant and image write-side cache invalidation to source/MoriiCoffee.Application/Commands/ProductVariant/CreateProductVariant/CreateProductVariantCommandHandler.cs, source/MoriiCoffee.Application/Commands/ProductVariant/UpdateProductVariant/UpdateProductVariantCommandHandler.cs, source/MoriiCoffee.Application/Commands/ProductVariant/DeleteProductVariant/DeleteProductVariantCommandHandler.cs, source/MoriiCoffee.Application/Commands/Product/UploadProductImages/UploadProductImagesCommandHandler.cs, source/MoriiCoffee.Application/Commands/Product/DeleteProductImage/DeleteProductImageCommandHandler.cs, and source/MoriiCoffee.Application/Commands/Product/ReorderProductImages/ReorderProductImagesCommandHandler.cs"
```

## Parallel Example: User Story 2

```bash
Task: "Add authenticated cart service behavior tests in source/MoriiCoffee.Application.Tests/Commands/Cart/RedisCartServiceTests.cs"
Task: "Add cart API flow tests in source/MoriiCoffee.Application.Tests/Commands/Cart/CartControllerTests.cs"
Task: "Add cart request DTOs in source/MoriiCoffee.Application/SeedWork/DTOs/Cart/AddCartItemDto.cs and source/MoriiCoffee.Application/SeedWork/DTOs/Cart/UpdateCartItemDto.cs"
```

## Parallel Example: User Story 3

```bash
Task: "Extend forgot-password ticket issuance tests in source/MoriiCoffee.Application.Tests/Commands/Auth/ForgotPasswordCommandHandlerTests.cs"
Task: "Extend reset-password ticket consumption tests in source/MoriiCoffee.Application.Tests/Commands/Auth/ResetPasswordCommandHandlerTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational prerequisites.
3. Complete Phase 3: User Story 1.
4. Validate catalog cache hit/miss, invalidation, and Redis-down fallback before expanding scope.

### Incremental Delivery

1. Ship catalog caching first as the lowest-risk, highest-value improvement.
2. Add authenticated cart persistence as the next independently testable slice.
3. Add opaque password reset tickets last, preserving public auth compatibility during rollout.
4. Finish with smoke tests and operational polish across all Redis-backed flows.

### Suggested MVP Scope

- **MVP**: Phase 1 + Phase 2 + Phase 3 (`T001-T016`)
- **Next increment**: Phase 4 (`T017-T024`)
- **Final increment**: Phase 5 + Phase 6 (`T025-T034`)

## Notes

- All tasks follow the required checklist format with task ID, optional parallel marker, story label where required, and exact file paths.
- User stories remain independently testable after the shared Redis foundation is complete.
- Tests are included because verification is explicitly required by the implementation plan and quickstart.
