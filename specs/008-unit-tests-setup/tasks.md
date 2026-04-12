# Tasks: Set Up Unit Tests

**Input**: Design documents from `/specs/008-unit-tests-setup/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, quickstart.md ✓

**Organization**: Tasks grouped by user story (P1 → P4) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on each other)
- **[Story]**: Which user story this task belongs to (US1–US4)

---

## Phase 1: Setup (Test Project Scaffolding)

**Purpose**: Create the two test project files and register them in the solution. Nothing else can start until this is done.

- [X] T001 Create `source/MoriiCoffee.Domain.Tests/MoriiCoffee.Domain.Tests.csproj` — xUnit test library targeting net10.0, referencing MoriiCoffee.Domain, with packages: xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.4.0, Moq 4.20.72, FluentAssertions 8.9.0
- [X] T002 Create `source/MoriiCoffee.Application.Tests/MoriiCoffee.Application.Tests.csproj` — xUnit test library targeting net10.0, referencing MoriiCoffee.Application + MoriiCoffee.Domain + MoriiCoffee.Domain.Shared, with same packages as T001
- [X] T003 Register both test projects in `MoriiCoffee.slnx` so they are discoverable by IDE test runners and `dotnet test source/`

---

## Phase 2: Foundational (Build Verification)

**Purpose**: Confirm both new test projects compile cleanly (inheriting `TreatWarningsAsErrors=true` from `Directory.Build.props`) before writing any test code.

**⚠️ CRITICAL**: No user story work can begin until the solution builds warning-free.

- [X] T004 Run `dotnet build source/` and verify zero errors and zero warnings — fix any project reference or package resolution issues before proceeding

**Checkpoint**: Solution builds clean — user story implementation can now begin

---

## Phase 3: User Story 1 — Domain Logic Unit Tests (Priority: P1) 🎯 MVP

**Goal**: All four domain aggregates have test classes covering every public domain method and state transition.

**Independent Test**: `dotnet test source/MoriiCoffee.Domain.Tests/` passes all tests.

- [X] T005 [P] [US1] Create `source/MoriiCoffee.Domain.Tests/Aggregates/UserAggregateTests.cs` — test class for the `User` aggregate covering: `UpdateProfile()` sets FullName/Dob/Gender/Bio; `SetAvatar()` updates AvatarUrl and AvatarFileName; `Activate()` sets Status to Active; `Deactivate()` sets Status to Inactive; `RaiseDomainEvent()` appends to internal list; `GetDomainEvents()` returns the raised events; `ClearDomainEvents()` empties the list
- [X] T006 [P] [US1] Create `source/MoriiCoffee.Domain.Tests/Aggregates/ProductAggregateTests.cs` — test class for the `Product` aggregate covering: property initialization (Name, Slug, BasePrice, Status default Active, IsFeatured, DisplayOrder); `RaiseDomainEvent()` appends event; `GetDomainEvents()` returns events; `ClearDomainEvents()` empties list
- [X] T007 [P] [US1] Create `source/MoriiCoffee.Domain.Tests/Aggregates/CategoryAggregateTests.cs` — test class for the `Category` aggregate covering: property initialization (Name, Description, IconUrl, DisplayOrder, IsActive default); `IsActive` toggle behavior
- [X] T008 [P] [US1] Create `source/MoriiCoffee.Domain.Tests/Aggregates/BannerAggregateTests.cs` — test class for the `Banner` aggregate covering: property initialization; IsActive/soft-delete behavior

**Checkpoint**: `dotnet test source/MoriiCoffee.Domain.Tests/` — all 4 aggregate test classes pass

---

## Phase 4: User Story 2 — Application Validator Tests (Priority: P2)

**Goal**: All FluentValidation validators across Auth, Product, ProductVariant, Category, User, and Banner have isolated unit tests covering every rule (valid case + each invalid case).

**Independent Test**: `dotnet test source/MoriiCoffee.Application.Tests/ --filter "Namespace~Commands"` passes all validator tests.

**Pattern for every validator test**: use `new XxxValidator().TestValidate(command)` then `.ShouldHaveValidationErrorFor(x => x.Field)` or `.ShouldNotHaveAnyValidationErrors()`. No mocks needed.

### Auth Validators

- [X] T009 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/SignUpCommandValidatorTests.cs` — test `SignUpCommandValidator`: empty email → error on Email; invalid email format → error on Email; empty password → error on Password; password too short (<8 chars) → error; password missing uppercase → error; missing lowercase → error; missing digit → error; missing special char → error; empty phone → error; invalid phone format → error; optional UserName too short (<3) → error; UserName too long (>50) → error; UserName with invalid chars → error; fully valid command → no errors
- [X] T010 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/SignInCommandValidatorTests.cs` — test `SignInCommandValidator`: empty email → error; empty password → error; valid inputs → no errors
- [X] T011 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/ForgotPasswordCommandValidatorTests.cs` — test `ForgotPasswordCommandValidator`: empty email → error; invalid email format → error; valid email → no errors
- [X] T012 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/ResetPasswordCommandValidatorTests.cs` — test `ResetPasswordCommandValidator`: empty token → error; password complexity rules (same as SignUp) → errors per rule; valid inputs → no errors

### Product Validators

- [X] T013 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Product/CreateProductCommandValidatorTests.cs` — test `CreateProductCommandValidator`: empty name → error on Name; name > 200 chars → error; slug > 200 chars → error; slug with uppercase → error (regex); slug with spaces → error; negative BasePrice → error; CategoryIds empty → error; DisplayOrder < 0 → error; valid command → no errors
- [X] T014 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Product/UpdateProductCommandValidatorTests.cs` — test `UpdateProductCommandValidator`: same rules as CreateProduct for applicable fields; valid command → no errors

### ProductVariant Validators

- [X] T015 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/ProductVariant/CreateProductVariantCommandValidatorTests.cs` — test `CreateProductVariantCommandValidator`: read the validator source and cover every defined rule for both valid and invalid cases

### Category Validators

- [X] T016 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Category/CreateCategoryCommandValidatorTests.cs` — test `CreateCategoryCommandValidator`: empty name → error; name > maxlen → error; DisplayOrder < 0 → error; valid → no errors
- [X] T017 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Category/UpdateCategoryCommandValidatorTests.cs` — test `UpdateCategoryCommandValidator`: same rules as Create; valid → no errors

### Banner Validators

- [X] T018 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Banner/CreateBannerCommandValidatorTests.cs` — test `CreateBannerCommandValidator`: read the validator source and cover every defined rule for valid and invalid inputs

### User Validators

- [X] T019 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/User/UpdateProfileCommandValidatorTests.cs` — test `UpdateProfileCommandValidator`: FullName > 200 chars → error; Bio > 1000 chars → error; null/empty FullName → no error (optional); valid inputs → no errors
- [X] T020 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/User/ChangePasswordCommandValidatorTests.cs` — test `ChangePasswordCommandValidator`: password complexity rules → errors per rule; valid password → no errors

**Checkpoint**: All 12 validator test classes pass in `dotnet test source/MoriiCoffee.Application.Tests/`

---

## Phase 5: User Story 3 — Application Command Handler Tests (Priority: P3)

**Goal**: All command handlers have unit tests covering the primary success path and each distinct failure path (NotFoundException, BadRequestException), using Moq-mocked dependencies injected via constructor.

**Independent Test**: `dotnet test source/MoriiCoffee.Application.Tests/ --filter "FullyQualifiedName~Commands"` passes all handler tests.

**Pattern for every handler test**:
1. Constructor: create `Mock<IDependency>()` fields, instantiate handler via `new XxxHandler(mock1.Object, ...)`
2. Each test: Setup mocks → `await handler.Handle(command, CancellationToken.None)` → Assert + Verify

### Auth Command Handlers

- [X] T021 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/SignUpCommandHandlerTests.cs` — include a `static Mock<UserManager<User>> CreateMockUserManager()` helper. Tests: success path (FindByEmailAsync=null, FindByNameAsync=null, CreateAsync=Success, AddToRoleAsync=Success → returns AuthResponseDto with tokens); email already exists (FindByEmailAsync returns existing user → throws BadRequestException); phone already exists → throws BadRequestException; CreateAsync returns IdentityResult.Failed → throws BadRequestException
- [X] T022 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/SignInCommandHandlerTests.cs` — Tests: success (FindByEmailAsync returns user, CheckPasswordAsync=true, GenerateAccessTokenAsync returns token → returns AuthResponseDto); user not found (FindByEmailAsync=null → throws UnauthorizedException or BadRequestException); wrong password (CheckPasswordAsync=false → throws appropriate exception)
- [X] T023 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/RefreshTokenCommandHandlerTests.cs` — Tests: success (GetPrincipalFromTokenAsync returns valid principal, GetAuthenticationTokenAsync returns stored token → returns new AuthResponseDto); invalid token (GetPrincipalFromTokenAsync returns null → throws UnauthorizedException); user not found → throws NotFoundException
- [X] T024 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/ForgotPasswordCommandHandlerTests.cs` — Tests: user exists → SendPasswordResetEmailAsync called once, no exception; user not found (FindByEmailAsync=null) → handler completes silently (no exception, no email sent)
- [X] T025 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/ResetPasswordCommandHandlerTests.cs` — Tests: success (user found, ResetPasswordAsync=Success → completes without error); user not found → throws NotFoundException; ResetPasswordAsync returns IdentityResult.Failed → throws BadRequestException
- [X] T026 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Auth/ExternalLoginCommandHandlerTests.cs` — Tests: existing external user found (FindByLoginAsync returns user → returns AuthResponseDto); new user created from external login (FindByLoginAsync=null, CreateAsync=Success → returns AuthResponseDto); invalid external token (GetPrincipalFromTokenAsync=null → throws UnauthorizedException)

### Product Command Handlers

- [X] T027 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Product/CreateProductCommandHandlerTests.cs` — Tests: success without thumbnail (Categories.GetByIdAsync returns valid category, Products.SlugExistsAsync=false, CommitAsync called once, IFileService.UploadAsync NOT called → returns ProductDto); success with thumbnail (IFileService.UploadAsync called once); category ID not found (Categories.GetByIdAsync=null → throws NotFoundException); slug already exists (SlugExistsAsync=true → throws BadRequestException or auto-generates unique slug, whichever the handler does — read the source first)
- [X] T028 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Product/UpdateProductCommandHandlerTests.cs` — Tests: success (Products.GetByIdAsync returns product, Update called, CommitAsync called once → returns ProductDto); product not found (GetByIdAsync=null → throws NotFoundException)
- [X] T029 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Product/DeleteProductCommandHandlerTests.cs` — Tests: success (GetByIdAsync returns product, SoftDelete called, CommitAsync called once); product not found → throws NotFoundException
- [X] T030 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Product/UploadProductImagesCommandHandlerTests.cs` — Tests: success (product found, IFileService.UploadAsync called once per file, ProductImages.CreateListAsync called, CommitAsync called); product not found → throws NotFoundException
- [X] T031 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Product/ReorderProductImagesCommandHandlerTests.cs` — Tests: success (product found, images reordered, CommitAsync called); product not found → throws NotFoundException

### ProductVariant Command Handlers

- [X] T032 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/ProductVariant/CreateProductVariantCommandHandlerTests.cs` — Tests: success (product found, CreateAsync called, CommitAsync called → returns VariantDto); product not found → throws NotFoundException
- [X] T033 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/ProductVariant/UpdateProductVariantCommandHandlerTests.cs` — Tests: success (variant found, Update called, CommitAsync called); variant not found → throws NotFoundException
- [X] T034 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/ProductVariant/DeleteProductVariantCommandHandlerTests.cs` — Tests: success (variant found, SoftDelete called, CommitAsync called); variant not found → throws NotFoundException

### Category Command Handlers

- [X] T035 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Category/CreateCategoryCommandHandlerTests.cs` — Tests: success without icon (CreateAsync called, CommitAsync called once, IFileService.UploadAsync NOT called); success with icon (IFileService.UploadAsync called once); CommitAsync called exactly once regardless of icon presence
- [X] T036 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Category/UpdateCategoryCommandHandlerTests.cs` — Tests: success (category found, Update called, CommitAsync called); category not found → throws NotFoundException
- [X] T037 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Category/DeleteCategoryCommandHandlerTests.cs` — Tests: success (category found, SoftDelete called, CommitAsync called); category not found → throws NotFoundException

### User Command Handlers

- [X] T038 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/User/UpdateProfileCommandHandlerTests.cs` — Tests: success (FindByIdAsync returns user, UpdateAsync called, returns UserDto); user not found → throws NotFoundException
- [X] T039 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/User/ChangeAvatarCommandHandlerTests.cs` — Tests: success (user found, IFileService.UploadAsync called, UpdateAsync called); user not found → throws NotFoundException
- [X] T040 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/User/ChangePasswordCommandHandlerTests.cs` — Tests: success (user found, ChangePasswordAsync=Success); user not found → throws NotFoundException; ChangePasswordAsync returns Failed (wrong current password) → throws BadRequestException
- [X] T041 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/User/AssignRolesCommandHandlerTests.cs` — Tests: success (user found, roles assigned via AddToRolesAsync/RemoveFromRolesAsync); user not found → throws NotFoundException

### Banner Command Handlers

- [X] T042 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Banner/CreateBannerCommandHandlerTests.cs` — Tests: success (CreateAsync called, CommitAsync called exactly once → returns BannerDto); IFileService.UploadAsync called if image provided
- [X] T043 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Banner/UpdateBannerCommandHandlerTests.cs` — Tests: success (banner found, Update called, CommitAsync called); banner not found → throws NotFoundException
- [X] T044 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Banner/DeleteBannerCommandHandlerTests.cs` — Tests: success (banner found, SoftDelete called, CommitAsync called); banner not found → throws NotFoundException

**Checkpoint**: `dotnet test source/MoriiCoffee.Application.Tests/ --filter "FullyQualifiedName~Commands"` — all command handler tests pass

---

## Phase 6: User Story 4 — Application Query Handler Tests (Priority: P4)

**Goal**: All query handlers have unit tests covering the success path, not-found path, and empty-collection path, verifying that AutoMapper correctly maps entities to DTOs.

**Independent Test**: `dotnet test source/MoriiCoffee.Application.Tests/ --filter "FullyQualifiedName~Queries"` passes all query tests.

**Pattern**: Create a real `IMapper` instance using `new MapperConfiguration(cfg => cfg.AddProfile<XxxMapper>()).CreateMapper()` — do NOT mock IMapper in query handler tests, so the mapping logic is exercised end-to-end.

### Product Queries

- [X] T045 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/Product/GetPaginatedProductsQueryHandlerTests.cs` — Tests: success with results (Products.PaginatedFind returns list → returns Pagination<ProductSummaryDto> with correct count and items); empty result (empty list → returns empty Pagination with TotalCount=0)
- [X] T046 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/Product/GetProductByIdQueryHandlerTests.cs` — Tests: product found (Products.GetByIdAsync returns product → returns ProductDto with correct Id); product not found (GetByIdAsync=null → throws NotFoundException)

### ProductVariant Queries

- [X] T047 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/ProductVariant/GetVariantsByProductIdQueryHandlerTests.cs` — Tests: success (product found, variants returned → returns list of VariantDto); product not found → throws NotFoundException
- [X] T048 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/ProductVariant/GetVariantByIdQueryHandlerTests.cs` — Tests: variant found → returns VariantDto; variant not found → throws NotFoundException

### Category Queries

- [X] T049 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/Category/GetAllCategoriesQueryHandlerTests.cs` — Tests: success with ordered results (3 categories with different DisplayOrder values → returned in ascending DisplayOrder, then ascending Name); empty → returns empty Pagination
- [X] T050 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/Category/GetCategoryByIdQueryHandlerTests.cs` — Tests: found → returns CategoryDto; not found → throws NotFoundException

### User Queries

- [X] T051 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/User/GetMyProfileQueryHandlerTests.cs` — Tests: user found (FindByIdAsync returns user, GetRolesAsync returns roles → returns UserDto with roles populated); user not found → throws NotFoundException
- [X] T052 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/User/GetPaginatedUsersQueryHandlerTests.cs` — Tests: success with results → returns Pagination<UserDto>; empty result → returns empty Pagination
- [X] T053 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/User/GetUserByIdQueryHandlerTests.cs` — Tests: found → returns UserDto; not found → throws NotFoundException

### Banner Queries

- [X] T054 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/Banner/GetAllBannersQueryHandlerTests.cs` — Tests: success with results → returns list of BannerDto; empty → returns empty result
- [X] T055 [P] [US4] Create `source/MoriiCoffee.Application.Tests/Queries/Banner/GetBannerByIdQueryHandlerTests.cs` — Tests: found → returns BannerDto; not found → throws NotFoundException

**Checkpoint**: `dotnet test source/MoriiCoffee.Application.Tests/ --filter "FullyQualifiedName~Queries"` — all query handler tests pass

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: AutoMapper profile configuration smoke tests, full test suite verification, and summary documentation.

### AutoMapper Profile Tests (FR-007)

- [X] T056 [P] Create `source/MoriiCoffee.Application.Tests/Mappings/ProductMapperTests.cs` — `MapperConfiguration(cfg => cfg.AddProfile<ProductMapper>()).AssertConfigurationIsValid()`; then map a `Product` → `ProductDto` and assert Name, Slug, BasePrice are correct; map `ProductVariant` → `ProductVariantDto`; map `ProductImage` → `ProductImageDto`
- [X] T057 [P] Create `source/MoriiCoffee.Application.Tests/Mappings/CategoryMapperTests.cs` — `AssertConfigurationIsValid()` for `CategoryMapper`; map `Category` → `CategoryDto` and assert key fields
- [X] T058 [P] Create `source/MoriiCoffee.Application.Tests/Mappings/UserMapperTests.cs` — `AssertConfigurationIsValid()` for `UserMapper`; map `User` → `UserDto` and assert key fields
- [X] T059 [P] Create `source/MoriiCoffee.Application.Tests/Mappings/BannerMapperTests.cs` — `AssertConfigurationIsValid()` for `BannerMapper`; map `Banner` → `BannerDto` and assert key fields

### Final Verification

- [X] T060 Run `dotnet test source/` from the repository root and verify: all tests pass, zero failures, zero skipped, execution time under 30 seconds — fix any remaining issues before marking complete
- [X] T061 Write summary documentation: `docs/explainations/summary-unit-tests-setup-VN.md` and `docs/explainations/summary-unit-tests-setup-ENG.md` covering what was implemented, files created, how to run tests, and how to add new tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user story phases
- **Phase 3 (US1)**: Depends on Phase 2 — domain tests only need Domain.Tests project
- **Phase 4 (US2)**: Depends on Phase 2 — validator tests only need Application.Tests project
- **Phase 5 (US3)**: Depends on Phase 2 — command handler tests need Application.Tests project
- **Phase 6 (US4)**: Depends on Phase 2 — query handler tests need Application.Tests project
- **Phase 7 (Polish)**: Depends on Phase 2 for mapper tests; T060/T061 depend on all prior phases

### User Story Dependencies

- **US1 (P1)**: Independent of US2/US3/US4 — uses only Domain.Tests project
- **US2 (P2)**: Independent of US1/US3/US4 — validators need no mocks
- **US3 (P3)**: Independent of US1/US2 — handlers can be tested while validators are being written
- **US4 (P4)**: Independent of US1/US2/US3 — query tests can be parallelized with command tests

### Within Each Phase — All [P] Tasks Are Parallel

All tasks marked [P] within a phase target different files and have no intra-phase dependencies. They can be executed concurrently.

---

## Parallel Execution Examples

### Phase 3 (US1) — all 4 tasks in parallel
```
Task T005: UserAggregateTests.cs
Task T006: ProductAggregateTests.cs
Task T007: CategoryAggregateTests.cs
Task T008: BannerAggregateTests.cs
```

### Phase 4 (US2) — all 12 validator tests in parallel
```
Task T009: SignUpCommandValidatorTests.cs
Task T010: SignInCommandValidatorTests.cs
Task T011: ForgotPasswordCommandValidatorTests.cs
... (all 12 simultaneously)
```

### Phase 5 (US3) — all 24 command handler tests in parallel
```
Task T021: SignUpCommandHandlerTests.cs
Task T027: CreateProductCommandHandlerTests.cs
Task T035: CreateCategoryCommandHandlerTests.cs
... (all 24 simultaneously)
```

### Cross-phase parallel (once Phase 2 is done)
```
Phase 3 (US1) + Phase 4 (US2) can run simultaneously (different test projects)
Phase 5 (US3) can start as soon as Phase 2 completes, regardless of US1/US2 progress
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Create both .csproj + solution entries
2. Complete Phase 2: Verify build
3. Complete Phase 3: Domain aggregate tests (T005–T008)
4. **STOP and VALIDATE**: `dotnet test source/MoriiCoffee.Domain.Tests/` — all pass
5. Domain business logic is now verifiably tested

### Incremental Delivery

1. Phase 1 + 2 → Infrastructure ready
2. Phase 3 → Domain tests → verify independently
3. Phase 4 → Validator tests → verify independently
4. Phase 5 → Command handler tests → verify independently
5. Phase 6 → Query handler tests → verify independently
6. Phase 7 → Mapper tests + full suite verification + docs

### Single-Developer Fast Path

Complete phases in order: 1 → 2 → 3 → 4 → 5 → 6 → 7  
Within each phase, use parallel agent dispatch for all [P] tasks.

---

## Notes

- **Task count**: 61 tasks total (3 setup + 1 foundational + 4 US1 + 12 US2 + 24 US3 + 11 US4 + 6 polish)
- **[P] tasks**: 58 of 61 tasks are parallelizable within their phase
- **No mocks for IMapper in query tests** — use a real `MapperConfiguration` instance to exercise actual mapping logic
- **UserManager mock helper**: Implement `CreateMockUserManager()` static method in T021 (`SignUpCommandHandlerTests.cs`) first — copy the pattern to T022–T026 and T038–T041
- **Read handler source before writing tests for T027** — the slug uniqueness behavior (throw vs. auto-generate) must match the actual implementation
- **TreatWarningsAsErrors**: All test code must compile warning-free; use `#pragma warning disable` only as a last resort
- Commit after T003 (solution updated), after each phase checkpoint, and after T060 (all green)
