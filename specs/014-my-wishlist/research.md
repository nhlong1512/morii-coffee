# Research: My Wishlist — Phase 0

**Branch**: `014-my-wishlist` | **Date**: 2026-05-22

All NEEDS CLARIFICATION items from Technical Context resolved below.

---

## Decision 1: Backend Storage — SQL vs Redis for Wishlist

**Decision**: SQL via EF Core (PostgreSQL), NOT Redis

**Rationale**:
- Cart uses Redis with a **24-hour TTL** — deliberately ephemeral. If Redis evicts the cart, the user just re-adds items (minor inconvenience).
- Wishlist represents **user intent** to revisit products. Losing wishlist data after a Redis TTL expiry or restart would be a data loss bug, not a minor inconvenience.
- `GET /v1/wishlist` must return **live product data** (current price, inStock status). SQL allows a single JOIN query: `WishlistItems JOIN Products`. Redis would require storing a snapshot at write time (stale data risk) or separate product lookups (N+1 queries).
- WishlistItems table is simple and low-volume (average user has <50 items) — no performance concern.

**Alternatives Considered**:
- **Redis (same as cart)**: Rejected — data loss on TTL expiry, snapshot staleness issue
- **Redis with product snapshot at write time**: Rejected — stale price/inStock, requires invalidation logic
- **Redis with PostgreSQL fallback**: Rejected — adds complexity for no benefit at this scale

---

## Decision 2: WishlistItem Entity — Aggregate Root or not?

**Decision**: `WishlistItem` is a **standalone entity** (not an aggregate root). No separate `Wishlist` aggregate.

**Rationale**:
- There is no domain logic that spans multiple wishlist items together (e.g., no "apply discount to entire wishlist"). Each item is an independent saved-product reference.
- Cart uses `ICartService` with Redis (no entity/aggregate). The closest parallel for SQL-backed persistence is an entity with a repository.
- Adding a `Wishlist` aggregate root with a collection of `WishlistItem` entities would introduce unnecessary complexity (cascade deletes, aggregate ID, entity ID vs aggregate ID confusion).
- Simpler: `WishlistItem` (userId, productId, addedAt) as a direct entity. Queries filter by `userId`.

**Alternatives Considered**:
- **Wishlist aggregate root containing WishlistItem entities**: Rejected — no cross-item domain logic justifies the complexity
- **User aggregate extension (add WishlistItems to UserAggregate)**: Rejected — violates aggregate root boundaries; User should not own product-domain entities

---

## Decision 3: `inStock` Derivation

**Decision**: `inStock = (product.Status == EProductStatus.Active)` at query time via EF Core projection.

**Rationale**:
- The existing `EProductStatus` enum has three values: `Active (0)`, `Inactive (1)`, `OutOfStock (2)`.
- `Active` → available for purchase → `inStock: true`
- `Inactive` or `OutOfStock` → not purchasable → `inStock: false`
- The query handler projects this directly: `inStock = product.Status == EProductStatus.Active`

---

## Decision 4: IWishlistRepository — Interface or Direct DbContext?

**Decision**: Introduce `IWishlistItemRepository` interface in Domain with SQL implementation in Infrastructure.Persistence.

**Rationale**:
- Consistent with the project's Clean Architecture pattern: existing repositories for Orders use `IOrderRepository`.
- Keeps application layer (command/query handlers) independent of persistence technology.
- The repository handles: `GetByUserIdAsync`, `AddAsync`, `RemoveAsync`, `ClearAsync`, `ExistsAsync`.

---

## Decision 5: Frontend — Product Snapshot Source for Guest Wishlist

**Decision**: Guest wishlist stores a **minimal snapshot** (id, name, slug, price, image, inStock, addedAt) in localStorage using the new `WishlistItem` interface.

**Rationale**:
- The current store only stores `string[]` of productIds. The wishlist page must render name, price, and image without going back to the API.
- Storing the snapshot at add-time (from the product card data already in the DOM) avoids a follow-up API call.
- Price staleness is acceptable for guests — authenticated users always get live data from `GET /v1/wishlist`.

**How data enters the store**:
- On ProductCard: `WishlistButton` receives product data as props (productId, name, slug, price, image) → passed to `addItem(WishlistItem)`.
- On product detail page: same props from the page's product data.

---

## Decision 6: `pendingIds` implementation

**Decision**: Use `Set<string>` on the store state (not persisted) to track in-flight requests.

**Rationale**:
- Per-productId locking (not entire store) allows concurrent operations on different products.
- `Set` is not serializable by Zustand persist's default serializer → use `partialize` to exclude it from localStorage.
- Initialized as empty `new Set()` on every page load.

---

## Decision 7: Heart Button — Framer Motion or CSS for animation?

**Decision**: CSS transition only (no Framer Motion dependency).

**Rationale**:
- The spec mentioned Framer Motion, but the current codebase does not have it installed.
- CSS scale transform on `:active` and a fill animation via SVG stroke/fill is sufficient for MVP.
- Avoids adding a new dependency for a minor UX polish.

---

## Decision 8: "Add All to Cart" — sequential or parallel calls?

**Decision**: Sequential calls (one at a time) to avoid overwhelming the cart API.

**Rationale**:
- Cart endpoint is Redis-backed; rapid parallel writes would require atomic locking logic.
- Sequential add is simpler and sufficient for MVP (<100 items).
- Optimistic UI: show a loading spinner on the "Add All to Cart" button for the duration.

---

## Resolved: No Backend Endpoints Need to be Pre-built

The feature plan includes building ALL 5 wishlist endpoints as part of this implementation. The frontend `wishlist-service.ts` will be built alongside, not waiting for a separate backend team. This is a full-stack implementation.
