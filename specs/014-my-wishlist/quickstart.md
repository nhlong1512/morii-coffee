# Developer Quickstart: My Wishlist

**Branch**: `014-my-wishlist` | **Date**: 2026-05-22

---

## Prerequisites

- Docker + Docker Compose running (dev stack: `cd deploy && bash run-docker-development.sh`)
- Backend running at `http://localhost:8002`
- Frontend running at `http://localhost:3000`
- pnpm installed (frontend package manager)

---

## Key Files to Know

| File | Purpose |
|------|---------|
| `morii-coffee-fe/src/stores/wishlist-store.ts` | Zustand store — central state for wishlist |
| `morii-coffee-fe/src/services/wishlist-service.ts` | API calls to backend wishlist endpoints |
| `morii-coffee-fe/src/types/api.ts` | `ApiWishlist`, `ApiWishlistItem` types |
| `morii-coffee-fe/src/components/ui/wishlist-button.tsx` | Heart toggle component |
| `morii-coffee-fe/src/components/providers.tsx` | `WishlistSessionSync` — auth change listener |
| `source/MoriiCoffee.Domain/Aggregates/WishlistAggregate/WishlistItem.cs` | Domain entity |
| `source/MoriiCoffee.Application/Commands/Wishlist/` | MediatR commands |
| `source/MoriiCoffee.Application/Queries/Wishlist/` | MediatR queries |
| `source/MoriiCoffee.Presentation/Controllers/WishlistController.cs` | REST controller |
| `source/MoriiCoffee.Infrastructure.Persistence/Migrations/` | DB migrations |

---

## Backend Setup (after branch checkout)

```bash
# 1. Navigate to source root
cd source

# 2. Run EF Core migration (adds WishlistItems table)
dotnet ef database update --project MoriiCoffee.Infrastructure.Persistence --startup-project MoriiCoffee.Presentation

# 3. Build and run
dotnet run --project MoriiCoffee.Presentation

# 4. Verify endpoints available in Swagger
# Open: http://localhost:8002/swagger — look for "Wishlist" section
```

---

## Frontend Setup (after branch checkout)

```bash
# 1. Navigate to frontend
cd morii-coffee-fe

# 2. Install dependencies (if new packages added)
pnpm install

# 3. Run dev server
pnpm dev

# 4. Test in browser
# Open: http://localhost:3000/wishlist
```

---

## Test the Full Flow

### Guest wishlist (no login needed)

1. Open `http://localhost:3000` in an incognito window
2. Browse products — click the ♥ heart button on any product card
3. Navigate to `/wishlist` — verify the product appears with name, price, image
4. Open DevTools → Application → localStorage → `morii-wishlist` — verify item is stored
5. Refresh the page — verify wishlist persists

### Auth merge flow

1. Add 2 products as guest (step above)
2. Sign in with a test account that has 1 existing wishlist item (different product)
3. Navigate to `/wishlist` — verify 3 unique items appear (merge happened)
4. Open Network tab — look for `POST /api/v1/wishlist/merge` request on sign-in

### Remove and clear

1. On `/wishlist`, click the trash icon on one item — verify it disappears
2. Click "Add to Cart" on an in-stock item — verify it appears in cart
3. Mark a product as OutOfStock in the admin panel — verify "Out of Stock" badge appears on wishlist

---

## Running Tests

```bash
# Frontend unit tests
cd morii-coffee-fe && pnpm test --testPathPattern wishlist

# Backend unit tests
cd source && dotnet test --filter "Wishlist"
```

---

## Common Issues

**Wishlist not syncing on login**: Check that `WishlistSessionSync` is mounted in `providers.tsx` and that `initializeForSession` is called when `isAuthenticated` changes.

**Out of stock not showing**: Verify the backend `GetWishlistQueryHandler` correctly joins `WishlistItems → Products` and maps `product.Status == EProductStatus.Active` to `inStock`.

**Guest wishlist lost on page refresh**: Check Zustand persist config — `storageMode` and `items` must be in `partialize`. Verify `pendingIds` (a `Set`) is NOT in `partialize` to avoid serialization issues.

**Heart button not disabled during request**: Verify `pendingIds.has(productId)` is checked in `WishlistButton.tsx` and the `disabled` prop is correctly passed to the button element.
