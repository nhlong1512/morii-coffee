# Feature Specification: Set Up Unit Tests

**Feature Branch**: `008-unit-tests-setup`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Please review the entire codebase, then set up and implement unit tests for this project."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Domain Logic Unit Tests (Priority: P1)

A developer working on domain entities (Product, User, Category, Banner) can run unit tests
that verify aggregate behavior, invariant enforcement, and domain event raising — all without
needing a running database, email service, or file storage.

**Why this priority**: Domain logic is the core business value of the application. Verifying
aggregates, value objects, and entity behaviors independently is the foundation of safe
refactoring and confident feature development.

**Independent Test**: Can be fully tested by running domain-layer tests alone. Delivers
confidence in business rule correctness.

**Acceptance Scenarios**:

1. **Given** a `Product` aggregate with valid inputs, **When** the product is created,
   **Then** all properties are set correctly and no domain events are raised unexpectedly.
2. **Given** a `User` aggregate, **When** `Deactivate()` is called, **Then** the user's status
   changes to `Inactive` and a domain event is recorded.
3. **Given** a `User` aggregate with a current avatar URL, **When** `SetAvatar()` is called
   with a new URL, **Then** the avatar URL is updated correctly.
4. **Given** invalid inputs (e.g. empty product name), **When** the aggregate factory/constructor
   is invoked, **Then** an appropriate exception or validation failure is raised.

---

### User Story 2 - Application Validator Tests (Priority: P2)

A developer working on FluentValidation validators can run isolated unit tests to verify all
validation rules — covering required fields, length limits, format constraints, and business rule
validations — without invoking command handlers or external services.

**Why this priority**: Validators are the first line of defense at the application boundary.
Testing them independently ensures that invalid input is rejected consistently before reaching
handlers or persistence.

**Independent Test**: Can be fully tested by invoking `TestValidate()` on each validator with
various inputs. Delivers confidence that no invalid input reaches the domain layer.

**Acceptance Scenarios**:

1. **Given** a `CreateProductCommandValidator`, **When** validated with an empty product name,
   **Then** a validation error is returned for the `Name` field.
2. **Given** a `CreateProductCommandValidator`, **When** validated with a negative base price,
   **Then** a validation error is returned for the `BasePrice` field.
3. **Given** a `SignUpCommandValidator`, **When** validated with a malformed email address,
   **Then** a validation error is returned for the `Email` field.
4. **Given** a valid command matching all rules, **When** validated,
   **Then** no validation errors are returned.

---

### User Story 3 - Application Command Handler Tests (Priority: P3)

A developer can run unit tests for command handlers (Auth, Product, Category, User, Banner)
with all external dependencies mocked — including repositories, unit of work, token service,
email service, and file service. Tests verify the handler's orchestration logic, including
success paths and business exception paths.

**Why this priority**: Command handlers are where business orchestration happens.
Testing them with mocked dependencies ensures the logic (e.g. slug uniqueness checks,
category existence validation, user creation workflow) is correct without requiring
live infrastructure.

**Independent Test**: Can be fully tested by constructing handlers with mock dependencies
and invoking `Handle()`. Delivers confidence that orchestration logic handles both
happy-path and error cases correctly.

**Acceptance Scenarios**:

1. **Given** a mocked repository that returns no existing product with the same slug,
   **When** `CreateProductCommandHandler.Handle()` is invoked with a valid command,
   **Then** the product is saved and `UnitOfWork.CommitAsync()` is called once.
2. **Given** a mocked `UserManager` that reports email already exists, **When**
   `SignUpCommandHandler.Handle()` is invoked, **Then** a `BadRequestException` is thrown
   with an appropriate message.
3. **Given** a mocked repository that returns `null` for a product ID, **When**
   `UpdateProductCommandHandler.Handle()` is invoked, **Then** a `NotFoundException` is thrown.
4. **Given** a mocked file service, **When** a command with a file attachment is handled,
   **Then** the file service `UploadAsync()` is called exactly once.

---

### User Story 4 - Application Query Handler Tests (Priority: P4)

A developer can run unit tests for query handlers (Product, Category, User, Banner) with
mocked repositories, verifying that data is correctly retrieved, mapped via AutoMapper,
and returned in the expected DTO shape.

**Why this priority**: Query handlers are the read side of the application. Testing them
ensures that pagination, filtering, and DTO mapping produce correct output without needing
a live database.

**Independent Test**: Can be fully tested by constructing query handlers with mock
repositories and an AutoMapper instance. Delivers confidence in the read model.

**Acceptance Scenarios**:

1. **Given** a mocked categories repository returning a list of active categories,
   **When** `GetAllCategoriesQueryHandler.Handle()` is invoked, **Then** the response
   contains the correct number of category DTOs in the correct order.
2. **Given** a mocked product repository returning `null` for an ID, **When**
   `GetProductByIdQueryHandler.Handle()` is invoked, **Then** a `NotFoundException` is thrown.
3. **Given** a mocked user repository, **When** `GetMyProfileQueryHandler.Handle()` is invoked
   with a valid user ID, **Then** the correct user profile DTO is returned.

---

### Edge Cases

- What happens when a command handler is invoked after `UnitOfWork.CommitAsync()` throws an exception?
- How does a validator handle `null` inputs vs. empty string inputs?
- How does a handler behave when the AutoMapper profile is misconfigured?
- What happens when a file service upload fails mid-handler execution?
- How does a query handler behave when the repository returns an empty collection vs. null?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A dedicated unit test project MUST be established for each testable layer
  (Domain, Application), organized to mirror the production project structure.
- **FR-002**: The test infrastructure MUST include a mocking library (for interfaces such as
  repositories, services, and UnitOfWork), a fluent assertion library, and a test runner
  compatible with the CI pipeline.
- **FR-003**: All four domain aggregate roots (Product, User, Category, Banner) MUST have
  unit tests covering their public domain methods and state transitions.
- **FR-004**: All FluentValidation validators across Auth, Product, ProductVariant, Category,
  User, and Banner commands MUST have unit tests covering at least one valid case and all
  defined invalid cases.
- **FR-005**: All command handlers MUST have unit tests covering the success path and each
  distinct exception/failure path, using mocked dependencies.
- **FR-006**: All query handlers MUST have unit tests covering the success path and
  not-found path, using mocked repositories.
- **FR-007**: All AutoMapper profiles MUST have unit tests verifying that source types map
  correctly to their destination DTOs without runtime errors.
- **FR-008**: Tests MUST run fully in isolation — no live database, email server, file
  storage, or network calls are required to execute any unit test.
- **FR-009**: Tests MUST be runnable via a single CLI command from the project root.
- **FR-010**: The test projects MUST be included in the solution file so they are
  discoverable by IDE test runners.

### Key Entities

- **Test Project (Domain)**: A test assembly targeting the Domain layer, containing tests
  for aggregates, value objects, and domain events.
- **Test Project (Application)**: A test assembly targeting the Application layer, containing
  tests for validators, command handlers, query handlers, and AutoMapper profiles.
- **Mock Dependencies**: Stand-in implementations of repository interfaces, service interfaces
  (ITokenService, IEmailService, IFileService), and IUnitOfWork used in handler tests.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All unit tests complete successfully within 30 seconds when run with a
  single command from the project root.
- **SC-002**: Every public method on the four domain aggregates (Product, User, Category,
  Banner) has at least one corresponding test case.
- **SC-003**: Every FluentValidation validator has test coverage for 100% of its defined
  validation rules, including both passing and failing inputs.
- **SC-004**: Every command handler and query handler has test cases covering at minimum
  the primary success path and the primary not-found/invalid-input failure path.
- **SC-005**: All AutoMapper profiles pass a configuration assertion test with zero mapping
  errors.
- **SC-006**: No test requires an external service, network connection, or running container
  to execute — tests pass in a fully offline environment.
- **SC-007**: A new developer can clone the repository and run all unit tests immediately
  without any local environment setup beyond the standard .NET SDK.

## Assumptions

- The primary test layer is **unit tests** (not integration tests). Integration tests with
  real database or containerized services are out of scope for this feature.
- The existing codebase on branch `007-cart-order-payment-stripe` contains all current
  source projects; tests will be added on top of the existing code.
- **xUnit** is used as the test framework (industry standard for .NET, compatible with
  `dotnet test`). NUnit would be equivalent — xUnit is assumed as the default.
- **Moq** is used as the mocking library. NSubstitute would be equivalent — Moq is assumed
  as the widely-used default.
- **FluentAssertions** is used for assertion syntax. This is a reasonable default for
  readable assertion messages.
- AutoMapper profiles already exist for all entities; the mapping tests verify them in their
  current form.
- The `IUnitOfWork` interface already wraps repository save operations; tests will mock it
  at the interface level.
- Tests do not cover the Presentation layer (controllers/middleware) in this phase.
- Tests do not cover Infrastructure services (email, file storage implementations) in this
  phase — only the application layer using mocked interfaces.
