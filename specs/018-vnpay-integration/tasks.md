# Tasks: VNPAY Integration

**Input**: Design documents from `/specs/018-vnpay-integration/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Automated tests are required by the feature specification. Write story tests first and confirm they fail before implementing the corresponding behavior.

**Organization**: Tasks are grouped by user story. The provider-neutral payment migration is foundational because every VNPAY story depends on trustworthy provider ownership and routing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it targets different files and has no dependency on another incomplete task in the phase.
- **[Story]**: Maps the task to a user story from `spec.md`.
- Every task includes an exact file or directory path.

## Phase 1: Setup

**Purpose**: Establish configuration, test structure, and implementation guardrails without changing payment behavior.

- [X] T001 Review `tasks/lessons.md` and record the implementation checklist for feature `018-vnpay-integration` in `tasks/todo.md`
- [X] T002 [P] Add VNPAY sandbox configuration contract with empty secret placeholders in `source/MoriiCoffee.Domain.Shared/Settings/VnpaySettings.cs` and `source/MoriiCoffee.Presentation/appsettings.json`
- [X] T003 [P] Add VNPAY settings binding and startup diagnostics registration skeletons in `source/MoriiCoffee.Infrastructure/Configurations/VnpayConfiguration.cs` and `source/MoriiCoffee.Infrastructure/Services/Payment/VnpayStartupDiagnosticsService.cs`
- [ ] T004 [P] Create VNPAY payment test directories and shared fixtures in `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/` and `source/MoriiCoffee.Application.Tests/Commands/Payment/VnpayTestData.cs`
- [X] T005 Capture the pre-change baseline by running `rtk dotnet build MoriiCoffee.slnx` and `rtk dotnet test MoriiCoffee.slnx`, then record results in `tasks/todo.md`

---

## Phase 2: Foundational Provider-Neutral Payment Migration

**Purpose**: Remove Stripe-only assumptions from shared payment state, routing, persistence, and finalization before implementing any VNPAY user story.

**CRITICAL**: This phase blocks all user stories. Code-review-graph reports high blast radius across shared payment abstractions, so Stripe/COD regression verification is mandatory at the checkpoint.

### Foundational Tests

- [X] T006 [P] Add provider-neutral payment aggregate and order payment snapshot tests in `source/MoriiCoffee.Domain.Tests/Payment/ProviderNeutralPaymentTests.cs`
- [ ] T007 [P] Add provider-neutral checkout draft finalization tests for Stripe, VNPAY, and COD non-regression in `source/MoriiCoffee.Application.Tests/Services/CheckoutDraftServiceTests.cs`
- [X] T008 [P] Add gateway resolver routing tests for Stripe/VNPAY/unsupported providers in `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/PaymentGatewayResolverTests.cs`
- [ ] T009 [P] Add provider-neutral webhook audit idempotency and normalized event dispatch tests in `source/MoriiCoffee.Application.Tests/Commands/Payment/HandleWebhookEventProviderNeutralTests.cs`
- [ ] T010 [P] Add provider-neutral payment history and refund DTO mapping regression tests in `source/MoriiCoffee.Application.Tests/Queries/Payment/GetPaymentByOrderIdQueryHandlerTests.cs` and `source/MoriiCoffee.Application.Tests/Commands/Payment/RefundPaymentCommandHandlerTests.cs`

### Foundational Implementation

- [X] T011 Add `VNPAY`, `EPaymentProvider`, and normalized provider event kinds in `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentMethod.cs`, `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentProvider.cs`, and `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentProviderEventKind.cs`
- [ ] T012 Generalize order successful-payment identifiers and payment transition methods in `source/MoriiCoffee.Domain/Aggregates/OrderAggregate/Order.cs`
- [X] T013 Generalize payment attempt identity, provider diagnostics, and transition methods in `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Payment.cs`
- [X] T014 [P] Generalize webhook audit provider/event identity in `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Entities/PaymentWebhookEvent.cs`
- [X] T015 [P] Generalize refund provider identity and response fields in `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Entities/RefundRecord.cs`
- [X] T016 Generalize payment and webhook repository contracts in `source/MoriiCoffee.Domain/Repositories/IPaymentRepository.cs` and `source/MoriiCoffee.Domain/Repositories/IPaymentWebhookEventRepository.cs`
- [ ] T017 Rename Stripe checkout draft DTO/service abstractions to provider-neutral equivalents in `source/MoriiCoffee.Application/SeedWork/DTOs/Payment/CheckoutDraftCacheDto.cs`, `source/MoriiCoffee.Application/SeedWork/Abstractions/ICheckoutDraftService.cs`, and `source/MoriiCoffee.Application/Services/CheckoutDraftService.cs`
- [X] T018 Generalize hosted-payment gateway request/result models and add provider identity in `source/MoriiCoffee.Application/SeedWork/Abstractions/IPaymentGateway.cs`
- [X] T019 Add provider-based gateway resolver abstraction in `source/MoriiCoffee.Application/SeedWork/Abstractions/IPaymentGatewayResolver.cs`
- [X] T020 Update Stripe gateway and add resolver implementation without changing Stripe behavior in `source/MoriiCoffee.Infrastructure/Services/Payment/StripePaymentGateway.cs` and `source/MoriiCoffee.Infrastructure/Services/Payment/PaymentGatewayResolver.cs`
- [X] T021 Normalize provider event dispatch and remove raw Stripe event switching from application orchestration in `source/MoriiCoffee.Application/Commands/Payment/HandleWebhookEvent/HandleWebhookEventCommandHandler.cs`
- [ ] T022 Update Stripe checkout creation/reconcile handlers to use provider-neutral draft and gateway contracts in `source/MoriiCoffee.Application/Commands/Payment/CreateCheckoutSession/` and `source/MoriiCoffee.Application/Commands/Payment/ReconcileStripePayment/`
- [X] T023 Update refund, refund reconcile, payment history, and order read models to provider-neutral fields and provider routing in `source/MoriiCoffee.Application/Commands/Payment/RefundPayment/`, `source/MoriiCoffee.Application/Commands/Payment/ReconcileRefundPayment/`, `source/MoriiCoffee.Application/Queries/Payment/GetPaymentByOrderId/`, and `source/MoriiCoffee.Application/Queries/Order/GetOrderById/`
- [X] T024 Update provider-neutral payment/refund/order DTOs in `source/MoriiCoffee.Application/SeedWork/DTOs/Payment/` and `source/MoriiCoffee.Application/SeedWork/DTOs/Order/OrderDto.cs`
- [X] T025 Update EF Core configurations and repositories for provider-scoped lookups/indexes in `source/MoriiCoffee.Infrastructure.Persistence/Configurations/PaymentConfiguration.cs`, `source/MoriiCoffee.Infrastructure.Persistence/Configurations/PaymentWebhookEventConfiguration.cs`, `source/MoriiCoffee.Infrastructure.Persistence/Configurations/RefundRecordConfiguration.cs`, and `source/MoriiCoffee.Infrastructure.Persistence/Repositories/PaymentRepository.cs`
- [X] T026 Generate and review the provider-neutral backfill migration in `source/MoriiCoffee.Infrastructure.Persistence/Migrations/` so existing order/payment/refund/webhook rows are preserved as Stripe-owned
- [X] T027 Update payment service registrations and Stripe webhook controller resolution in `source/MoriiCoffee.Infrastructure/DependencyInjection.cs` and `source/MoriiCoffee.Presentation/Controllers/PaymentWebhookController.cs`
- [X] T028 Update all affected Stripe payment tests and fixtures for provider-neutral names in `source/MoriiCoffee.Application.Tests/Commands/Payment/`, `source/MoriiCoffee.Application.Tests/Services/`, and `source/MoriiCoffee.Application.Tests/Queries/Payment/`
- [X] T029 Run provider-neutral migration tests plus full Stripe/COD build and regression suite, then record the foundational checkpoint in `tasks/todo.md`

**Checkpoint**: Existing Stripe and COD flows pass using provider-neutral persistence, routing, drafts, events, history, and refunds.

---

## Phase 3: User Story 1 - Complete Checkout With VNPAY (Priority: P1) MVP

**Goal**: Allow authenticated customers to create a signed VNPAY checkout and finalize exactly one paid order from a verified authoritative IPN.

**Independent Test**: Create a VNPAY checkout from a valid cart, submit a valid successful IPN, and verify exactly one paid VNPAY order/payment with the authoritative amount; invalid and duplicate IPNs must create no duplicate paid order.

### Tests for User Story 1

- [X] T030 [P] [US1] Add canonical query, URL encoding, HMAC-SHA512 golden vector, amount scaling, and GMT+7 clock tests in `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/VnpaySignatureServiceTests.cs` and `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/VnpayClockTests.cs`
- [ ] T031 [P] [US1] Add payment URL creation handler tests for valid cart, empty cart, stale shipping quote, authoritative amount, and persisted draft ordering in `source/MoriiCoffee.Application.Tests/Commands/Payment/CreateVnpayPaymentUrlCommandHandlerTests.cs`
- [ ] T032 [P] [US1] Add IPN handler tests for valid success, invalid checksum, wrong terminal, unknown reference, amount mismatch, duplicate delivery, terminal failure, fraud, and paid-state preservation in `source/MoriiCoffee.Application.Tests/Commands/Payment/HandleVnpayIpnCommandHandlerTests.cs`
- [ ] T033 [P] [US1] Add VNPAY payment URL and IPN controller contract/authorization tests in `source/MoriiCoffee.Application.Tests/Presentation/VnpayPaymentAuthorizationTests.cs`

### Implementation for User Story 1

- [X] T034 [P] [US1] Implement shared VNPAY canonicalization and constant-time HMAC-SHA512 verification in `source/MoriiCoffee.Infrastructure/Services/Payment/VnpaySignatureService.cs`
- [X] T035 [P] [US1] Implement testable GMT+7 timestamp conversion and formatting in `source/MoriiCoffee.Infrastructure/Services/Payment/VnpayClock.cs`
- [X] T036 [P] [US1] Add provider-specific VNPAY request/response models in `source/MoriiCoffee.Infrastructure/Services/Payment/Models/`
- [X] T037 [US1] Implement VNPAY hosted payment URL creation and callback verification in `source/MoriiCoffee.Infrastructure/Services/Payment/VnpayPaymentGateway.cs`
- [X] T038 [US1] Register VNPAY settings, gateway, resolver entry, HTTP client, and startup diagnostics in `source/MoriiCoffee.Infrastructure/Configurations/VnpayConfiguration.cs` and `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`
- [X] T039 [US1] Implement create-payment-url command, validator, response DTO, and draft persistence in `source/MoriiCoffee.Application/Commands/Payment/CreateVnpayPaymentUrl/` and `source/MoriiCoffee.Application/SeedWork/DTOs/Payment/VnpayPaymentUrlResponseDto.cs`
- [X] T040 [US1] Implement authoritative VNPAY IPN command using normalized events, audit identity, amount verification, and idempotent finalization in `source/MoriiCoffee.Application/Commands/Payment/HandleVnpayIpn/`
- [X] T041 [US1] Expose authenticated payment-url and anonymous IPN routes with exact provider response casing in `source/MoriiCoffee.Presentation/Controllers/PaymentsController.cs` and `source/MoriiCoffee.Presentation/Controllers/VnpayCallbackController.cs`
- [X] T042 [US1] Verify VNPAY is treated as prepaid in shipping quote/fingerprint and shipment COD calculations in `source/MoriiCoffee.Application/Services/Shipping/ShippingQuoteFingerprintService.cs`, `source/MoriiCoffee.Application/Commands/Shipping/CreateShippingQuote/CreateShippingQuoteCommandHandler.cs`, and `source/MoriiCoffee.Application/Services/Shipping/ShipmentLifecycleService.cs`
- [X] T043 [US1] Run US1 tests and demonstrate valid, invalid, wrong-amount, and duplicate IPN behavior against the contract in `specs/018-vnpay-integration/contracts/callbacks.md`

**Checkpoint**: VNPAY checkout and authoritative IPN finalization work independently as the MVP.

---

## Phase 4: User Story 2 - Return Safely From VNPAY (Priority: P1)

**Goal**: Verify browser return information without mutating payment state and redirect customers with sanitized result data.

**Independent Test**: Submit valid success, pending, failed, invalid, and replayed return parameters and verify sanitized redirects with zero payment/order mutations.

### Tests for User Story 2

- [ ] T044 [P] [US2] Add return verification/status mapping tests in `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/VnpayReturnVerificationTests.cs`
- [ ] T045 [P] [US2] Add read-only return redirect and sanitized-query controller tests in `source/MoriiCoffee.Application.Tests/Presentation/VnpayReturnControllerTests.cs`

### Implementation for User Story 2

- [X] T046 [US2] Add return verification result mapping to the VNPAY gateway models in `source/MoriiCoffee.Infrastructure/Services/Payment/VnpayPaymentGateway.cs` and `source/MoriiCoffee.Infrastructure/Services/Payment/Models/`
- [X] T047 [US2] Implement the read-only return route and sanitized storefront redirect in `source/MoriiCoffee.Presentation/Controllers/VnpayCallbackController.cs`
- [X] T048 [US2] Run US2 tests and prove the return route performs no payment/order persistence using `specs/018-vnpay-integration/contracts/callbacks.md`

**Checkpoint**: Customer return is safe, sanitized, and independent from authoritative finalization.

---

## Phase 5: User Story 3 - Recover Missing Or Delayed Confirmation (Priority: P2)

**Goal**: Allow owners/admins to reconcile pending VNPAY attempts through verified QueryDR results.

**Independent Test**: Reconcile successful, pending, failed, invalid-signature, and unauthorized attempts; only a verified successful provider result may finalize exactly once.

### Tests for User Story 3

- [ ] T049 [P] [US3] Add QueryDR request signing and response verification tests in `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/VnpayQueryDrTests.cs`
- [ ] T050 [P] [US3] Add reconcile handler tests for local-finalized shortcut, success, pending, failure, invalid response, missing draft, and owner/admin authorization in `source/MoriiCoffee.Application.Tests/Commands/Payment/ReconcileVnpayPaymentCommandHandlerTests.cs`
- [ ] T051 [P] [US3] Add reconcile endpoint authorization/contract tests in `source/MoriiCoffee.Application.Tests/Presentation/VnpayReconcileAuthorizationTests.cs`

### Implementation for User Story 3

- [X] T052 [US3] Implement signed QueryDR request/response handling in `source/MoriiCoffee.Infrastructure/Services/Payment/VnpayPaymentGateway.cs` and `source/MoriiCoffee.Infrastructure/Services/Payment/Models/`
- [X] T053 [US3] Implement owner/admin-authorized VNPAY reconcile command and DTOs in `source/MoriiCoffee.Application/Commands/Payment/ReconcileVnpayPayment/` and `source/MoriiCoffee.Application/SeedWork/DTOs/Payment/`
- [X] T054 [US3] Expose the VNPAY reconcile endpoint in `source/MoriiCoffee.Presentation/Controllers/PaymentsController.cs`
- [X] T055 [US3] Run US3 tests and verify delayed-IPN recovery without duplicate finalization using `specs/018-vnpay-integration/contracts/reconcile.md`

**Checkpoint**: Missing/delayed confirmations can be recovered securely by the owner or an admin.

---

## Phase 6: User Story 4 - Operate And Support VNPAY Payments (Priority: P2)

**Goal**: Provide provider-neutral payment history and provider-routed full/partial refund operations with accurate asynchronous states.

**Independent Test**: View Stripe and VNPAY payment histories, route refunds to the correct provider, reject disabled VNPAY refund capability, and reconcile pending refund outcomes.

### Tests for User Story 4

- [ ] T056 [P] [US4] Add provider-neutral payment history mapping tests including VNPAY diagnostic fields in `source/MoriiCoffee.Application.Tests/Queries/Payment/GetPaymentByOrderIdQueryHandlerTests.cs` and `source/MoriiCoffee.Application.Tests/Queries/Order/GetOrderByIdQueryHandlerTests.cs`
- [ ] T057 [P] [US4] Add VNPAY full/partial refund signing, response verification, and status mapping tests in `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/VnpayRefundTests.cs`
- [ ] T058 [P] [US4] Add provider-routed refund/reconcile handler tests for Stripe, VNPAY enabled, VNPAY disabled, pending, succeeded, and failed outcomes in `source/MoriiCoffee.Application.Tests/Commands/Payment/RefundPaymentCommandHandlerTests.cs` and `source/MoriiCoffee.Application.Tests/Commands/Payment/ReconcileRefundPaymentCommandHandlerTests.cs`

### Implementation for User Story 4

- [X] T059 [US4] Implement VNPAY full/partial refund request signing, capability gate, and verified response mapping in `source/MoriiCoffee.Infrastructure/Services/Payment/VnpayPaymentGateway.cs` and `source/MoriiCoffee.Infrastructure/Services/Payment/Models/`
- [X] T060 [US4] Route refund creation and reconciliation by persisted payment provider in `source/MoriiCoffee.Application/Commands/Payment/RefundPayment/RefundPaymentCommandHandler.cs`, `source/MoriiCoffee.Application/Commands/Payment/ReconcileRefundPayment/ReconcileRefundPaymentCommandHandler.cs`, and `source/MoriiCoffee.Application/SeedWork/Helpers/RefundStateReconciler.cs`
- [X] T061 [US4] Expose provider-neutral payment/refund fields from payment and order queries in `source/MoriiCoffee.Application/Queries/Payment/GetPaymentByOrderId/GetPaymentByOrderIdQueryHandler.cs` and `source/MoriiCoffee.Application/Queries/Order/GetOrderById/GetOrderByIdQueryHandler.cs`
- [X] T062 [US4] Update Swagger descriptions and admin payment/refund response contracts in `source/MoriiCoffee.Presentation/Controllers/PaymentsController.cs` and `source/MoriiCoffee.Application/SeedWork/DTOs/Payment/`
- [X] T063 [US4] Run US4 tests and verify payment history/refund behavior against `specs/018-vnpay-integration/contracts/payment-history-refunds.md`

**Checkpoint**: Admin/support workflows identify providers correctly and never route a payment to the wrong gateway.

---

## Phase 7: User Story 5 - Continue Frontend Delivery From A Stable Handoff (Priority: P3)

**Goal**: Deliver a verified handoff that enables the separate frontend team to implement VNPAY safely.

**Independent Test**: A frontend developer can identify checkout, redirect, pending storage, return polling, reconcile, history, refund display, security, and testing requirements without undocumented assumptions.

### Tasks for User Story 5

- [X] T064 [P] [US5] Reconcile implemented backend routes and DTOs against all files in `specs/018-vnpay-integration/contracts/`
- [X] T065 [P] [US5] Document frontend checkout selection, prepaid shipping mapping, redirect, pending storage, and retry behavior in `docs/features/vnpay-integration/FRONTEND_HANDOFF.md`
- [X] T066 [P] [US5] Document frontend return polling, authoritative reconcile behavior, customer/admin payment displays, i18n, and expected frontend tests in `docs/features/vnpay-integration/FRONTEND_HANDOFF.md`
- [X] T067 [US5] Review `docs/features/vnpay-integration/FRONTEND_HANDOFF.md` for secret exposure, browser-return trust, cart-clearing rules, and contract completeness

**Checkpoint**: Frontend handoff reflects verified backend behavior and security boundaries.

---

## Phase 8: Polish And Cross-Cutting Verification

**Purpose**: Complete security hardening, regression verification, sandbox evidence, and delivery documentation.

- [ ] T068 [P] Add startup/configuration validation and secret-safe logging tests in `source/MoriiCoffee.Application.Tests/Infrastructure/Payment/VnpayConfigurationTests.cs`
- [ ] T069 [P] Add callback logging, fingerprinting, and sensitive-data redaction tests in `source/MoriiCoffee.Application.Tests/Commands/Payment/HandleWebhookEventProviderNeutralTests.cs`
- [X] T070 Configure trusted forwarded-header behavior for customer IP capture and document deployment requirements in `source/MoriiCoffee.Presentation/Extensions/ApplicationExtensions.cs` and `docs/features/vnpay-integration/README.md`
- [X] T071 Run `rtk dotnet build MoriiCoffee.slnx`, `rtk dotnet test MoriiCoffee.slnx`, and `rtk git diff --check`; record zero-error evidence in `tasks/todo.md`
- [X] T072 Run code-review-graph change detection and impact-radius review for payment-owned changes, then record resolved risks and remaining test gaps in `tasks/todo.md`
- [ ] T073 Execute the sandbox acceptance flow from `specs/018-vnpay-integration/quickstart.md` and record evidence for success, failure, cancellation, duplicate IPN, reconciliation, and refunds when enabled
- [X] T074 Update implementation results, verification evidence, and deferred production rollout items in `tasks/todo.md` and `docs/features/vnpay-integration/README.md`

---

## Dependencies And Execution Order

### Phase Dependencies

- **Phase 1 Setup**: Starts immediately.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks every user story.
- **US1 Complete Checkout**: Starts after Phase 2 and is the MVP.
- **US2 Safe Return**: Starts after Phase 2; depends on the shared VNPAY signature verification primitives from US1 tasks T034-T037.
- **US3 Reconciliation**: Starts after Phase 2; depends on gateway resolver/finalization foundation and VNPAY gateway primitives from US1.
- **US4 Operations/Refunds**: Starts after Phase 2; provider-neutral history/refund foundations are already available, but VNPAY refund implementation depends on VNPAY gateway primitives.
- **US5 Frontend Handoff**: Depends on completed and verified backend contracts from US1-US4.
- **Phase 8 Polish**: Depends on all desired user stories.

### User Story Completion Order

```text
Setup
  -> Provider-Neutral Foundation
      -> US1 Complete Checkout With VNPAY (MVP)
          -> US2 Return Safely From VNPAY
          -> US3 Recover Missing/Delayed Confirmation
          -> US4 Operate And Support VNPAY Payments
              -> US5 Frontend Handoff
                  -> Polish And Sandbox Verification
```

### Parallel Opportunities

- T002-T004 can run in parallel after T001.
- T006-T010 can run in parallel before foundational implementation.
- T014-T015 can run in parallel after T013.
- T030-T033 can run in parallel after the foundation checkpoint.
- T034-T036 can run in parallel before T037.
- US2 tests, US3 tests, and US4 tests can be prepared in parallel after US1 gateway primitives stabilize.
- T056-T058 can run in parallel.
- T065-T066 can run in parallel after T064.
- T068-T069 can run in parallel before final verification.

## Parallel Execution Examples

### User Story 1

```text
Task T030: Protocol/signature/clock tests
Task T031: Payment URL command tests
Task T032: IPN handler tests
Task T033: Controller contract/authorization tests
```

Then:

```text
Task T034: Signature service
Task T035: VNPAY clock
Task T036: VNPAY protocol models
```

### User Stories 2-4

After US1 gateway primitives:

```text
Developer A: T044-T048 (US2 return)
Developer B: T049-T055 (US3 reconciliation)
Developer C: T056-T063 (US4 history/refunds)
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 setup.
2. Complete the provider-neutral foundation and prove Stripe/COD regression safety.
3. Complete US1 VNPAY checkout and authoritative IPN.
4. Stop and validate the MVP independently before adding return, reconcile, or refund behavior.

### Incremental Delivery

1. Provider-neutral foundation: no new customer feature, but all existing payment flows remain green.
2. US1: customers can complete VNPAY checkout through authoritative IPN.
3. US2: customers return safely with sanitized UI guidance.
4. US3: delayed/missing IPN can be reconciled securely.
5. US4: admins receive provider-neutral history and refund operations.
6. US5: frontend team receives a stable verified handoff.
7. Final verification: full build/tests, code-review-graph review, and sandbox evidence.

## Notes

- Do not implement VNPAY by storing its identifiers in Stripe-named fields.
- Do not mark payment paid from browser return.
- Do not trust frontend amount or arbitrary forwarded IP headers.
- Write story tests first and confirm they fail before implementation.
- Run the full Stripe/COD regression suite at the foundational checkpoint and before delivery.
- Keep production activation and non-PAY VNPAY products out of scope.
