# Implementation Plan: GHN Sandbox Integration

**Branch**: `016-ghn-integration` | **Date**: 2026-05-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-ghn-integration/spec.md`

## Summary

Implement a sandbox-only GHN shipping module that upgrades Morii's current flat delivery flow into a structured delivery, quoting, shipment, and tracking flow.

The technical approach is:

- extend the current order and saved delivery profile model from free-text delivery data to a structured delivery snapshot with province, district, and ward identifiers
- introduce a dedicated GHN integration slice in the backend so the frontend talks only to Morii-native contracts for master data, quotes, shipment lifecycle, and webhook-driven status sync
- keep order creation as the source of truth for commerce while creating GHN shipments as a follow-up backend step that is idempotent and non-blocking for the order itself
- reuse the repository's existing patterns for external provider orchestration, webhook auditing, transaction boundaries, and admin-only operational actions
- deliver phase 1 as sandbox-only, with no multi-provider abstraction and no production credential rollout

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) across backend projects  
**Primary Dependencies**: ASP.NET Core Web API, MediatR, FluentValidation, AutoMapper, EF Core 10, Npgsql, Swashbuckle, existing cache abstractions, existing payment/webhook patterns  
**Storage**: PostgreSQL via EF Core + Npgsql for order, shipment, master-data cache, and webhook audit persistence; existing cache layer for short-lived quote and route support data  
**Testing**: xUnit + Moq + FluentAssertions in `MoriiCoffee.Application.Tests`, with handler, validator, mapping, and controller authorization coverage  
**Target Platform**: ASP.NET Core backend service in Docker, consumed by Morii customer and admin frontends  
**Project Type**: Backend web service with external carrier integration and documented frontend-facing contracts  
**Performance Goals**:
- shipping master-data reads <= 1 second p95 from warm cache
- quote response <= 5 seconds p95 for valid GHN delivery requests
- automatic shipment creation <= 10 seconds p95 after eligible order creation
- webhook/manual sync updates reflected on subsequent reads within 1 minute
**Constraints**:
- must preserve current Clean Architecture layering and current `ApiResponse` envelope style
- must keep shipment state separate from order fulfilment state and payment state
- must keep order creation successful even if GHN shipment creation fails afterwards
- must use GHN sandbox only in this release
- must not require frontend direct calls to GHN or expose GHN credentials publicly
**Scale/Scope**:
- low admin concurrency, moderate checkout traffic, and bursty webhook deliveries
- phase scope covers master data, quote, structured delivery profile, order placement extensions, shipment creation, shipment read models, admin actions, and GHN webhook ingestion
- implementation is expected to touch 30-45 files across Domain, Application, Infrastructure.Persistence, Infrastructure, Presentation, tests, and docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance check | Status |
|---|---|---|
| **I. Plan Mode Default** | This is a multi-step, cross-layer external integration with domain model changes, async webhook processing, admin operations, and new persistence. Planning is required and is being completed before implementation. | ✅ |
| **II. Verification Before Done** | Implementation will require handler/unit tests, controller authorization checks, build validation, and endpoint/workflow verification for quote, shipment creation, and webhook sync. Quickstart defines the verification path. | ✅ |
| **III. Simplicity First & Minimal Impact** | The design extends existing order and delivery concepts, reuses current MediatR and webhook patterns, and avoids introducing a generic shipping platform or a separate integration service. | ✅ |
| **IV. Subagent Strategy & Delegation** | No subagents were required for this planning pass. `code-review-graph` was used to keep exploration focused on current order, payment, delivery profile, and webhook surfaces. | ✅ |
| **V. Self-Improvement Loop** | No user correction occurred in this planning pass. If corrections arise during implementation, they will be captured in `tasks/lessons.md`. | ✅ |
| **VI. Autonomous Execution with Concise Communication** | The planning artefacts are being generated end-to-end without blocking on avoidable questions, while explicitly calling out the bounded release assumptions. | ✅ |
| **Tech stack constraints** | The design stays on the existing .NET / ASP.NET Core / EF Core / PostgreSQL stack and reuses current infrastructure patterns. | ✅ |
| **Minimal impact to existing features** | Pickup flow, Stripe payment-first flow, and existing order retrieval remain intact; new behavior is additive around GHN delivery orders only. | ✅ |

**Result (pre-design)**: No constitutional violations. No entries in *Complexity Tracking*.

### Post-design re-evaluation

After Phase 1 artefacts (`research.md`, `data-model.md`, `contracts/*`, `quickstart.md`) were authored:

| Principle | Re-check finding |
|---|---|
| Simplicity / Minimal impact | Confirmed. The plan extends current order and delivery profile models instead of adding a parallel checkout domain, and introduces GHN-specific services only where integration is required. |
| Verification before done | Confirmed. The quickstart defines unit, build, and endpoint-level verification for quote, shipment lifecycle, and webhook sync. |
| Layering discipline | Confirmed. Public/admin HTTP contracts remain in Presentation, orchestration in Application, shipment/address entities and enums in Domain, and GHN HTTP/persistence work in Infrastructure layers. |
| Controlled external integration | Confirmed. GHN quirks are isolated behind Morii-native contracts and adapter services rather than leaking provider payloads into controllers or frontend consumers. |

No new constitutional violations were introduced by the design.

## Project Structure

### Documentation (this feature)

```text
specs/016-ghn-integration/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── README.md
│   ├── shipping-master-data.md
│   ├── shipping-quotes.md
│   └── order-shipment-lifecycle.md
└── tasks.md                # Phase 2 output — produced by /speckit-tasks
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Domain/
│   ├── Aggregates/
│   │   ├── OrderAggregate/
│   │   ├── ShippingAggregate/
│   │   └── UserAggregate/
│   ├── Repositories/
│   │   ├── IShipmentRepository.cs
│   │   ├── IShippingMasterDataRepository.cs
│   │   └── IShipmentWebhookEventRepository.cs
│   └── Shared/
│       └── Enums/
├── MoriiCoffee.Application/
│   ├── Commands/
│   │   ├── Order/
│   │   ├── Shipping/
│   │   └── User/
│   ├── Queries/
│   │   ├── Order/
│   │   └── Shipping/
│   ├── SeedWork/
│   │   └── DTOs/
│   │       ├── Order/
│   │       ├── Shipping/
│   │       └── User/
│   └── Services/
│       └── Shipping/
├── MoriiCoffee.Infrastructure.Persistence/
│   ├── Configurations/
│   ├── Data/
│   ├── Migrations/
│   ├── Repositories/
│   └── SeedWork/
├── MoriiCoffee.Infrastructure/
│   ├── Configurations/
│   └── Services/
│       └── Shipping/
├── MoriiCoffee.Presentation/
│   └── Controllers/
│       ├── OrdersController.cs
│       ├── PaymentsController.cs
│       ├── ShippingController.cs
│       └── ShippingWebhookController.cs
└── MoriiCoffee.Application.Tests/
    ├── Commands/
    │   ├── Order/
    │   ├── Shipping/
    │   └── User/
    ├── Queries/
    │   └── Shipping/
    └── Presentation/
        └── ShippingAuthorizationTests.cs
```

**Structure Decision**: Keep the existing single backend solution structure and add a dedicated shipping integration slice that spans the current Clean Architecture layers. Order placement and payment flows stay where they already live; GHN-specific orchestration is added as a focused shipping module instead of creating a separate service or a generic multi-provider abstraction.

## Complexity Tracking

No constitutional violations to justify. Section intentionally empty.
