# Tasks: My Wishlist

**Input**: Design documents from `/specs/014-my-wishlist/`  
**Branch**: `014-my-wishlist` | **Date**: 2026-05-22  
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/api.md ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.  
**Tests**: Not explicitly requested — omitted per Task Generation Rules.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in same phase)
- **[Story]**: Which user story this task belongs to (US1–US7)

## Path Conventions

```
Frontend root: morii-coffee-fe/src/
Backend root:  source/
```

---

## Phase 1: Setup

**Purpose**: No new project initialization needed — both frontend and backend already running.

- [X] T001 Verify Docker dev stack is running at http://localhost:8002 (backend) and http://localhost:3000 (frontend)
- [X] T002 Checkout branch `014-my-wishlist` and confirm git status is clean

---

## Phase 2: Backend Foundation (Blocking Prerequisites)

**Purpose**: Database entity, DTOs, repository, EF Core configuration, migration, and DI registration.
All subsequent backend work depends on this phase.

**⚠️ CRITICAL**: No user story backend work can begin until this phase is complete.

- [X] T003 [P] Create domain entity `WishlistItem` in `source/MoriiCoffee.Domain/Aggregates/WishlistAggregate/WishlistItem.cs` with fields: Id (Guid PK), UserId (Guid FK), ProductId (Guid FK), AddedAt (DateTime UTC)
- [X] T004 [P] Create DTO classes: `WishlistItemDto`, `WishlistDto`, `MergeGuestWishlistDto`, `GuestWishlistItemDto` in `source/MoriiCoffee.Application/SeedWork/DTOs/Wishlist/`
- [X] T005 Create `IWishlistItemRepository` interface in `source/MoriiCoffee.Domain/Aggregates/WishlistAggregate/IWishlistItemRepository.cs` (depends on T003)
- [X] T006 Create EF Core configuration `WishlistItemConfiguration` in `source/MoriiCoffee.Infrastructure.Persistence/Configurations/WishlistItemConfiguration.cs` with unique constraint `(UserId, ProductId)` and index on `UserId` (depends on T003)
- [X] T007 Add `DbSet<WishlistItem> WishlistItems` to `source/MoriiCoffee.Infrastructure.Persistence/Data/ApplicationDbContext.cs` (depends on T003, T006)
- [X] T008 Implement `WishlistItemRepository` in `source/MoriiCoffee.Infrastructure.Persistence/Repositories/WishlistItemRepository.cs` with methods: `GetByUserIdAsync`, `AddAsync`, `RemoveAsync`, `ExistsAsync`, `ClearAsync` (depends on T005, T007)
- [X] T009 Register `IWishlistItemRepository → WishlistItemRepository` as `AddScoped` in `source/MoriiCoffee.Infrastructure.Persistence/DependencyInjection.cs` (depends on T008)
- [X] T010 Generate and apply EF Core migration: `dotnet ef migrations add AddWishlistItems` in `source/MoriiCoffee.Infrastructure.Persistence`, verify `WishlistItems` table created with FK constraints (depends on T006, T007)

**Checkpoint**: `WishlistItems` table exists in DB. Backend DI resolves `IWishlistItemRepository`.

---

## Phase 3: Frontend Foundation (Blocking Prerequisites)

**Purpose**: API types and service layer that all frontend user story tasks depend on.

**⚠️ CRITICAL**: No user story frontend work can begin until this phase is complete.

- [X] T011 Add `ApiWishlistItem`, `ApiWishlist`, `ApiMergeWishlistRequest` interfaces to `morii-coffee-fe/src/types/api.ts` after the `ApiCart` block (line ~213)
- [X] T012 Create `morii-coffee-fe/src/services/wishlist-service.ts` with: `mapApiWishlistItem()`, `getWishlist()`, `addItem()`, `removeItem()`, `clearWishlist()`, `mergeWishlist()` — mirrors `cart-service.ts` pattern, calls `/v1/wishlist` endpoints (depends on T011)

**Checkpoint**: `wishlist-service.ts` exists and TypeScript compiles without errors (`pnpm tsc --noEmit`).

---

## Phase 4: User Story 1 — Guest Saves Products to Wishlist (Priority: P1) 🎯 MVP

**Goal**: Guest can heart products on product cards, see them persisted in localStorage, and view them at `/wishlist` with full product snapshots (name, price, image).

**Independent Test**:
1. Open browser incognito (no login)
2. Click heart on any product card → heart fills immediately
3. Navigate to `/wishlist` → product appears with name, price, image
4. Refresh → wishlist persists
5. Click remove → item disappears

### Backend: GET + ADD + REMOVE endpoints

- [X] T013 [P] Create `GetWishlistQuery` and `GetWishlistQueryHandler` in `source/MoriiCoffee.Application/Queries/Wishlist/GetWishlist/` — JOIN `WishlistItems → Products`, project to `WishlistItemDto` with `inStock = product.Status == EProductStatus.Active` (depends on T004, T008)
- [X] T014 [P] Create `AddItemToWishlistCommand` and `AddItemToWishlistCommandHandler` in `source/MoriiCoffee.Application/Commands/Wishlist/AddItemToWishlist/` — idempotent upsert (check `ExistsAsync` before `AddAsync`), 404 if product not found (depends on T004, T008)
- [X] T015 [P] Create `RemoveItemFromWishlistCommand` and `RemoveItemFromWishlistCommandHandler` in `source/MoriiCoffee.Application/Commands/Wishlist/RemoveItemFromWishlist/` — 404 if item not in wishlist (depends on T004, T008)
- [X] T016 Create `WishlistController` in `source/MoriiCoffee.Presentation/Controllers/WishlistController.cs` with: `[Authorize]`, `GET /api/v1/wishlist`, `POST /api/v1/wishlist/items`, `DELETE /api/v1/wishlist/items/{productId}` — mirrors `CartController` pattern with `GetCurrentUserId()` helper (depends on T013, T014, T015)

### Frontend: Store upgrade + WishlistButton + ProductCard + WishlistPage

- [X] T017 [US1] Rewrite `morii-coffee-fe/src/stores/wishlist-store.ts`: change `items` from `string[]` to `WishlistItem[]`, add `pendingIds: Set<string>`, `storageMode`, `isReady`, `syncError`; implement `addItem(item: WishlistItem)` and `removeItem(productId)` with optimistic updates + rollback (mirrors `cart-store.ts` pattern); implement `isInWishlist`, `totalItems`; update `partialize` to persist only `{items, storageMode}` (depends on T012)
- [X] T018 [P] [US1] Create `morii-coffee-fe/src/components/ui/wishlist-button.tsx`: heart SVG icon, `isInWishlist(productId)` from store, `onClick` toggles add/remove, `disabled` while `pendingIds.has(productId)`, CSS fill animation on active state; props: `productId`, `name`, `slug`, `price`, `image`, `size?: "sm" | "md"`, `variant?: "overlay" | "inline"` (depends on T017)
- [X] T019 [US1] Add `<WishlistButton>` overlay to `morii-coffee-fe/src/components/home/product-card.tsx`: absolute positioned top-right of product image, `e.stopPropagation()` to prevent Link navigation, pass product data as props (depends on T018)
- [X] T020 [US1] Refactor `morii-coffee-fe/src/app/wishlist/page.tsx`: remove `getAllProducts()` call, read `useWishlistStore(s => s.items)` directly, render `WishlistCard` grid per item showing name, price, image, remove button; add empty state with "Browse Products" link (depends on T017, T018)

**Checkpoint**: Guest can heart products on product cards, see them in `/wishlist`, and remove them. Data persists on refresh.

---

## Phase 5: User Story 2 — Auth Sync on Login (Priority: P1)

**Goal**: Guest wishlist merges into server wishlist on sign-in with zero data loss.

**Independent Test**:
1. Guest: add 3 products
2. Sign in (account has 2 existing items, 1 is a duplicate of guest item)
3. Navigate to `/wishlist` → see 4 unique items (3 + 2 - 1 duplicate)

### Backend: Merge endpoint

- [X] T021 [P] [US2] Create `MergeGuestWishlistCommand` and `MergeGuestWishlistCommandHandler` in `source/MoriiCoffee.Application/Commands/Wishlist/MergeGuestWishlist/` — iterate `guestItems`, call `ExistsAsync`; skip duplicates, `AddAsync` for new items; silently ignore unknown productIds (depends on T004, T008)
- [X] T022 [US2] Add `POST /api/v1/wishlist/merge` endpoint to `WishlistController.cs` using `MergeGuestWishlistCommand` + return `GetWishlistQuery` result as merged wishlist (depends on T016, T021)

### Frontend: initializeForSession + WishlistSessionSync

- [X] T023 [US2] Add `initializeForSession(isAuthenticated: boolean): Promise<void>` and `resetAfterLogout(): void` to `morii-coffee-fe/src/stores/wishlist-store.ts`: mirrors `cart-store.ts` pattern — guest → `wishlistService.mergeWishlist(localItems)`, authenticated → `wishlistService.getWishlist()` (depends on T017, T012)
- [X] T024 [US2] Add `WishlistSessionSync` component to `morii-coffee-fe/src/components/providers.tsx`: copies `CartSessionSync` pattern, watches `isAuthenticated`, calls `initializeForSession` / `resetAfterLogout` (depends on T023)

**Checkpoint**: Sign-in triggers merge. Wishlist page shows merged items. Signing out clears wishlist state.

---

## Phase 6: User Story 3 — Add Wishlist Product Directly to Cart (Priority: P1)

**Goal**: From the wishlist page, user can add individual or all in-stock products directly to cart.

**Independent Test**:
1. Open `/wishlist` with 2 in-stock and 1 out-of-stock item
2. Click "Add to Cart" on one in-stock item → it appears in cart drawer/badge
3. Click "Add All to Cart" → 2 in-stock items added, out-of-stock skipped

- [X] T025 [US3] Add "Add to Cart" button to each `WishlistCard` in `morii-coffee-fe/src/app/wishlist/page.tsx`: calls `useCartStore.addItem()` with product data, disabled if `!item.inStock` (depends on T020)
- [X] T026 [US3] Add "Add All to Cart" button to `morii-coffee-fe/src/app/wishlist/page.tsx`: sequential calls to `useCartStore.addItem()` for each item where `inStock === true`, shows loading state during calls, disabled if all items are out-of-stock (depends on T025)

**Checkpoint**: "Add to Cart" and "Add All to Cart" buttons work from wishlist page.

---

## Phase 7: User Story 4 — Out-of-Stock Visibility (Priority: P2)

**Goal**: Wishlist shows "Out of Stock" badge on unavailable products and disables their "Add to Cart" button.

**Independent Test**:
1. Set a wishlisted product to `Status = OutOfStock` via backend/admin
2. Reload `/wishlist` → product shows "Out of Stock" badge
3. "Add to Cart" button is disabled with tooltip

- [X] T027 [P] [US4] Verify `GetWishlistQueryHandler` correctly maps: `inStock = (product.Status == EProductStatus.Active)` — confirm `Inactive` and `OutOfStock` statuses both resolve to `inStock: false` in `source/MoriiCoffee.Application/Queries/Wishlist/GetWishlist/GetWishlistQueryHandler.cs` (depends on T013)
- [X] T028 [P] [US4] Add "Out of Stock" badge to `WishlistCard` in `morii-coffee-fe/src/app/wishlist/page.tsx`: conditionally render badge when `!item.inStock`; disable "Add to Cart" button (depends on T025)

**Checkpoint**: Out-of-stock products show badge, "Add to Cart" disabled. In-stock products unaffected.

---

## Phase 8: User Story 5 — Heart Button on Product Detail Page (Priority: P2)

**Goal**: Users can wishlist a product from its detail page, with the heart pre-filled if already wishlisted.

**Independent Test**:
1. Wishlist a product from the product card
2. Navigate to that product's detail page `/products/[slug]`
3. Heart icon is already filled (inWishlist = true)
4. Click it → heart unfills and item removed from wishlist

- [X] T029 [US5] Add `<WishlistButton variant="inline">` to `morii-coffee-fe/src/app/products/[slug]/page.tsx` next to the "Add to Cart" button; pass product `id`, `name`, `slug`, `price`, `image` as props (depends on T018)

**Checkpoint**: Heart button on product detail page shows correct state and toggles wishlist.

---

## Phase 9: User Story 6 — Wishlist Icon in Header (Priority: P2)

**Goal**: Header shows a heart icon with item count badge that links to `/wishlist` — mirrors the CartButton.

**Independent Test**:
1. Add 3 items to wishlist
2. Look at the header → heart icon with badge "3" is visible
3. Click heart icon → navigates to `/wishlist`

- [X] T030 [P] [US6] Create `morii-coffee-fe/src/components/layout/wishlist-button.tsx`: heart SVG icon + badge count from `useWishlistStore(s => s.totalItems())`, Link to `/wishlist`, mounted ref for hydration safety — mirrors `cart-button.tsx` exactly (depends on T017)
- [X] T031 [US6] Add `<WishlistButton />` to `morii-coffee-fe/src/components/layout/header.tsx` adjacent to the existing `<CartButton />` (depends on T030)

**Checkpoint**: Header shows heart icon with count badge. Badge updates when wishlist changes.

---

## Phase 10: User Story 7 — Logout Clears Wishlist (Priority: P3)

**Goal**: When a user logs out, their wishlist state is cleared so the next guest/user starts fresh.

**Independent Test**:
1. Log in, add 3 products to wishlist
2. Log out
3. Navigate to `/wishlist` → empty state (previous user's items not visible)
4. Add a new item as guest → shows in wishlist

_Note_: `resetAfterLogout` was already added to the store in T023 (US2). This phase wires it correctly.

- [X] T032 [US7] Verify `WishlistSessionSync` in `morii-coffee-fe/src/components/providers.tsx` correctly calls `resetAfterLogout()` when `isAuthenticated` transitions from `true` to `false` (edge case: ensure it only fires on logout, not on initial guest load) (depends on T024)
- [X] T033 [P] [US7] Manual test: login → add items → logout → verify localStorage `morii-wishlist` is cleared or `items: []` and `storageMode: "guest"` (no code change needed if T032 passes; document test result in PR description)

**Checkpoint**: Logout clears wishlist state. Next session starts with empty guest wishlist.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Remaining backend endpoint (clear), i18n strings, and documentation.

- [X] T034 [P] Create `ClearWishlistCommand` and `ClearWishlistCommandHandler` in `source/MoriiCoffee.Application/Commands/Wishlist/ClearWishlist/` — calls `IWishlistItemRepository.ClearAsync(userId)` (depends on T008)
- [X] T035 Add `DELETE /api/v1/wishlist` endpoint to `WishlistController.cs` using `ClearWishlistCommand` (depends on T016, T034)
- [X] T036 [P] Add `clearWishlist(): Promise<void>` to `morii-coffee-fe/src/stores/wishlist-store.ts` — optimistic clear + API call + rollback on failure (depends on T017, T012)
- [X] T037 [P] Add `"wishlist"` i18n namespace to `morii-coffee-fe/src/i18n/messages/en.json` and `vi.json`: `title`, `empty`, `emptyDescription`, `browseProducts`, `addAllToCart`, `removeFromWishlist`, `addedToWishlist`, `removedFromWishlist`
- [X] T038 Wire i18n strings into `morii-coffee-fe/src/app/wishlist/page.tsx` and `morii-coffee-fe/src/components/ui/wishlist-button.tsx` using `useTranslations("wishlist")` (depends on T037)
- [X] T039 Run full verification flow from `specs/014-my-wishlist/quickstart.md`: guest flow → auth merge → logout → out-of-stock badge
- [X] T040 [P] Write summary docs: `docs/explainations/summary-my-wishlist-ENG.md` and `docs/explainations/summary-my-wishlist-VN.md`

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
  └── Phase 2 (Backend Foundation) ──────────────────┐
  └── Phase 3 (Frontend Foundation) ─────────────────┤
        Both Phase 2 + 3 must complete ───────────────┤
          └── Phase 4 (US1 — P1) ◄──────────────── FIRST P1
                └── Phase 5 (US2 — P1) ◄─────────── SECOND P1
                      └── Phase 6 (US3 — P1) ◄───── THIRD P1
                            └── Phase 7 (US4 — P2)
                            └── Phase 8 (US5 — P2)
                            └── Phase 9 (US6 — P2)
                                  └── Phase 10 (US7 — P3)
                                        └── Phase 11 (Polish)
```

### User Story Dependencies

| Story | Priority | Depends on | Notes |
|-------|----------|------------|-------|
| US1 (Guest saves products) | P1 | Phase 2 + 3 foundation | MVP core — first to implement |
| US2 (Auth sync on login) | P1 | US1 complete | Needs store from US1 to add initializeForSession |
| US3 (Add to cart from wishlist) | P1 | US1 complete | Needs WishlistCard from US1 |
| US4 (Out-of-stock visibility) | P2 | US1 complete | Backend handler already in US1 |
| US5 (Heart on product detail) | P2 | US1 complete (WishlistButton from T018) | Single task |
| US6 (Header icon) | P2 | US1 complete (totalItems from T017) | Single component |
| US7 (Logout clears wishlist) | P3 | US2 complete (resetAfterLogout from T023) | Wire + verify only |

### Within Each Phase

```
Phase 2 (Backend Foundation):
  T003, T004 [P] can start immediately
  T005 → depends on T003
  T006 → depends on T003, T004
  T007 → depends on T005, T006
  T008 → depends on T006, T007
  T009, T010 → depends on T007, T008 (full foundation ready)

Phase 3 (Frontend Foundation):
  T011 can start immediately [P] with Phase 2
  T012 → depends on T011

Phase 4 (US1):
  Backend: T013, T014, T015 [P] can start after Phase 2
  Backend: T016 → depends on T013, T014, T015
  Frontend: T017 → depends on T012
  Frontend: T018, T019, T020 → depends on T017

Phase 5 (US2):
  T021 [P] with T022 after Phase 2
  T022 → depends on T016, T021
  T023 → depends on T017
  T024 → depends on T023
```

---

## Parallel Opportunities

### Phase 2 Parallel Launch

```
T003 — WishlistItem entity
T004 — WishlistItemDto/WishlistDto/MergeGuestWishlistDto
```

### Phase 3 Parallel with Phase 2

```
T011 — API types (api.ts) — can start while Phase 2 runs
```

### Phase 4 Backend Parallel (after Phase 2 complete)

```
T013 — GetWishlistQueryHandler
T014 — AddItemToWishlistCommandHandler
T015 — RemoveItemFromWishlistCommandHandler
```

### Phase 4 Frontend Parallel (after T017 complete)

```
T018 — WishlistButton component
T019 — ProductCard modification       (depends on T018)
T020 — WishlistPage refactor          (depends on T017, T018)
```

### Phase 5 Backend Parallel

```
T021 — MergeGuestWishlistCommandHandler (after Phase 2)
```

### Phase 11 Polish Parallel

```
T034 — ClearWishlistCommand
T036 — clearWishlist in store
T037 — i18n strings
T040 — Summary docs
```

---

## Implementation Strategy

### MVP First (User Stories 1, 2, 3 only — all P1)

1. ✅ Complete Phase 1: Setup (verify environment)
2. ✅ Complete Phase 2: Backend Foundation (entity → migration)
3. ✅ Complete Phase 3: Frontend Foundation (types → service)
4. ✅ Complete Phase 4: US1 (guest wishlist → product card heart → /wishlist page)
5. ✅ Complete Phase 5: US2 (auth merge on login)
6. ✅ Complete Phase 6: US3 (add to cart from wishlist)
7. **STOP and VALIDATE**: All 3 P1 stories work end-to-end

### Incremental Delivery

1. After MVP (P1 stories): Add P2 stories (US4 out-of-stock, US5 product detail heart, US6 header icon) — these are polish
2. After P2 stories: Add P3 (US7 logout) — simplest story, already mostly wired by US2
3. Final: Polish phase (clear endpoint, i18n, docs)

### Solo Developer Strategy

Work sequentially: Phase 2 → Phase 3 (parallel with browser open) → Phase 4 backend → Phase 4 frontend → Phase 5 → Phase 6 → Phase 7, 8, 9 → Phase 10 → Phase 11

---

## Task Summary

| Phase | Tasks | Story | Notes |
|-------|-------|-------|-------|
| Phase 1: Setup | T001–T002 | — | 2 tasks |
| Phase 2: Backend Foundation | T003–T010 | — | 8 tasks, blocks backend US |
| Phase 3: Frontend Foundation | T011–T012 | — | 2 tasks, blocks frontend US |
| Phase 4: US1 (P1) | T013–T020 | US1 | 8 tasks (4 backend + 4 frontend) |
| Phase 5: US2 (P1) | T021–T024 | US2 | 4 tasks |
| Phase 6: US3 (P1) | T025–T026 | US3 | 2 tasks |
| Phase 7: US4 (P2) | T027–T028 | US4 | 2 tasks |
| Phase 8: US5 (P2) | T029 | US5 | 1 task |
| Phase 9: US6 (P2) | T030–T031 | US6 | 2 tasks |
| Phase 10: US7 (P3) | T032–T033 | US7 | 2 tasks |
| Phase 11: Polish | T034–T040 | — | 7 tasks |
| **TOTAL** | **T001–T040** | | **40 tasks** |

---

## Notes

- `[P]` tasks = different files, no dependencies within the same phase
- `[Story]` label maps task to specific user story for traceability
- Each user story is independently testable after its phase completes
- Commit after each task or logical group (atomic commits per constitution)
- Run `pnpm tsc --noEmit` after every frontend file change
- Run `dotnet build` after every backend file change
- Stop at each checkpoint to validate story independently before moving on
