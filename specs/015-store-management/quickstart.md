# Quickstart: Store Management (015)

**Branch**: `015-store-management`
**Date**: 2026-05-22

---

## How to Run

```bash
# From repo root
cd deploy && bash run-docker-development.sh
```

Backend: `http://localhost:8002/api`
Frontend: `http://localhost:3000`

---

## How to Test This Feature

### Backend — after running migration

```bash
# Apply migration (from Infrastructure.Persistence project directory)
dotnet ef database update \
  --project source/MoriiCoffee.Infrastructure.Persistence \
  --startup-project source/MoriiCoffee.Presentation
```

#### Smoke test (curl)

```bash
# 1. List public stores
curl http://localhost:8002/api/v1/stores

# 2. List stores near Saigon center
curl "http://localhost:8002/api/v1/stores?latitude=10.7769&longitude=106.7009&radius=10"

# 3. Admin login (get JWT)
TOKEN=$(curl -s -X POST http://localhost:8002/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@moriicoffee.vn","password":"your_password"}' \
  | jq -r '.data.accessToken')

# 4. Admin list stores
curl -H "Authorization: Bearer $TOKEN" http://localhost:8002/api/v1/admin/stores

# 5. Create a store
curl -X POST http://localhost:8002/api/v1/admin/stores \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Morii Coffee - Test",
    "address": "123 Test Street",
    "city": "Ho Chi Minh City",
    "latitude": 10.7769,
    "longitude": 106.7009,
    "phone": "+84 28 0000 0001",
    "isActive": true,
    "displayOrder": 99,
    "openingHours": [
      {"dayOfWeek":0,"openTime":"07:00","closeTime":"22:00","isClosed":false},
      {"dayOfWeek":1,"openTime":"07:00","closeTime":"22:00","isClosed":false},
      {"dayOfWeek":2,"openTime":"07:00","closeTime":"22:00","isClosed":false},
      {"dayOfWeek":3,"openTime":"07:00","closeTime":"22:00","isClosed":false},
      {"dayOfWeek":4,"openTime":"07:00","closeTime":"22:00","isClosed":false},
      {"dayOfWeek":5,"openTime":"07:00","closeTime":"22:00","isClosed":false},
      {"dayOfWeek":6,"openTime":"07:00","closeTime":"22:00","isClosed":false}
    ]
  }'

# 6. Toggle status
curl -X PATCH http://localhost:8002/api/v1/admin/stores/{id}/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"isActive": false}'
```

---

### Frontend — Manual Verification Paths

| Path | What to verify |
|------|---------------|
| `/stores` | All active stores listed with open/closed badges; "Near Me" button requests geolocation |
| `/stores` (with location granted) | List re-sorts by distance, each card shows "X km away" |
| `/stores` (with location denied) | Full list shown, no error state |
| `/stores` (city filter) | Clicking a city pill filters list client-side without reload |
| Home page | Store preview section shows first 3 live stores (no hardcoded dummy data) |
| `/admin/stores` | DataTable with store list, status toggle, edit/delete actions, city/status filters |
| `/admin/stores/new` | Form with 7-row opening hours editor; save creates store and redirects to list |
| `/admin/stores/edit/{id}` | Form pre-filled; save updates store |
| Delete action | ConfirmDialog shown; on confirm, store removed from list and hidden from `/stores` |
| Ordering tab | Drag-reorder interface; save fires PATCH /reorder; public page reflects new order |

---

## Key Files

### Backend

| File | Description |
|------|-------------|
| `source/MoriiCoffee.Domain/Aggregates/StoreAggregate/Store.cs` | Aggregate root |
| `source/MoriiCoffee.Domain/Aggregates/StoreAggregate/Entities/StoreOpeningHours.cs` | Child entity |
| `source/MoriiCoffee.Domain/Repositories/IStoresRepository.cs` | Repository interface |
| `source/MoriiCoffee.Domain/SeedWork/Persistence/IUnitOfWork.cs` | Add `IStoresRepository Stores` |
| `source/MoriiCoffee.Application/SeedWork/DTOs/Store/` | All DTOs |
| `source/MoriiCoffee.Application/SeedWork/Mappings/StoreMapper.cs` | AutoMapper profile |
| `source/MoriiCoffee.Application/Commands/Store/` | Command handlers (Create, Update, Delete, Status, Reorder) |
| `source/MoriiCoffee.Application/Queries/Store/` | Query handlers (GetPublic, GetAdmin, GetById x2) |
| `source/MoriiCoffee.Infrastructure.Persistence/Configurations/StoreConfiguration.cs` | EF Core config |
| `source/MoriiCoffee.Infrastructure.Persistence/Configurations/StoreOpeningHoursConfiguration.cs` | EF Core config |
| `source/MoriiCoffee.Infrastructure.Persistence/Repositories/StoresRepository.cs` | Repository impl |
| `source/MoriiCoffee.Infrastructure.Persistence/SeedWork/UnitOfWork/UnitOfWork.cs` | Add Stores property |
| `source/MoriiCoffee.Presentation/Controllers/StoresController.cs` | Public API |
| `source/MoriiCoffee.Presentation/Controllers/AdminStoresController.cs` | Admin API |

### Frontend (in `morii-coffee-fe/`)

| File | Description |
|------|-------------|
| `src/types/api.ts` | Add `ApiStore`, `ApiStoreOpeningHours` types |
| `src/features/stores/types.ts` | Query/request types |
| `src/features/stores/api.ts` | API functions |
| `src/features/stores/hooks.ts` | `useStores`, `useAdminStores`, etc. |
| `src/features/stores/utils.ts` | `getOpeningStatus`, `formatTime`, `getDistanceLabel` |
| `src/features/stores/schema.ts` | Zod schema for admin form |
| `src/features/stores/components/` | store-card, store-map, store-hours-badge, etc. |
| `src/constants/api-endpoints.ts` | Add `ADMIN.STORES.*` endpoints |
| `src/constants/routes.ts` | Add `ADMIN.STORES`, `ADMIN.STORES_NEW`, `ADMIN.STORES_EDIT` |
| `src/app/admin/layout.tsx` | Add "Stores" nav item with `MapPin` icon |
| `src/app/stores/page.tsx` | Refactor to use API (replace dummy data) |
| `src/app/admin/stores/page.tsx` | Admin list + ordering tabs |
| `src/app/admin/stores/new/page.tsx` | Create form page |
| `src/app/admin/stores/edit/[id]/page.tsx` | Edit form page |
| `src/components/home/store-locator-preview.tsx` | Refactor to use API |
| `src/data/stores.ts` | Delete after API integration verified |

---

## Definition of Done

- [ ] Backend: Migration runs cleanly, 5 seed stores inserted
- [ ] Backend: All 9 endpoints return correct responses (verified via curl)
- [ ] Backend: Soft-delete hides from public, visible in admin
- [ ] Backend: Geolocation query returns stores sorted by distance with `distanceKm`
- [ ] Frontend: `/stores` shows live data with open/closed status badges
- [ ] Frontend: "Near Me" sorts by distance when granted
- [ ] Frontend: City filter works client-side
- [ ] Frontend: Home page preview uses live data (0 hardcoded stores remain)
- [ ] Frontend: Admin CRUD (create/edit/delete/toggle) works end-to-end
- [ ] Frontend: Admin ordering tab saves new order visible on public page
- [ ] Frontend: i18n keys added in en.json and vi.json
- [ ] Frontend: `src/data/stores.ts` deleted
- [ ] Summary docs created in `docs/explanations/`
