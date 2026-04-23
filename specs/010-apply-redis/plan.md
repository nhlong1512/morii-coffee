# Implementation Plan: Redis-Backed Core Flows

**Branch**: `010-apply-redis` | **Date**: 2026-04-24 | **Spec**: [spec.md](/Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee/specs/010-apply-redis/spec.md)
**Input**: Feature specification from `/specs/010-apply-redis/spec.md`

## Summary

Introduce Redis as a focused supporting store for three bounded flows in the existing MoriiCoffee API: cache hot product reads with explicit invalidation, add an authenticated-user cart that persists across app restarts, and replace exposed password reset tokens with opaque one-time reset tickets. The design stays aligned with the current Clean Architecture layout by adding thin application abstractions, Redis-backed infrastructure services, targeted controller/endpoints for cart, and limited updates to existing product and auth handlers.

## Technical Context

**Language/Version**: C# on .NET 10.0  
**Primary Dependencies**: ASP.NET Core Web API, MediatR, ASP.NET Identity, Entity Framework Core, AutoMapper, Serilog, StackExchange.Redis, Microsoft.Extensions.Caching.StackExchangeRedis  
**Storage**: SQL Server as system of record, Redis for ephemeral cache/session data, S3-compatible object storage for media  
**Testing**: xUnit, FluentAssertions, NSubstitute, existing Application and Domain test projects  
**Target Platform**: Linux-hosted web API with local Docker-based development services  
**Project Type**: Clean Architecture web service  
**Performance Goals**: Repeated catalog reads complete within 1 second for 95% of requests; cart reads/mutations feel immediate for authenticated users; reset tickets remain single-use and short-lived  
**Constraints**: Preserve current API layering, avoid generic cache behavior without explicit invalidation rules, keep catalog reads available when Redis is unavailable, avoid account enumeration in password reset, keep scope limited to authenticated carts only  
**Scale/Scope**: One API solution with five backend projects, targeted changes in product read/write flows, new cart slice, and auth recovery redesign

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- `Plan Mode Default`: Satisfied. This feature spans architecture, new storage, new endpoints, and cross-layer changes, so planning artifacts are required before implementation.
- `Verification Before Done`: Satisfied. Plan includes unit coverage for catalog cache behavior, cart lifecycle, and reset-ticket consumption plus manual/API smoke checks.
- `Simplicity First & Minimal Impact`: Satisfied. Design uses targeted Redis abstractions instead of a global MediatR cache pipeline or broader session platform.
- `Subagent Strategy & Delegation`: Satisfied without delegation. No subagents were used because this turn did not authorize delegated agent work.
- `Self-Improvement Loop`: No correction-triggered update required in this turn.
- `Autonomous Execution with Concise Communication`: Satisfied. Planning artifacts are produced directly from the current repository state.

**Gate Result**: PASS

## Project Structure

### Documentation (this feature)

```text
specs/010-apply-redis/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── redis-core-flows.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Application/
│   ├── Commands/
│   │   ├── Auth/
│   │   ├── Product/
│   │   └── ProductVariant/
│   ├── Queries/
│   │   └── Product/
│   └── SeedWork/
│       ├── Abstractions/
│       └── DTOs/
├── MoriiCoffee.Application.Tests/
│   ├── Commands/
│   └── Queries/
├── MoriiCoffee.Domain/
│   ├── Aggregates/
│   ├── Repositories/
│   └── SeedWork/
├── MoriiCoffee.Domain.Shared/
│   ├── Constants/
│   └── Settings/
├── MoriiCoffee.Infrastructure/
│   ├── Configurations/
│   ├── Services/
│   │   ├── Email/
│   │   └── Redis/
│   └── DependencyInjection.cs
├── MoriiCoffee.Infrastructure.Persistence/
│   ├── Configurations/
│   ├── Data/
│   └── Repositories/
└── MoriiCoffee.Presentation/
    ├── Controllers/
    ├── Extensions/
    ├── Program.cs
    └── appsettings.json

deploy/
└── docker-compose.development.yml
```

**Structure Decision**: Keep the existing backend-only Clean Architecture structure. Redis-facing interfaces live in `MoriiCoffee.Application/SeedWork/Abstractions`, implementations in `MoriiCoffee.Infrastructure/Services/Redis`, HTTP contracts/controllers in `MoriiCoffee.Presentation`, and cache invalidation hooks stay close to affected command/query handlers.

## Phase 0 Research Summary

- Use explicit Redis-backed services instead of a generic cross-cutting cache pipeline.
- Keep catalog caching read-through and invalidate on successful write completion only.
- Store cart state as one JSON document per authenticated user with TTL refresh on mutation.
- Model password recovery as opaque reset tickets with one-time consumption semantics.
- Treat Redis as optional for catalog acceleration but required for cart and reset-session flows.

## Phase 1 Design Summary

1. Add Redis configuration, DI registration, and startup connectivity logging in Infrastructure/Presentation.
2. Introduce focused abstractions:
   - `IProductCatalogCache`
   - `ICartService`
   - `IPasswordResetTicketStore`
3. Update product queries to read through Redis and product/product-variant/image write handlers to invalidate affected cache entries after commit.
4. Add cart DTOs, commands or service methods, and a `CartController` for authenticated cart operations.
5. Refactor forgot/reset password handlers to issue and consume opaque reset tickets while preserving the current anti-enumeration response behavior.
6. Add unit tests for cache hit/miss/fallback, cart lifecycle rules, and one-time reset ticket consumption.

## Phase 2 Implementation Preview

1. Redis foundation
   - Add packages and settings
   - Register multiplexer/database/services
   - Add Docker development service and startup validation
2. Catalog caching
   - Cache paginated lists and product detail DTOs
   - Track list keys for safe invalidation
   - Add fallback logging and tests
3. Authenticated cart
   - Add cart schemas, controller, and Redis service
   - Validate variants before add/update
   - Clear cart after successful checkout integration point
4. Password reset tickets
   - Replace exposed reset token flow with opaque ticket lookup
   - Preserve one-time use and expiry
   - Keep public responses privacy-safe

## Post-Design Constitution Check

- `Verification Before Done`: Still satisfied. Design artifacts define concrete automated and manual verification points.
- `Simplicity First & Minimal Impact`: Still satisfied. Redis is applied only to three explicitly scoped flows; no unrelated architectural expansion was introduced.
- `Plan Mode Default`: Still satisfied. The plan remains the controlling artifact for implementation sequencing.

**Post-Design Gate Result**: PASS

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
