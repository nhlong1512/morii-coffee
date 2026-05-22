# Implementation Plan: Store Management

**Branch**: `015-store-management` | **Date**: 2026-05-22 | **Spec**: [spec.md](spec.md)

## Summary

Implement the full Store Management feature: a backend that exposes public store-locator endpoints (with geolocation sorting and structured opening hours) plus an admin CRUD API, and a frontend that replaces all static dummy store data with live API data, adds an interactive public `/stores` page with a Google Maps integration, and delivers a complete admin panel for managing store locations and ordering.

**Key technical approach** (from [research.md](research.md)):
- Backend follows Clean Architecture: `Store` aggregate root + `StoreOpeningHours` child entity, following the `Banner`/`Order` pattern exactly.
- Distance computation via Haversine formula in-memory (store count < 100 for MVP).
- Opening hours: exactly 7 records per store; replaced atomically on full update.
- Frontend: new `src/features/stores/` folder following the `blogs` pattern; admin page with DataTable + ordering tabs.

---

## Technical Context

**Language/Version**: C# / .NET 10.0 (backend), TypeScript (frontend)
**Primary Dependencies**: ASP.NET Core 10.0.5, EF Core 10.0.5, MediatR 14.1.0, AutoMapper 16.1.1, FluentValidation 12.1.1 (backend); Next.js 16, Tailwind CSS v4, Zustand, next-intl, Radix UI / shadcn, Zod, `@googlemaps/js-api-loader` (frontend)
**Storage**: PostgreSQL via Npgsql + EF Core 10.0.5 — 2 new tables (`Stores`, `StoreOpeningHours`)
**Testing**: xUnit + Moq + FluentAssertions (backend unit tests for command handlers)
**Target Platform**: Linux container (Docker Compose), Next.js SSR/CSR
**Project Type**: Web service (REST API) + web application (Next.js)
**Performance Goals**: Public store list < 2s; distance sort in-memory acceptable for < 100 stores
**Constraints**: Exactly 7 opening hours per store; cross-midnight hours out of scope; cover image is URL-only (no upload in this feature)
**Scale/Scope**: ~5–50 stores; 2 controllers, 9 endpoints, 5 command handlers, 4 query handlers, 2 entities, ~15 frontend files

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|---------|
| I. Plan Mode Default | ✅ PASS | Planning complete before any code |
| II. Verification Before Done | ✅ PASS | `quickstart.md` defines Definition of Done with explicit verification steps |
| III. Simplicity First | ✅ PASS | In-memory Haversine (not PostGIS), replace-all hours (not upsert), URL-only images (no upload) |
| IV. Subagent Strategy | ✅ PASS | Two parallel research subagents used for backend/frontend patterns |
| V. Self-Improvement Loop | ✅ PASS | `tasks/lessons.md` to be updated after any corrections |
| VI. Autonomous + Concise | ✅ PASS | Plan is self-contained, no ambiguities remain |

**Tech Stack Compliance**:
- Frontend: Next.js 16, TypeScript, Tailwind v4, shadcn, next-intl, pnpm ✅
- Backend: .NET 10.0, Clean Architecture, JWT Bearer, `http://localhost:8002/api` ✅

**No violations to document.**

---

## Project Structure

### Documentation (this feature)

```text
specs/015-store-management/
├── plan.md              ← this file
├── spec.md              ← feature specification
├── research.md          ← Phase 0 research decisions
├── data-model.md        ← entity definitions and DTOs
├── quickstart.md        ← run/test guide + definition of done
├── contracts/
│   └── api-contracts.md ← REST endpoint contracts
├── checklists/
│   └── requirements.md  ← spec quality checklist
└── tasks.md             ← Phase 2 output (via /speckit.tasks)
```

### Source Code — Backend

```text
source/MoriiCoffee.Domain/
├── Aggregates/StoreAggregate/
│   ├── Store.cs                          ← NEW AggregateRoot
│   └── Entities/
│       └── StoreOpeningHours.cs          ← NEW EntityBase child
└── Repositories/
    └── IStoresRepository.cs              ← NEW interface
    (IUnitOfWork.cs — add Stores property)

source/MoriiCoffee.Application/
├── SeedWork/
│   ├── DTOs/Store/
│   │   ├── StoreDto.cs                   ← NEW
│   │   ├── StoreOpeningHoursDto.cs       ← NEW
│   │   ├── CreateStoreDto.cs             ← NEW
│   │   ├── CreateStoreOpeningHoursDto.cs ← NEW
│   │   ├── UpdateStoreStatusDto.cs       ← NEW
│   │   └── ReorderStoresDto.cs           ← NEW
│   └── Mappings/
│       └── StoreMapper.cs                ← NEW AutoMapper Profile
├── Commands/Store/
│   ├── CreateStore/
│   │   ├── CreateStoreCommand.cs         ← NEW
│   │   ├── CreateStoreCommandHandler.cs  ← NEW
│   │   └── CreateStoreCommandValidator.cs ← NEW
│   ├── UpdateStore/...                   ← NEW (same pattern)
│   ├── DeleteStore/...                   ← NEW
│   ├── UpdateStoreStatus/...             ← NEW
│   └── ReorderStores/...                 ← NEW
└── Queries/Store/
    ├── GetPublicStores/...               ← NEW
    ├── GetPublicStoreById/...            ← NEW
    ├── GetAdminStores/...                ← NEW
    └── GetAdminStoreById/...             ← NEW

source/MoriiCoffee.Infrastructure.Persistence/
├── Configurations/
│   ├── StoreConfiguration.cs             ← NEW
│   └── StoreOpeningHoursConfiguration.cs ← NEW
├── Repositories/
│   └── StoresRepository.cs              ← NEW
└── SeedWork/UnitOfWork/
    └── UnitOfWork.cs                    ← MODIFIED (add Stores)

source/MoriiCoffee.Presentation/
└── Controllers/
    ├── StoresController.cs              ← NEW (public)
    └── AdminStoresController.cs         ← NEW (admin)

source/MoriiCoffee.Infrastructure.Persistence/Migrations/
└── [timestamp]_AddStoreManagement.cs    ← NEW (generated)
```

### Source Code — Frontend (`morii-coffee-fe/`)

```text
src/
├── types/
│   └── api.ts                           ← MODIFIED (add ApiStore, ApiStoreOpeningHours)
├── constants/
│   ├── api-endpoints.ts                 ← MODIFIED (add ADMIN.STORES.*)
│   └── routes.ts                        ← MODIFIED (add ADMIN.STORES*)
├── features/stores/
│   ├── api.ts                           ← NEW
│   ├── hooks.ts                         ← NEW
│   ├── types.ts                         ← NEW
│   ├── schema.ts                        ← NEW (Zod)
│   ├── utils.ts                         ← NEW (getOpeningStatus, formatTime, getDistanceLabel)
│   ├── index.ts                         ← NEW
│   └── components/
│       ├── store-card.tsx               ← NEW
│       ├── store-map.tsx                ← NEW (Google Maps)
│       ├── store-hours-badge.tsx        ← NEW
│       ├── store-opening-hours.tsx      ← NEW
│       ├── store-city-filter.tsx        ← NEW
│       ├── store-list-table.tsx         ← NEW (admin DataTable)
│       └── store-form.tsx               ← NEW (admin form with 7-row hours)
├── app/
│   ├── stores/
│   │   └── page.tsx                     ← MODIFIED (replace dummy data with API)
│   ├── admin/
│   │   ├── layout.tsx                   ← MODIFIED (add Stores nav item)
│   │   └── stores/
│   │       ├── page.tsx                 ← NEW (list + ordering tabs)
│   │       ├── new/
│   │       │   └── page.tsx             ← NEW
│   │       └── edit/[id]/
│   │           └── page.tsx             ← NEW
│   └── [locales/]/
│       ├── en.json                      ← MODIFIED (add stores.* and adminStores.*)
│       └── vi.json                      ← MODIFIED
└── components/home/
    └── store-locator-preview.tsx        ← MODIFIED (replace dummy with useStores)

src/data/stores.ts                       ← DELETED (after API integration verified)
```

---

## Complexity Tracking

No constitutional violations. No unjustified complexity.
