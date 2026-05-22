# Implementation Plan: My Wishlist

**Branch**: `014-my-wishlist` | **Date**: 2026-05-22 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/014-my-wishlist/spec.md`

## Summary

Customers can "heart" any product to save it to a wishlist that works for both **guest users** (localStorage, no login required) and **authenticated users** (SQL-backed, synced with server). On sign-in, guest items merge into the server wishlist with zero data loss — identical to the existing cart pattern. The wishlist page (`/wishlist`) renders product snapshots (name, price, thumbnail, stock status) without secondary product lookups, and supports adding items directly to the cart.

**Frontend approach**: Upgrade the bare-bones `wishlist-store.ts` to mirror `cart-store.ts` (storageMode, optimistic updates, initializeForSession, resetAfterLogout) and add a new `wishlist-service.ts`. Add heart buttons to ProductCard and product detail pages, a count badge to the header, and refactor the wishlist page.

**Backend approach**: SQL-backed wishlist (EF Core + PostgreSQL). Each user has a `WishlistItems` table row per saved product. `GET /v1/wishlist` joins to the Products table to return product snapshots with live `inStock` status. Full CQRS stack (commands/queries/handler) mirroring CartController.

## Technical Context

**Language/Version**: TypeScript 5.x (frontend) / C# .NET 10.0 (backend)  
**Primary Dependencies**:
- Frontend: Next.js 16 (App Router), Zustand + persist middleware, next-intl, Tailwind CSS v4, Radix UI / shadcn
- Backend: MediatR 14, EF Core 10 + Npgsql, FluentValidation 12, AutoMapper 16

**Storage**:
- Frontend: localStorage via Zustand persist (guest mode), server API (authenticated mode)
- Backend: PostgreSQL via EF Core (WishlistItems table + Product join for snapshots); NOT Redis — wishlist is long-lived unlike the 24h-TTL cart

**Testing**:
- Frontend: Jest + React Testing Library
- Backend: xUnit, Moq, FluentAssertions

**Target Platform**: Web — browser (Chrome 90+, Safari 14+, Firefox 88+), responsive (375px–1920px)  
**Project Type**: Full-stack web application (Next.js frontend + .NET REST API)  
**Performance Goals**: Wishlist page renders with 100 items <500ms; optimistic update visible <100ms; "Add All to Cart" completes <1s  
**Constraints**: WCAG AA accessibility, i18n (VI/EN), zero data loss on guest→auth merge, CRUD operations ≤200ms API p95  
**Scale/Scope**: Single-user wishlist per account; no pagination needed for MVP (assume <100 items per user)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Plan Mode Default** | ✅ PASS | Full planning artifacts generated before any code |
| **II. Verification Before Done** | ✅ PASS | Each task phase includes verification criteria; UI to be tested in browser |
| **III. Simplicity First** | ✅ PASS | Mirrors existing cart pattern — no new abstractions introduced; wishlist store is a direct clone-and-adapt of cart-store |
| **IV. Subagent Strategy** | ✅ PASS | Code exploration delegated to subagents; main context kept focused |
| **V. Self-Improvement Loop** | ✅ PASS | lessons.md to be updated after corrections |
| **VI. Autonomous Execution** | ✅ PASS | Plan is specific enough for autonomous execution |
| **Tech Stack** | ✅ PASS | All tech choices comply with constitution (Next.js 16, Zustand, next-intl, .NET 10, EF Core, MediatR) |

**Post-Design Re-check**: SQL storage decision for wishlist (vs Redis cart) is justified because wishlist is long-lived (no TTL), benefits from join queries for live product data, and is semantically more like an Order than a Cart.

## Project Structure

### Documentation (this feature)

```text
specs/014-my-wishlist/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Phase 1 entity and DB model
├── quickstart.md        # Developer setup guide
├── contracts/
│   └── api.md           # REST endpoint contracts
├── checklists/
│   └── requirements.md  # Quality checklist (already completed)
└── tasks.md             # Phase 2 output (/speckit.tasks - NOT yet created)
```

### Source Code

```text
# Frontend: morii-coffee-fe/src/
types/
└── api.ts                        # ADD: ApiWishlistItem, ApiWishlist, ApiMergeWishlistRequest

services/
└── wishlist-service.ts           # CREATE NEW: getWishlist, addItem, removeItem, clearWishlist, mergeWishlist

stores/
└── wishlist-store.ts             # UPGRADE: WishlistItem type, storageMode, optimistic updates, initializeForSession, resetAfterLogout

components/
├── ui/
│   └── wishlist-button.tsx       # CREATE NEW: heart toggle button (overlay/inline variants)
├── home/
│   └── product-card.tsx          # MODIFY: add WishlistButton overlay on product image
└── layout/
    ├── header.tsx                 # MODIFY: add wishlist icon + count badge next to CartButton
    └── wishlist-button.tsx        # CREATE NEW: header icon button (mirrors cart-button.tsx)

app/
├── wishlist/
│   └── page.tsx                   # MODIFY: remove getAllProducts(), read from store, add "Add All to Cart"
└── products/[slug]/
    └── page.tsx                   # MODIFY: add WishlistButton inline next to Add-to-Cart

components/
└── providers.tsx                  # MODIFY: add WishlistSessionSync (mirrors CartSessionSync)

i18n/messages/
├── en.json                        # ADD: "wishlist" namespace
└── vi.json                        # ADD: "wishlist" namespace

# Backend: source/
MoriiCoffee.Domain/
└── Aggregates/
    └── WishlistAggregate/
        └── WishlistItem.cs        # CREATE NEW: domain entity

MoriiCoffee.Application/
├── Commands/Wishlist/
│   ├── AddItemToWishlist/
│   │   ├── AddItemToWishlistCommand.cs
│   │   └── AddItemToWishlistCommandHandler.cs
│   ├── RemoveItemFromWishlist/
│   │   ├── RemoveItemFromWishlistCommand.cs
│   │   └── RemoveItemFromWishlistCommandHandler.cs
│   ├── ClearWishlist/
│   │   ├── ClearWishlistCommand.cs
│   │   └── ClearWishlistCommandHandler.cs
│   └── MergeGuestWishlist/
│       ├── MergeGuestWishlistCommand.cs
│       └── MergeGuestWishlistCommandHandler.cs
├── Queries/Wishlist/
│   └── GetWishlist/
│       ├── GetWishlistQuery.cs
│       └── GetWishlistQueryHandler.cs
└── SeedWork/DTOs/Wishlist/
    ├── WishlistDto.cs
    ├── WishlistItemDto.cs
    └── MergeGuestWishlistDto.cs

MoriiCoffee.Infrastructure.Persistence/
├── Configurations/
│   └── WishlistItemConfiguration.cs    # EF Core configuration
├── Repositories/
│   └── WishlistItemRepository.cs       # CREATE NEW
└── Data/
    └── ApplicationDbContext.cs          # MODIFY: add DbSet<WishlistItem>

MoriiCoffee.Presentation/
└── Controllers/
    └── WishlistController.cs            # CREATE NEW: mirrors CartController
```

**Structure Decision**: Full-stack web app — frontend in `morii-coffee-fe/`, backend in `morii-coffee/source/`. Follows existing project conventions exactly (CQRS, repository, EF Core configuration pattern).
