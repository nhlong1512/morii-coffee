# Tasks: Stripe Online Payment for Cart Checkout

**Input**: Design documents from `/specs/011-stripe-payment/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Unit tests are **mandatory** for this feature — the user request explicitly demands them and the build/test must be green for the feature to ship (FR-022/FR-023, SC-009).

**Organization**: Tasks are grouped by user story. Each story is independently testable; the project can ship after any single P-level story is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Task can run in parallel with other [P] tasks in the same phase (different files, no incomplete dependencies).
- **[Story]**: `[US1]…[US4]` map to user stories from `spec.md`. Setup / Foundational / Polish phases carry no story tag.
- Every task names exact file paths.

## Path Conventions

All paths are absolute from repo root. The repository is a .NET Clean Architecture solution under `source/`. New feature code lives in:

- `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/` — Payment aggregate
- `source/MoriiCoffee.Domain.Shared/Enums/Order/` + `Settings/` — new enums + settings
- `source/MoriiCoffee.Application/Commands/Payment/` + `Queries/Payment/` + `SeedWork/{DTOs,Abstractions}/Payment/`
- `source/MoriiCoffee.Infrastructure/{Services,Configurations}/Payment/`
- `source/MoriiCoffee.Infrastructure.Persistence/{Configurations,Repositories,Migrations}/`
- `source/MoriiCoffee.Presentation/Controllers/`
- `source/MoriiCoffee.Application.Tests/{Commands,Queries}/Payment/`
- `docs/` (beginner guides + summaries)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the Stripe SDK dependency, declare configuration shape, and surface env-var bindings.

- [X] T001 Add `Stripe.net` NuGet package (latest 47.x) to `source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj` via `<PackageReference Include="Stripe.net" Version="47.0.0" />`
- [X] T002 [P] Add `Stripe` configuration section (empty placeholders) to `source/MoriiCoffee.Presentation/appsettings.json`, `appsettings.Development.json`, and `appsettings.Production.json` with keys: `SecretKey`, `PublishableKey`, `WebhookSigningSecret`, `Currency` (= `"vnd"`), `SuccessUrlTemplate`, `CancelUrlPath`
- [X] T003 [P] Create `source/MoriiCoffee.Domain.Shared/Settings/StripeSettings.cs` per `data-model.md §6` (POCO bound from config, with computed `IsLiveMode` property)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, enums, persistence schema, infrastructure abstractions, DI wiring, and webhook controller skeleton — everything every user story builds on.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

### Domain enums and settings

- [X] T004 [P] Create `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentStatus.cs` (values: `NotRequired = 1, Pending = 2, Paid = 3, Failed = 4, Refunded = 5, PartiallyRefunded = 6`)
- [X] T005 [P] Create `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentTransactionStatus.cs` (`Created = 1, Succeeded = 2, Failed = 3, Expired = 4`)
- [X] T006 [P] Create `source/MoriiCoffee.Domain.Shared/Enums/Order/ERefundStatus.cs` (`Pending = 1, Succeeded = 2, Failed = 3`)
- [X] T007 [P] Create `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentWebhookProcessingResult.cs` (`Processed = 1, Duplicate = 2, SignatureInvalid = 3, OrderNotFound = 4, UnhandledEventType = 5, Failed = 6`)
- [X] T008 Modify `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentMethod.cs` to add `STRIPE = 4`

### Domain entities — Payment aggregate

- [X] T009 Create `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Entities/RefundRecord.cs` (child entity per `data-model.md §3`; `EntityBase`; methods `MarkSucceeded`, `MarkFailed`)
- [X] T010 Create `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Entities/PaymentWebhookEvent.cs` (per `data-model.md §4`; `EntityBase`; method `MarkProcessed(EPaymentWebhookProcessingResult, string? error)`)
- [X] T011 Create `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Payment.cs` (aggregate root per `data-model.md §2`; factory `Create`, methods `MarkSucceeded`, `MarkFailed`, `MarkExpired`, `AddRefund`)

### Domain — extend Order aggregate

- [X] T012 Modify `source/MoriiCoffee.Domain/Aggregates/OrderAggregate/Order.cs` to add `PaymentStatus` property (default `NotRequired`), update `Create(...)` to set initial `PaymentStatus = Pending` for `STRIPE` payment method and `NotRequired` for `COD`, add methods `MarkPaymentPaid`, `MarkPaymentFailed`, `ApplyRefund`, extend `Confirm()` guard for unpaid Stripe orders (FR-013)

### Domain — repository interfaces

- [X] T013 [P] Create `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Repositories/IPaymentRepository.cs` (extends `IBaseRepository<Payment>`; methods `GetBySessionIdAsync`, `GetByPaymentIntentIdAsync`, `GetLatestPendingByOrderIdAsync`)
- [X] T014 [P] Create `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Repositories/IPaymentWebhookEventRepository.cs` (methods `TryInsertAsync` returning bool, `UpdateAsync`, `GetByEventIdAsync`)

### Application — gateway abstraction + DTOs

- [X] T015 [P] Create `source/MoriiCoffee.Application/SeedWork/Abstractions/IPaymentGateway.cs` with methods `CreateCheckoutSessionAsync(CreateCheckoutSessionRequest)`, `ConstructWebhookEvent(string rawBody, string signatureHeader)`, `CreateRefundAsync(RefundRequest)`, plus DTO request/response types (`CreateCheckoutSessionRequest`, `CheckoutSessionResult`, `RefundRequest`, `RefundResult`, `WebhookEventEnvelope`)
- [X] T016 [P] Create payment DTOs under `source/MoriiCoffee.Application/SeedWork/DTOs/Payment/`: `CreateCheckoutSessionDto.cs`, `CheckoutSessionResponseDto.cs`, `PaymentDto.cs`, `RefundDto.cs`, `RefundResponseDto.cs`, `OrderPaymentSummaryDto.cs`

### Infrastructure — Stripe gateway + DI

- [X] T017 Create `source/MoriiCoffee.Infrastructure/Services/Payment/StripePaymentGateway.cs` implementing `IPaymentGateway` using `Stripe.net` `SessionService`, `RefundService`, and `EventUtility.ConstructEvent` (per `contracts/create-checkout-session.md`, `contracts/webhook.md`, `contracts/refund.md`)
- [X] T018 [P] Create `source/MoriiCoffee.Infrastructure/Configurations/ConfigureStripeOptions.cs` that binds `StripeSettings` from configuration, validates required fields, logs `IsLiveMode` at startup (R-009/FR-020)
- [X] T019 Modify `source/MoriiCoffee.Infrastructure/DependencyInjection.cs` to call `ConfigureStripeOptions` from `ConfigureSettings` (or a new `ConfigureStripe` method) and register `services.AddScoped<IPaymentGateway, StripePaymentGateway>()` inside `ConfigureDependencyInjection`

### Persistence — EF configurations, repositories, UoW, DbContext, migration

- [X] T020 [P] Create `source/MoriiCoffee.Infrastructure.Persistence/Configurations/PaymentConfiguration.cs` (`HasMany(p => p.Refunds).WithOne().HasForeignKey(r => r.PaymentId).OnDelete(Cascade)`; UNIQUE index on `StripeSessionId`; non-unique on `OrderId` and `StripePaymentIntentId`; `HasOne(...)` to Order with FK `OrderId`, `OnDelete(Restrict)`)
- [X] T021 [P] Create `source/MoriiCoffee.Infrastructure.Persistence/Configurations/RefundRecordConfiguration.cs` (UNIQUE index on `StripeRefundId`; FK to `AspNetUsers` for `InitiatedByAdminUserId`, `OnDelete(Restrict)`)
- [X] T022 [P] Create `source/MoriiCoffee.Infrastructure.Persistence/Configurations/PaymentWebhookEventConfiguration.cs` (UNIQUE index on `StripeEventId`; non-unique on `ReceivedAt DESC`; optional FK `RelatedPaymentId` `OnDelete(SetNull)`)
- [X] T023 [P] Create `source/MoriiCoffee.Infrastructure.Persistence/Repositories/PaymentRepository.cs` implementing `IPaymentRepository` (extends `BaseRepository<Payment>`; uses `.Include(p => p.Refunds)` where needed)
- [X] T024 [P] Create `source/MoriiCoffee.Infrastructure.Persistence/Repositories/PaymentWebhookEventRepository.cs` implementing `IPaymentWebhookEventRepository` (translates Npgsql `23505` unique-violation into `TryInsertAsync` returning `false`)
- [X] T025 Modify `source/MoriiCoffee.Application/SeedWork/Abstractions/IUnitOfWork.cs` (or equivalent existing interface) to expose `IPaymentRepository Payments` and `IPaymentWebhookEventRepository PaymentWebhookEvents`
- [X] T026 Modify `source/MoriiCoffee.Infrastructure.Persistence/UnitOfWork/UnitOfWork.cs` to lazily expose the two new repositories
- [X] T027 Modify `source/MoriiCoffee.Infrastructure.Persistence/ApplicationDbContext.cs` to add `DbSet<Payment> Payments` and `DbSet<PaymentWebhookEvent> PaymentWebhookEvents` (configurations are auto-discovered)
- [X] T028 Generate migration `AddStripePaymentSupport` via `dotnet ef migrations add AddStripePaymentSupport --project source/MoriiCoffee.Infrastructure.Persistence --startup-project source/MoriiCoffee.Presentation --output-dir Migrations` — verify generated SQL adds `Orders.PaymentStatus int NOT NULL DEFAULT 1` plus three new tables

### Webhook controller skeleton (no event dispatch yet)

- [X] T029 Create `source/MoriiCoffee.Presentation/Controllers/PaymentWebhookController.cs` with `[AllowAnonymous] POST /api/v1/payments/webhook` that reads the raw request body, calls `IPaymentGateway.ConstructWebhookEvent` for signature verification, dispatches to `HandleWebhookEventCommand` via MediatR, and returns `200 OK`. Returns `422` on signature failure with audit row written via the command.
- [X] T030 Create `source/MoriiCoffee.Application/Commands/Payment/HandleWebhookEvent/HandleWebhookEventCommand.cs` (carries `rawBody`, `signatureHeader`, the parsed `WebhookEventEnvelope`) and `HandleWebhookEventCommandHandler.cs` skeleton: only persist a `PaymentWebhookEvent` audit row with `ProcessingResult = UnhandledEventType` and return. Story-specific event dispatch lives in subsequent US tasks.

**Checkpoint**: All dependencies for any user story are in place. Build the solution and confirm `0 Warning(s), 0 Error(s)`.

- [X] T031 Run `dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental` and resolve any error before proceeding (clean-architecture-skill §9)

---

## Phase 3: User Story 1 — Card payment end-to-end (Priority: P1) 🎯 MVP

**Goal**: A customer can complete a card payment from cart → Stripe Checkout → success page, with the order arriving in `PaymentStatus = Paid`.

**Independent Test**: With Stripe CLI forwarding events, place an order, hit `POST /payments/checkout-session`, pay with card `4242 4242 4242 4242`, confirm `GET /payments/by-order/{id}` returns `paymentStatus: "Paid"` within seconds.

### Implementation for US1

- [X] T032 [US1] Create `source/MoriiCoffee.Application/Commands/Payment/CreateCheckoutSession/CreateCheckoutSessionCommand.cs` + `CreateCheckoutSessionCommandValidator.cs` (FluentValidation: `OrderId` non-empty, `UserId` non-empty)
- [X] T033 [US1] Create `source/MoriiCoffee.Application/Commands/Payment/CreateCheckoutSession/CreateCheckoutSessionCommandHandler.cs` per `contracts/create-checkout-session.md`: loads order, asserts ownership + `PaymentMethod == STRIPE` + `PaymentStatus == Pending`, calls `IPaymentGateway.CreateCheckoutSessionAsync`, persists `Payment` row inside `IUnitOfWork.ExecuteInTransactionAsync`, returns `CheckoutSessionResponseDto`
- [X] T034 [US1] Create `source/MoriiCoffee.Presentation/Controllers/PaymentsController.cs` and add `POST /api/v1/payments/checkout-session` action wired to `CreateCheckoutSessionCommand`. Include Swashbuckle annotations matching `contracts/create-checkout-session.md`.
- [X] T035 [US1] Modify `source/MoriiCoffee.Application/Commands/Payment/HandleWebhookEvent/HandleWebhookEventCommandHandler.cs` to dispatch `checkout.session.completed`: look up `Payment` by `metadata.paymentId`, call `Payment.MarkSucceeded(pi, charge)`, call `Order.MarkPaymentPaid(pi, charge)`, persist transactionally
- [X] T036 [US1] Create `source/MoriiCoffee.Application/Queries/Payment/GetPaymentByOrderId/GetPaymentByOrderIdQuery.cs` + handler that returns `OrderPaymentSummaryDto` (includes all `Payment` attempts and their `RefundRecord` children) with owner-or-admin authorisation check
- [X] T037 [US1] Add `GET /api/v1/payments/by-order/{orderId}` action to `PaymentsController.cs` per `contracts/get-payment-by-order.md`

### Tests for US1

- [X] T038 [P] [US1] Create `source/MoriiCoffee.Application.Tests/Commands/Payment/CreateCheckoutSessionCommandHandlerTests.cs` with cases: (a) happy path persists Payment + returns DTO with `checkoutUrl`, (b) rejects when `PaymentMethod == COD`, (c) rejects when `PaymentStatus != Pending`, (d) rejects when order belongs to a different user, (e) gateway exception is propagated and no Payment row persists
- [X] T039 [P] [US1] Create `source/MoriiCoffee.Application.Tests/Queries/Payment/GetPaymentByOrderIdQueryHandlerTests.cs` with cases: (a) owner can view, (b) admin can view, (c) other user gets `ForbiddenException`, (d) DTO shape matches contract
- [X] T040 [P] [US1] Create `source/MoriiCoffee.Application.Tests/Commands/Payment/HandleWebhookEventCompletedTests.cs`: simulate a `checkout.session.completed` envelope → asserts `Payment.Status = Succeeded`, `Order.PaymentStatus = Paid`, `Payment.StripePaymentIntentId` + `StripeChargeId` populated
- [X] T041 [US1] Run `dotnet test source/MoriiCoffee.Application.Tests/MoriiCoffee.Application.Tests.csproj --filter "Category!=integration"` and confirm all new tests pass

**Checkpoint**: US1 fully functional. Card payment happy path works end-to-end. Build green, US1 tests green.

---

## Phase 4: User Story 2 — Reliable webhook processing (Priority: P1)

**Goal**: Webhook handler is idempotent, signature-verified, and correctly handles abandonment/failure events even when the customer's browser never returns.

**Independent Test**: Trigger a `checkout.session.expired` event via Stripe CLI for a pending order → confirm `PaymentStatus = Failed` and the audit row records `Processed`. Then resend the same event id → second call returns 200 with audit row marked `Duplicate` and no state change. Tamper the signature header → 422 with audit row marked `SignatureInvalid`.

### Implementation for US2

- [X] T042 [US2] Modify `source/MoriiCoffee.Application/Commands/Payment/HandleWebhookEvent/HandleWebhookEventCommandHandler.cs` to implement the full idempotency flow (per `contracts/webhook.md`): compute payload SHA-256 fingerprint → call `IPaymentWebhookEventRepository.TryInsertAsync(new PaymentWebhookEvent { StripeEventId, EventType, PayloadFingerprint, SignatureVerified, ReceivedAt })` → if returns `false` (duplicate), return `Duplicate` and update no state
- [X] T043 [US2] Modify the same handler to add a dispatch case for `checkout.session.expired`: look up Payment by session id, `Payment.MarkExpired()`, `Order.MarkPaymentFailed()`
- [X] T044 [US2] Modify the same handler to add a dispatch case for `payment_intent.payment_failed`: look up Payment by PI id, `Payment.MarkFailed(reason)`, `Order.MarkPaymentFailed()` if still in `Pending`
- [X] T045 [US2] Modify the same handler to update the audit row at the end of processing: set `ProcessingResult` to the resolved value (`Processed | Duplicate | OrderNotFound | UnhandledEventType | Failed`), set `ProcessedAt = utcNow`. Wrap the whole dispatch in a try/catch — on exception, set `ProcessingResult = Failed` + `ErrorMessage`, rethrow so the controller returns 500 (Stripe retries)
- [X] T046 [US2] Modify `source/MoriiCoffee.Presentation/Controllers/PaymentWebhookController.cs` so that signature-invalid → returns `422` with an audit row written, and unknown event types still return `200` so Stripe stops retrying

### Tests for US2

- [X] T047 [P] [US2] Add to `HandleWebhookEventCommandHandlerTests` (or a new file) cases: (a) same event id sent twice → second returns `Duplicate`, no state change, (b) invalid signature → returns `SignatureInvalid`, no state change, (c) unknown event type → returns `UnhandledEventType`, no state change
- [X] T048 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Payment/HandleWebhookEventExpiredTests.cs`: `checkout.session.expired` envelope → `Payment.Status = Expired` and `Order.PaymentStatus = Failed`
- [X] T049 [P] [US2] Create `source/MoriiCoffee.Application.Tests/Commands/Payment/HandleWebhookEventPaymentFailedTests.cs`: `payment_intent.payment_failed` envelope → `Payment.Status = Failed` with `FailureReason` populated, `Order.PaymentStatus = Failed`
- [X] T050 [P] [US2] Domain-level test in `source/MoriiCoffee.Domain.Tests/Aggregates/OrderAggregate/OrderPaymentStatusTests.cs`: `MarkPaymentPaid` is idempotent with same PI id, throws with a different PI id (covers belt-and-braces idempotency)
- [X] T051 [US2] Run `dotnet test` and confirm all US2 tests pass

**Checkpoint**: US1 + US2 work independently. Webhook handler is production-grade.

---

## Phase 5: User Story 3 — Admin refunds (Priority: P2)

**Goal**: An admin can issue a full or partial refund against a paid order via the API; the order reflects the refund within 30 seconds.

**Independent Test**: With a paid test order, call `POST /api/v1/payments/{orderId}/refund` as admin with `{"amount": 50000}`. Confirm Stripe dashboard shows the refund, then `charge.refunded` webhook flips the order to `PartiallyRefunded`. Calling as a non-admin returns 403; calling with amount > balance returns 400.

### Implementation for US3

- [X] T052 [US3] Create `source/MoriiCoffee.Application/Commands/Payment/RefundPayment/RefundPaymentCommand.cs` + `RefundPaymentCommandValidator.cs` (FluentValidation: `OrderId` non-empty; if `Amount` provided, must be > 0)
- [X] T053 [US3] Create `source/MoriiCoffee.Application/Commands/Payment/RefundPayment/RefundPaymentCommandHandler.cs` per `contracts/refund.md`: load `Payment` with `Status == Succeeded`, compute remaining unrefunded balance, validate amount, call `IPaymentGateway.CreateRefundAsync(piId, amount)`, persist `RefundRecord` with `Status = Pending`, return `RefundResponseDto`
- [X] T054 [US3] Add `POST /api/v1/payments/{orderId}/refund` action to `PaymentsController.cs` with `[Authorize(Roles = nameof(ERole.ADMIN))]`, per `contracts/refund.md`
- [X] T055 [US3] Modify `source/MoriiCoffee.Application/Commands/Payment/HandleWebhookEvent/HandleWebhookEventCommandHandler.cs` to dispatch `charge.refunded`: parse `event.data.object.refunds`, match each by `StripeRefundId` to a local `RefundRecord`, call `MarkSucceeded()`, then call `Order.ApplyRefund(cumulativeRefundedAmount)`

### Tests for US3

- [X] T056 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Payment/RefundPaymentCommandHandlerTests.cs` with cases: (a) full refund happy path, (b) partial refund happy path (45 000 of 137 000), (c) two partial refunds summing to the total flip status to `Refunded`, (d) amount exceeding remaining balance → `BadRequestException`, (e) order has no succeeded payment → `BadRequestException`, (f) gateway throws → no local `RefundRecord` persisted
- [X] T057 [P] [US3] Create `source/MoriiCoffee.Application.Tests/Commands/Payment/HandleWebhookEventRefundedTests.cs`: `charge.refunded` envelope flips `RefundRecord.Status` to `Succeeded` and updates `Order.PaymentStatus` to `Refunded` or `PartiallyRefunded` based on cumulative amount
- [X] T058 [US3] Run `dotnet test` and confirm all US3 tests pass — verified: 268 Application tests + 81 Domain tests, 0 failures

**Checkpoint**: US1 + US2 + US3 work independently. Admin refund flow is complete.

---

## Phase 6: User Story 4 — COD non-regression (Priority: P2)

**Goal**: The existing cash-on-delivery checkout flow is unchanged — zero Stripe API calls, zero new latency, zero new failure modes.

**Independent Test**: Place an order with `paymentMethod: "COD"`. Confirm: (a) no `Payment` row is created, (b) `Order.PaymentStatus = NotRequired`, (c) `Order.OrderStatus = PENDING`, (d) cart is cleared, (e) admin can still call `PATCH /orders/{id}/status` to confirm the order (FR-013 guard does NOT block COD).

### Implementation for US4

- [X] T059 [US4] Audit `source/MoriiCoffee.Application/Commands/Order/PlaceOrder/PlaceOrderCommandHandler.cs` — confirm the only change required is the implicit one from T012 (the `Order.Create` factory now sets `PaymentStatus = NotRequired` for COD); no other modification needed

### Tests for US4

- [X] T060 [P] [US4] Add to `source/MoriiCoffee.Application.Tests/Commands/Order/PlaceOrderCommandHandlerTests.cs` (file already exists per feature 008): COD path creates Order with `PaymentStatus = NotRequired` and does NOT touch `Mock<IPaymentGateway>` (verify with `Times.Never`). If the test file does not yet have payment-gateway awareness, create `PlaceOrderCodNonRegressionTests.cs` alongside it.
- [X] T061 [P] [US4] Add a test that admin's `UpdateOrderStatusCommand` on a `PaymentStatus = NotRequired` COD order successfully transitions PENDING → CONFIRMED (proving FR-013 guard does not block COD)
- [X] T062 [US4] Run `dotnet test` and confirm all US4 tests pass; full suite is green — verified: 349 total tests (268 + 81), 0 failures

**Checkpoint**: All four user stories are independently testable and pass.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Beginner guides, summary docs, build verification, and quickstart smoke.

### Documentation (FR-022, FR-023, CLAUDE.md §7)

- [X] T063 [P] Create `docs/stripe-integration-guide-ENG.md` — beginner-friendly end-to-end guide: what Stripe is, how to sign up, where to find test/live keys, what each env var means, how the moving parts fit together (sequence diagram of customer → backend → Stripe → webhook → backend), how to test locally with Stripe CLI, how to deploy to production, common troubleshooting (signature mismatch, test/live key mix, webhook IP issues). Target audience: someone who has never seen Stripe before.
- [X] T064 [P] Create `docs/stripe-integration-guide-VN.md` — same content as the ENG guide, in Vietnamese
- [X] T065 [P] Create `docs/explainations/summary-stripe-payment-ENG.md` per CLAUDE.md §7: what was implemented + why, complete list of new/modified files grouped by layer, schema changes (Orders.PaymentStatus column + Payments/Refunds/PaymentWebhookEvents tables), all new API endpoints with method/path/auth, business rules enforced (every FR mapped to its code home), how to verify (point at `quickstart.md`)
- [X] T066 [P] Create `docs/explainations/summary-stripe-payment-VN.md` — same content as ENG summary, in Vietnamese
- [X] T067 [P] Update `CLAUDE.md` (the project-level one at repo root) by adding to the `Active Technologies` section the line: `Stripe.net 47.x, Stripe webhook signature verification, Checkout Sessions (011-stripe-payment)` (the agent-context script already added a partial entry; verify and clean it up)

### Verification gates (NON-NEGOTIABLE per clean-architecture-skill §9 + constitution principle II)

- [X] T068 Run `dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental` — must report `0 Warning(s), 0 Error(s)`. Fix any error/warning and re-run until clean.
- [X] T069 Run `dotnet test source/MoriiCoffee.Application.Tests/MoriiCoffee.Application.Tests.csproj` — must report `Failed: 0`. Also run `dotnet test source/MoriiCoffee.Domain.Tests/MoriiCoffee.Domain.Tests.csproj` for the domain idempotency test.
- [X] T070 Run `dotnet ef migrations script --idempotent --output /tmp/morii-stripe-migration.sql --project source/MoriiCoffee.Infrastructure.Persistence --startup-project source/MoriiCoffee.Presentation` and visually inspect the migration script — confirm `Orders.PaymentStatus int NOT NULL DEFAULT 1`, the three new tables, and the four indexes are all present
- [ ] T071 Execute `quickstart.md` step 5 manually (curl-based E2E test with test card `4242 4242 4242 4242` against a local stack with `stripe listen` running) and capture the JSON responses to confirm `paymentStatus: "Paid"` after the success webhook. Record any deviations against `tasks/lessons.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No prior dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1. **BLOCKS** every user story.
- **Phase 3 (US1)**: Depends on Phase 2 complete.
- **Phase 4 (US2)**: Depends on Phase 2 complete. Stories 1 and 2 share `HandleWebhookEventCommandHandler.cs`, so US2's modifications come after US1's initial implementation if done sequentially. Otherwise they can be coordinated in the same PR.
- **Phase 5 (US3)**: Depends on Phase 2 complete; also extends `PaymentsController.cs` (US1 already created it) and `HandleWebhookEventCommandHandler.cs` (US2 already touched it). If parallel, US3 must rebase on US1+US2 once those land.
- **Phase 6 (US4)**: Depends on Phase 2 complete. Logically independent of US1/US2/US3 because it only adds tests against the existing COD path.
- **Phase 7 (Polish)**: Depends on every user story scheduled for this delivery being complete.

### User Story Dependencies (in this feature)

- **US1**: Independent. Can ship as MVP without US2/US3/US4 if needed (US1 customers would be at higher risk for stuck-Pending orders if a webhook is missed — exactly what US2 protects).
- **US2**: Sits on the same files as US1 but its behaviour is additive (idempotency + extra event types). Together with US1 forms the production-grade payment flow. Recommended to ship together.
- **US3**: Independent of US1/US2 in terms of *running* code, but in practice is only useful once US1 + US2 are live (no paid orders to refund otherwise).
- **US4**: Strictly a non-regression guarantee for the existing COD flow. Can ship in any order; the test tasks here just lock-in the behaviour.

### Within Each User Story

- DTO/abstraction definition → handler implementation → controller action → unit tests → run tests.
- For the webhook handler (touched by US1, US2, US3), edits are sequential by phase to avoid merge conflicts.

### Parallel Opportunities

- **Phase 1**: T002 and T003 can run in parallel.
- **Phase 2 enums**: T004, T005, T006, T007 are four different files — all parallel.
- **Phase 2 entities**: T009 (RefundRecord) and T010 (PaymentWebhookEvent) are parallel; T011 (Payment) depends on neither but references the same aggregate folder, so safe in parallel.
- **Phase 2 repository interfaces**: T013, T014 — parallel.
- **Phase 2 abstraction + DTOs**: T015, T016 — parallel with each other.
- **Phase 2 EF configurations**: T020, T021, T022 — three different files, parallel.
- **Phase 2 repository implementations**: T023, T024 — parallel.
- **US1 tests**: T038, T039, T040 — three new test files, parallel.
- **US2 tests**: T048, T049, T050 — three new test files, parallel; T047 modifies an existing file so sequential w.r.t. T040.
- **US3 tests**: T056, T057 — parallel.
- **US4 tests**: T060, T061 — parallel.
- **Polish docs**: T063, T064, T065, T066, T067 — five different files, all parallel.

### Parallel Example: kicking off Phase 2 in one batch

```bash
# Enums (all different files, no inter-dependency)
Task T004: Create EPaymentStatus.cs
Task T005: Create EPaymentTransactionStatus.cs
Task T006: Create ERefundStatus.cs
Task T007: Create EPaymentWebhookProcessingResult.cs

# Entities + abstractions (waves)
Wave A (after enums): T008 (EPaymentMethod modify), T009 (RefundRecord), T010 (PaymentWebhookEvent), T013, T014, T015, T016
Wave B (after Wave A): T011 (Payment aggregate root references its children), T012 (Order extension)

# Persistence (after T011 / T012)
Wave C parallel: T020, T021, T022 (EF configs), T023, T024 (repositories)
Wave D: T025 → T026 (UoW + DbContext, both modify shared files)
Wave E: T027 (DbContext final), T028 (migration generated last)

# Infrastructure (after T015)
Wave F parallel: T017 (StripePaymentGateway), T018 (ConfigureStripeOptions), T019 (DI)

# Webhook skeleton (after T015 + T030 abstractions)
Wave G: T029 (controller), T030 (command + handler skeleton)

# Phase 2 build gate
T031: dotnet build (must pass before any US starts)
```

---

## Implementation Strategy

### MVP First (US1 only)

1. **Phase 1** (T001 → T003) — get Stripe.net wired up.
2. **Phase 2** (T004 → T031) — domain, persistence, gateway, webhook skeleton. Run `dotnet build` cleanly before stopping.
3. **Phase 3** (T032 → T041) — the customer-facing card payment path. Run `quickstart.md` step 5 manually.
4. **STOP** and validate: a real test-mode card payment completes end-to-end and the order shows `Paid`.

This MVP is shippable to a small private beta with the understanding that webhook reliability (US2) is still bare-bones (no idempotency yet — if Stripe retries, we'd double-process). For public launch, complete US2 first.

### Recommended sequencing

US1 + US2 ship together → US3 → US4 → Polish. They're small enough that a single developer can complete the full feature in one focused sprint:

- Day 1: Phase 1 + Phase 2 (entities, persistence, migration, webhook skeleton) + build green.
- Day 2: US1 + US2 (Checkout session creation + happy-path webhook + idempotency + expired/failed handling) + tests green.
- Day 3: US3 (refunds + charge.refunded webhook) + US4 (COD non-regression) + tests green.
- Day 4: Polish (docs in ENG + VN + summaries + manual quickstart verification + final build/test gate).

### Parallel Team Strategy

With two developers:

- Both: Phase 1 + Phase 2 together (foundations are small and shared).
- Then: Dev A → US1 + US2 (webhook handler is one file, easier in one head); Dev B → US3 (refunds) + US4 (COD tests). Polish split: ENG docs vs VN docs.

---

## Notes

- `[P]` tasks ⇒ different files, no incomplete dependencies — safe to run in parallel.
- `[USx]` label maps tasks to spec.md user stories for traceability.
- Verify each unit test fails (red) before its production code is written, or write them together with code — both acceptable; the gate is `dotnet test` green at each story checkpoint.
- Commit after each task or each logical group (constitution principle II + CLAUDE.md §4 "Git Discipline").
- After every correction the user makes during implementation, update `tasks/lessons.md` (constitution principle V).
- No task is "done" until both build and tests are clean (clean-architecture-skill §9).

## Task Count Summary

| Phase | Tasks | Notes |
|---|---|---|
| Phase 1 — Setup | 3 (T001–T003) | |
| Phase 2 — Foundational | 28 (T004–T031) | Includes a build-gate task |
| Phase 3 — US1 (card payment) | 10 (T032–T041) | Includes 3 test files |
| Phase 4 — US2 (webhook reliability) | 10 (T042–T051) | Includes 4 test files |
| Phase 5 — US3 (refunds) | 7 (T052–T058) | Includes 2 test files |
| Phase 6 — US4 (COD non-regression) | 4 (T059–T062) | Includes 2 test files |
| Phase 7 — Polish | 9 (T063–T071) | 4 docs + 1 CLAUDE.md edit + 3 verification + 1 manual |
| **Total** | **71** | |

**Parallel opportunities**: 32 of the 71 tasks are marked `[P]`. With perfect parallelism the critical path is ~25 sequential tasks.

**Independent MVP**: US1 alone (Phase 1 + Phase 2 + Phase 3, tasks T001–T041) — 41 tasks, ~3 days of focused work.
