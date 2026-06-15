# Implementation Plan: VNPAY Integration

**Branch**: `017-fix-refactor` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/018-vnpay-integration/spec.md`

## Summary

Implement VNPAY PAY v2.1.0 as a sandbox-first hosted payment option while preserving the existing payment-first checkout architecture and Stripe/COD behavior.

The technical approach is:

- migrate Stripe-specific payment, checkout-draft, webhook-audit, refund, order, repository, and read-model fields to provider-neutral contracts before adding VNPAY
- introduce a payment gateway resolver so checkout, reconciliation, refund, and provider-status operations route by persisted provider
- isolate VNPAY canonicalization, HMAC-SHA512 signing, GMT+7 timestamps, QueryDR, and refund protocol details in the Infrastructure layer
- treat the verified VNPAY IPN as authoritative, keep the browser return read-only, and reuse the existing idempotent payment finalization transaction
- preserve PostgreSQL audit/history data through an explicit EF Core migration that backfills existing rows as Stripe-owned
- deliver automated regression coverage, sandbox verification guidance, and a frontend handoff document under `docs/features/vnpay-integration/`

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) across backend projects  
**Primary Dependencies**: ASP.NET Core Web API, MediatR 14, FluentValidation 12, EF Core 10, Npgsql 10, existing Redis cache abstraction, existing Stripe.net integration, `HttpClient`, built-in cryptography APIs  
**Storage**: PostgreSQL for orders, payment attempts, webhook/IPN audits, and refund history; existing cache service for provider-neutral checkout drafts  
**Testing**: xUnit 2.9, Moq 4.20, FluentAssertions; handler, domain, gateway protocol, controller authorization, migration, and regression coverage  
**Target Platform**: Linux-hosted ASP.NET Core backend consumed by the separate Morii Coffee Next.js storefront/admin application  
**Project Type**: Backend web service with an external hosted-payment integration and documented frontend-facing contracts  
**Performance Goals**:
- create a signed VNPAY payment URL within 5 seconds p95 under normal conditions
- return recognized IPN outcomes within VNPAY retry expectations while finalizing each successful transaction exactly once
- expose authoritative reconcile state to the customer within 10 seconds after return under normal provider conditions
**Constraints**:
- IPN is authoritative; browser return must never mark payment paid
- amount must be calculated from the authenticated cart and validated shipping quote
- VNPAY signatures, terminal code, and hash secret remain backend-only
- VNPAY amount multiplication by `100` occurs exactly once; Stripe VND remains zero-decimal
- VNPAY timestamps use GMT+7 and provider-required formats
- migration must preserve existing Stripe rows, indexes, refunds, and webhook audit history
- existing Stripe and COD behavior must remain unchanged
- first release is VNPAY PAY sandbox only; token, installment, recurring, and production activation are excluded
**Scale/Scope**:
- moderate checkout traffic, bursty duplicate callback delivery, and low admin refund/reconcile concurrency
- expected to touch 45-70 backend files across Domain.Shared, Domain, Application, Infrastructure.Persistence, Infrastructure, Presentation, tests, specs, and frontend handoff docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance check | Status |
|---|---|---|
| **I. Plan Mode Default** | This is a cross-layer payment integration with migrations, cryptographic protocol handling, callbacks, refunds, and regression risk. Planning artifacts are completed before implementation. | PASS |
| **II. Verification Before Done** | Quickstart requires full build/test, protocol golden vectors, authorization/idempotency tests, migration verification, and sandbox evidence before delivery. | PASS |
| **III. Simplicity First & Minimal Impact** | The design extends the current payment-first flow and existing aggregates rather than creating a parallel payment subsystem. Provider-neutral changes are limited to payment-owned surfaces required by VNPAY. | PASS |
| **IV. Subagent Strategy & Delegation** | No subagents were requested or used. Code-review-graph was used for focused architecture and blast-radius analysis. | PASS |
| **V. Self-Improvement Loop** | The earlier phase-scope correction and branch/feature mismatch prevention rules were recorded in `tasks/lessons.md`. | PASS |
| **VI. Autonomous Execution with Concise Communication** | Planning artifacts are generated end-to-end without implementation or avoidable user blocking. | PASS |
| **Tech stack constraints** | Remains on the repository's .NET 10 Clean Architecture stack and uses built-in cryptography/HTTP primitives. | PASS |
| **Minimal impact to existing features** | Stripe and COD remain supported through the same public routes and behavior, with mandatory regression tests. | PASS |

**Pre-design gate result**: PASS. No constitutional violations require justification.

### Post-Design Re-evaluation

| Principle | Re-check finding |
|---|---|
| Simplicity / minimal impact | Provider-neutral migration is justified because all existing payment-owned entities and handlers use Stripe-specific identities; adding VNPAY without migration would create ambiguous history and unsafe refund routing. |
| Verification before done | Contracts and quickstart define protocol, migration, regression, callback, reconcile, refund, and sandbox evidence gates. |
| Layering discipline | Domain owns provider-neutral state, Application owns orchestration, Infrastructure owns VNPAY protocol/signing, Persistence owns migration/indexes, and Presentation owns HTTP contracts. |
| Controlled external integration | Frontend never signs requests or receives secrets; VNPAY-specific payloads do not leak into application/domain contracts. |

**Post-design gate result**: PASS. No constitutional violations introduced.

## Project Structure

### Documentation (this feature)

```text
specs/018-vnpay-integration/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── README.md
│   ├── create-payment-url.md
│   ├── callbacks.md
│   ├── reconcile.md
│   └── payment-history-refunds.md
└── tasks.md                     # Produced by /speckit-tasks
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Domain.Shared/
│   ├── Enums/Order/
│   │   ├── EPaymentMethod.cs
│   │   ├── EPaymentProvider.cs
│   │   └── EPaymentProviderEventKind.cs
│   └── Settings/VnpaySettings.cs
├── MoriiCoffee.Domain/
│   ├── Aggregates/
│   │   ├── OrderAggregate/Order.cs
│   │   └── PaymentAggregate/
│   │       ├── Payment.cs
│   │       └── Entities/
│   │           ├── PaymentWebhookEvent.cs
│   │           └── RefundRecord.cs
│   └── Repositories/
│       ├── IPaymentRepository.cs
│       └── IPaymentWebhookEventRepository.cs
├── MoriiCoffee.Application/
│   ├── Commands/Payment/
│   │   ├── CreateCheckoutSession/
│   │   ├── CreateVnpayPaymentUrl/
│   │   ├── HandleWebhookEvent/
│   │   ├── HandleVnpayIpn/
│   │   ├── ReconcileStripePayment/
│   │   ├── ReconcileVnpayPayment/
│   │   ├── RefundPayment/
│   │   └── ReconcileRefundPayment/
│   ├── Queries/Payment/GetPaymentByOrderId/
│   ├── Services/CheckoutDraftService.cs
│   └── SeedWork/
│       ├── Abstractions/
│       │   ├── IPaymentGateway.cs
│       │   ├── IPaymentGatewayResolver.cs
│       │   └── ICheckoutDraftService.cs
│       └── DTOs/Payment/
├── MoriiCoffee.Infrastructure.Persistence/
│   ├── Configurations/
│   ├── Migrations/
│   └── Repositories/
├── MoriiCoffee.Infrastructure/
│   ├── Configurations/VnpayConfiguration.cs
│   └── Services/Payment/
│       ├── PaymentGatewayResolver.cs
│       ├── StripePaymentGateway.cs
│       ├── VnpayPaymentGateway.cs
│       ├── VnpaySignatureService.cs
│       ├── VnpayClock.cs
│       ├── VnpayStartupDiagnosticsService.cs
│       └── Models/
├── MoriiCoffee.Presentation/
│   ├── Controllers/
│   │   ├── PaymentsController.cs
│   │   ├── PaymentWebhookController.cs
│   │   └── VnpayCallbackController.cs
│   └── appsettings.json
└── MoriiCoffee.Application.Tests/
    ├── Commands/Payment/
    ├── Services/
    ├── Presentation/
    └── Infrastructure/Payment/

docs/features/vnpay-integration/
├── README.md
└── FRONTEND_HANDOFF.md          # Implementation-phase deliverable
```

**Structure Decision**: Keep the existing single backend solution and payment-first checkout slice. Generalize only payment-owned abstractions and persistence, then add a VNPAY-specific Infrastructure adapter plus focused Application commands/controllers. The frontend remains outside this repository; only its handoff contract is delivered here.

## Complexity Tracking

No constitutional violations to justify.
