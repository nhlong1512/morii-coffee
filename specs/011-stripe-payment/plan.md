# Implementation Plan: Stripe Online Payment for Cart Checkout

**Branch**: `011-stripe-payment` | **Date**: 2026-05-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-stripe-payment/spec.md`

## Summary

Integrate Stripe as a second payment option (alongside the existing Cash-on-Delivery) for the order checkout flow.

The technical approach is:
- Use **Stripe Checkout Sessions** (Stripe-hosted card form) — keeps Morii Coffee out of the strictest PCI scope (SAQ-A) and is the simplest path for a beginner-friendly first integration. The customer is redirected to Stripe, then back to a success/cancel URL on the storefront.
- Persist a new `Payment` aggregate that records every Stripe Checkout Session created against an Order, plus all `RefundRecord` rows when admins refund.
- Listen for Stripe webhook events (`checkout.session.completed`, `checkout.session.expired`, `charge.refunded`, `payment_intent.payment_failed`) to drive the order's payment status. Webhook signature verification and idempotent processing (via a `WebhookEvent` audit table) guarantee correctness even when Stripe retries.
- Extend `Order` with a `PaymentStatus` field separate from the existing fulfilment status. Add a new `EPaymentMethod.STRIPE` value. Keep COD behaviour exactly as it is today (zero regression).
- Refund endpoint is admin-only, supports both full and partial refunds, and is auditable.
- Produce two beginner-friendly Markdown guides under `docs/` (English + Vietnamese — per `CLAUDE.md` workflow rule 7) plus a summary doc once implementation lands.

This plan creates only design artefacts (research, data model, contracts, quickstart). Implementation tasks are produced by `/speckit.tasks` next.

## Technical Context

**Language/Version**: C# / .NET 10.0 (`net10.0`) — confirmed in `MoriiCoffee.Presentation.csproj`. The constitution mentions .NET 8 but the repo has already migrated (feature `006-dotnet-10-upgrade`).
**Primary Dependencies**:
- **New**: `Stripe.net` 47.0.0+ (official Stripe .NET SDK, supports Checkout Sessions + Webhook signature verification + Refund API)
- **Existing (reused)**: MediatR 14.1.0 (CQRS), FluentValidation 12.1.1, AutoMapper 16.1.1, EF Core 10.0.5 (Npgsql), Serilog 4.3.1, Swashbuckle 6.7.2, Hangfire 1.8.23 (already in `Presentation.csproj` — not used by this feature but available)
**Storage**: PostgreSQL via EF Core 10.0.5 + Npgsql. Two new tables (`Payments`, `Refunds`, `PaymentWebhookEvents`). One column added to `Orders` (`PaymentStatus`).
**Testing**: xUnit 2.9.3 + Moq 4.20.72 + FluentAssertions 8.9.0 + MockQueryable.Moq 10.0.5 — same stack already used by `MoriiCoffee.Application.Tests` (per feature `008-unit-tests-setup`).
**Target Platform**: ASP.NET Core 10 web service running in Docker (per `deploy/run-docker-development.sh`).
**Project Type**: Backend web-service (Clean Architecture). Frontend changes are out of scope for this feature — the frontend team consumes the new API contracts.
**Performance Goals**:
- Create Checkout Session: ≤ 1.5 s p95 (dominated by Stripe API roundtrip).
- Process webhook event: ≤ 500 ms p95 (DB write + idempotency check).
- Issue refund: ≤ 2 s p95.
**Constraints**:
- Must work in environments where outbound HTTPS to `api.stripe.com` is reachable.
- Webhook endpoint must be reachable from Stripe (HTTPS in production, Stripe CLI tunnel in dev).
- Database writes during webhook handling must be transactional and idempotent.
- Stripe API key and webhook signing secret are read from configuration; **never** committed.
**Scale/Scope**:
- Expected load: ≤ 10 paid orders/minute during normal traffic; Stripe handles surge.
- Code scope: ~30–40 new files (1 aggregate, 3 EF configs, 1 migration, 5 commands + 5 handlers, 2 queries, 2 controllers, 1 infrastructure service, 1 webhook controller, ~15 unit-test files), plus 3 docs in `docs/`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance check | Status |
|---|---|---|
| **I. Plan Mode Default** | This plan exists; non-trivial multi-step feature. | ✅ |
| **II. Verification Before Done** | Plan mandates `dotnet build` + `dotnet test` green before any task is marked complete (FR-022, FR-023, SC-009). Quickstart script in Phase 1 spells out manual verification. | ✅ |
| **III. Simplicity First & Minimal Impact** | We reuse existing patterns (CQRS via MediatR, `IUnitOfWork`, `EntityBase`, `IEntityTypeConfiguration<T>`, FluentValidation, ApiResponse). No new framework or pattern introduced. New code is isolated to `Domain/Aggregates/PaymentAggregate`, `Application/Commands/Payment`, `Infrastructure/Services/Payment`, `Presentation/Controllers`. Existing `Order` aggregate gets a single new field (`PaymentStatus`). COD flow is left untouched. | ✅ |
| **IV. Subagent Strategy & Delegation** | Where useful (research on Stripe.net SDK quirks, large file reads), the implementation phase will dispatch focused subagents to keep the main context clean. | ✅ |
| **V. Self-Improvement Loop** | Any user correction during implementation lands in `tasks/lessons.md`. | ✅ |
| **VI. Autonomous Execution with Concise Communication** | Plan and following commands act, report, move on — no narration. | ✅ |
| **Tech stack constraint — backend Clean Architecture** | All new code respects layer boundaries (Domain → Application → Infrastructure ← Presentation). Stripe SDK is referenced **only** from `Infrastructure` (concrete `StripePaymentService`). Application + Domain depend only on an `IPaymentGateway` abstraction. | ✅ |
| **XML summary comments on every type/non-trivial member** *(clean-architecture-skill §1)* | Every new file will carry `/// <summary>` per the skill. | ✅ |
| **DataAnnotations on entity fields** *(clean-architecture-skill §2)* | `Payment`, `RefundRecord`, `PaymentWebhookEvent` entities will use `[Table]`, `[Key]`, `[Required]`, `[MaxLength]`, `[Column(TypeName)]` directly. | ✅ |
| **EF relationships in `Infrastructure.Persistence/Configurations/`** *(skill §3)* | Three new `IEntityTypeConfiguration<T>` classes will hold all relationship configuration. No fluent calls in the entity classes. | ✅ |
| **Migration created when schema changes** *(skill §5–§7)* | One new migration `AddStripePaymentSupport` covers all schema changes (Orders.PaymentStatus, Payments table, Refunds table, PaymentWebhookEvents table). | ✅ |
| **Build passes 0 warnings 0 errors** *(skill §9)* | Required exit gate in `/speckit.implement`. | ✅ |

**Result (pre-design)**: No violations. No entries in *Complexity Tracking*.

### Post-design re-evaluation

After Phase 1 artefacts (`research.md`, `data-model.md`, `contracts/*.md`, `quickstart.md`) were authored, the same gates were re-checked:

| Principle | Re-check finding |
|---|---|
| Simplicity / Minimal impact | Confirmed. The design adds exactly one new aggregate (`Payment`), one new column on `Order`, four new enums, one new repository pair, one new infrastructure service, two new controllers, and the migration. No existing handler is rewritten — `PlaceOrderCommandHandler` gets one line added via the existing `Order.Create` factory. |
| Stripe SDK isolation | Confirmed in `data-model.md` and `contracts/`. `Stripe.net` types appear only inside `Infrastructure/Services/Payment/StripePaymentGateway.cs`. The Application + Domain layers reference only the abstraction `IPaymentGateway` defined in `Application/SeedWork/Abstractions/`. |
| Idempotency strategy is data-model-backed | Confirmed. `PaymentWebhookEvents.StripeEventId` UNIQUE constraint plus `Order.MarkPaymentPaid` aggregate-level idempotency together satisfy SC-004 without app-level locks. |
| No card data in logs | Confirmed by R-013. Logging plan never references the event body, only event id and type. |
| Verification before done | `quickstart.md` defines manual E2E steps, `data-model.md` enumerates every unit test, and the existing `MoriiCoffee.Application.Tests` project is reused — no test infrastructure changes needed. |

No new constitutional violations introduced by the design. *Complexity Tracking* remains empty.

## Project Structure

### Documentation (this feature)

```text
specs/011-stripe-payment/
├── plan.md                 # This file
├── research.md             # Phase 0 output (decision log: SDK choice, hosted vs embedded, idempotency strategy, etc.)
├── data-model.md           # Phase 1 output (entities, fields, relationships, state machine)
├── quickstart.md           # Phase 1 output (engineer onboarding: get Stripe account → run locally → make a test payment)
├── contracts/              # Phase 1 output (OpenAPI snippets per new endpoint)
│   ├── README.md
│   ├── create-checkout-session.md
│   ├── webhook.md
│   ├── get-payment-by-order.md
│   └── refund.md
├── checklists/
│   └── requirements.md     # Already produced by /speckit.specify
└── tasks.md                # Phase 2 output — produced by /speckit.tasks, NOT this command
```

### Source Code (repository root) — additions only; existing files unchanged unless listed

```text
source/
├── MoriiCoffee.Domain/
│   └── Aggregates/
│       └── PaymentAggregate/                      # NEW aggregate
│           ├── Payment.cs                         # aggregate root: 1 row per Stripe Checkout Session
│           ├── Entities/
│           │   ├── RefundRecord.cs                # child entity
│           │   └── PaymentWebhookEvent.cs         # child entity (audit/idempotency)
│           └── Repositories/
│               ├── IPaymentRepository.cs          # extends IBaseRepository<Payment>
│               └── IPaymentWebhookEventRepository.cs
│
├── MoriiCoffee.Domain/
│   └── Aggregates/
│       └── OrderAggregate/
│           └── Order.cs                           # MODIFIED: add PaymentStatus + methods
│
├── MoriiCoffee.Domain.Shared/
│   └── Enums/
│       └── Order/
│           ├── EPaymentMethod.cs                  # MODIFIED: add STRIPE = 4
│           ├── EPaymentStatus.cs                  # NEW: NotRequired, Pending, Paid, Failed, Refunded, PartiallyRefunded
│           └── EPaymentWebhookProcessingResult.cs # NEW: Processed, Duplicate, SignatureInvalid, OrderNotFound, Failed
│   └── Settings/
│       └── StripeSettings.cs                      # NEW: SecretKey, PublishableKey, WebhookSigningSecret, SuccessUrl, CancelUrl, Currency, IsLiveMode
│
├── MoriiCoffee.Application/
│   ├── SeedWork/
│   │   ├── Abstractions/
│   │   │   └── IPaymentGateway.cs                 # NEW: abstraction over Stripe SDK (CreateCheckoutSession, GetSession, Refund)
│   │   └── DTOs/
│   │       └── Payment/
│   │           ├── CreateCheckoutSessionDto.cs
│   │           ├── CheckoutSessionResponseDto.cs
│   │           ├── PaymentDto.cs
│   │           ├── RefundDto.cs
│   │           └── RefundResponseDto.cs
│   ├── Commands/
│   │   └── Payment/
│   │       ├── CreateCheckoutSession/             # POST /api/v1/payments/checkout-session
│   │       ├── HandleWebhookEvent/                # POST /api/v1/payments/webhook
│   │       └── RefundPayment/                     # POST /api/v1/payments/{orderId}/refund (admin)
│   └── Queries/
│       └── Payment/
│           └── GetPaymentByOrderId/               # GET /api/v1/payments/by-order/{orderId}
│
├── MoriiCoffee.Infrastructure/
│   ├── Services/
│   │   └── Payment/
│   │       └── StripePaymentGateway.cs            # IPaymentGateway implementation using Stripe.net SDK
│   └── Configurations/
│       └── ConfigureStripeOptions.cs              # binds StripeSettings + registers IPaymentGateway
│   └── DependencyInjection.cs                     # MODIFIED: AddScoped<IPaymentGateway, StripePaymentGateway>()
│
├── MoriiCoffee.Infrastructure.Persistence/
│   ├── Configurations/
│   │   ├── PaymentConfiguration.cs                # NEW
│   │   ├── RefundRecordConfiguration.cs           # NEW
│   │   └── PaymentWebhookEventConfiguration.cs    # NEW
│   ├── Repositories/
│   │   ├── PaymentRepository.cs                   # NEW
│   │   └── PaymentWebhookEventRepository.cs       # NEW
│   ├── UnitOfWork/
│   │   └── UnitOfWork.cs                          # MODIFIED: expose IPaymentRepository, IPaymentWebhookEventRepository
│   ├── ApplicationDbContext.cs                    # MODIFIED: DbSet<Payment>, DbSet<PaymentWebhookEvent>
│   └── Migrations/
│       └── <timestamp>_AddStripePaymentSupport.cs # NEW
│
├── MoriiCoffee.Presentation/
│   ├── Controllers/
│   │   ├── PaymentsController.cs                  # NEW: customer endpoints + admin refund
│   │   └── PaymentWebhookController.cs            # NEW: POST /api/v1/payments/webhook (AllowAnonymous, raw body)
│   └── appsettings.json + appsettings.Development.json + appsettings.Production.json   # MODIFIED: add Stripe section with empty values in source control
│
└── MoriiCoffee.Application.Tests/
    └── Commands/
        └── Payment/
            ├── CreateCheckoutSessionCommandHandlerTests.cs
            ├── HandleWebhookEventCommandHandlerTests.cs
            └── RefundPaymentCommandHandlerTests.cs
    └── Queries/
        └── Payment/
            └── GetPaymentByOrderIdQueryHandlerTests.cs

docs/
├── docs/features/stripe-checkout/stripe-integration-guide-ENG.md  # NEW: beginner-friendly end-to-end guide (FR-022)
├── docs/features/stripe-checkout/stripe-integration-guide-VN.md   # NEW: same content in Vietnamese
└── explainations/
    ├── summary-stripe-payment-ENG.md              # NEW: final summary (FR-023) per CLAUDE.md §7
    └── summary-stripe-payment-VN.md               # NEW
```

**Structure Decision**: Clean Architecture / DDD module structure already in use in the repo. The feature creates **one new aggregate** (`PaymentAggregate`) so all Payment state changes happen through one root, and extends the existing `OrderAggregate` with the minimum required (a single new `PaymentStatus` field plus a couple of state-transition methods). The Stripe SDK lives only behind an `IPaymentGateway` abstraction in `Application`, with the concrete `StripePaymentGateway` in `Infrastructure` — Domain has zero knowledge of Stripe.

## Complexity Tracking

No constitutional violations to justify. Section intentionally empty.
