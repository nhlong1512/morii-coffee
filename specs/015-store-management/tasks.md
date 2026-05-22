# Tasks: Store Management (015)

**Branch**: `015-store-management`
**Input**: Design documents from `specs/015-store-management/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅ | quickstart.md ✅

**Path conventions**:
- `[be]/` → `source/` (backend Clean Architecture project)
- `[fe]/` → `../morii-coffee-fe/` (Next.js frontend, sibling repo)
- Tests are NOT included — not requested in spec

**User stories**:
- **US1** (P1): Admin Creates and Manages Store Locations (CRUD)
- **US2** (P2): Admin Toggles Store Visibility
- **US3** (P2): Public Visitor Finds a Store Near Them
- **US4** (P3): Public Visitor Explores Stores on a Map
- **US5** (P4): Admin Reorders Store Display Sequence

---

## Phase 1: Setup

**Purpose**: No new project setup required — `015-store-management` branch already created. Confirm baseline.

- [ ] T001 Verify branch is `015-store-management` and Docker stack starts cleanly (`cd deploy && bash run-docker-development.sh`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, DTOs, infrastructure, and EF migration that ALL user stories depend on. No US work can begin until this phase is complete.

**⚠️ CRITICAL**: Complete T002–T020 before any Phase 3+ tasks.

### Domain Layer

- [ ] T002 [P] Create `Store` aggregate root in `[be]/MoriiCoffee.Domain/Aggregates/StoreAggregate/Store.cs` — fields: Id (Guid), Name (MaxLength 200, unique), Slug (MaxLength 200, unique), Address (MaxLength 500), District (MaxLength 100, nullable), City (MaxLength 100), Province (MaxLength 100, nullable), Latitude (double), Longitude (double), Phone (MaxLength 20), Email (MaxLength 100, nullable), CoverImageUrl (MaxLength 500, nullable), IsActive (bool, default true), DisplayOrder (int), OpeningHours (ICollection<StoreOpeningHours>) — factory method `Store.Create(CreateStoreDto dto, string slug)` + `Update(UpdateStoreDto dto, string slug)` + `SetStatus(bool isActive)` + `SetDisplayOrder(int order)`

- [ ] T003 [P] Create `StoreOpeningHours` child entity in `[be]/MoriiCoffee.Domain/Aggregates/StoreAggregate/Entities/StoreOpeningHours.cs` — fields: Id (Guid), StoreId (Guid), Store (navigation), DayOfWeek (int 0–6), OpenTime (MaxLength 5, "HH:mm"), CloseTime (MaxLength 5, "HH:mm"), IsClosed (bool) — factory method `StoreOpeningHours.Create(Guid storeId, int dayOfWeek, string openTime, string closeTime, bool isClosed)`

- [ ] T004 [P] Create `IStoresRepository` interface in `[be]/MoriiCoffee.Domain/Repositories/IStoresRepository.cs` extending `IRepositoryBase<Store>` — add method: `Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)`

- [ ] T005 Add `IStoresRepository Stores { get; }` property to `[be]/MoriiCoffee.Domain/SeedWork/Persistence/IUnitOfWork.cs`

### Application Layer — DTOs

- [ ] T006 [P] Create `StoreDto.cs` in `[be]/MoriiCoffee.Application/SeedWork/DTOs/Store/StoreDto.cs` — all fields from data-model.md including `double? DistanceKm` and `List<StoreOpeningHoursDto> OpeningHours`

- [ ] T007 [P] Create `StoreOpeningHoursDto.cs` in `[be]/MoriiCoffee.Application/SeedWork/DTOs/Store/StoreOpeningHoursDto.cs` — Id, DayOfWeek, OpenTime, CloseTime, IsClosed

- [ ] T008 [P] Create `CreateStoreDto.cs` and `CreateStoreOpeningHoursDto.cs` in `[be]/MoriiCoffee.Application/SeedWork/DTOs/Store/` — all writable fields; `OpeningHours` is `List<CreateStoreOpeningHoursDto>` (exactly 7 required)

- [ ] T009 [P] Create `UpdateStoreStatusDto.cs` (single field `IsActive`) and `ReorderStoresDto.cs` + `ReorderStoreItem.cs` (Id + DisplayOrder) in `[be]/MoriiCoffee.Application/SeedWork/DTOs/Store/`

### Application Layer — AutoMapper

- [ ] T010 Create `StoreMapper.cs` AutoMapper profile in `[be]/MoriiCoffee.Application/SeedWork/Mappings/StoreMapper.cs` — `CreateMap<Store, StoreDto>()` with `Ignore(dest => dest.DistanceKm)` + `CreateMap<StoreOpeningHours, StoreOpeningHoursDto>()` — register in DI alongside existing mapper profiles

### Infrastructure Layer — EF Core

- [ ] T011 [P] Create `StoreConfiguration.cs` in `[be]/MoriiCoffee.Infrastructure.Persistence/Configurations/StoreConfiguration.cs` — unique indexes on Slug and Name, standard indexes on City, IsActive, DisplayOrder; HasMany(OpeningHours).WithOne(s => s.Store).HasForeignKey(h => h.StoreId).OnDelete(DeleteBehavior.Cascade)

- [ ] T012 [P] Create `StoreOpeningHoursConfiguration.cs` in `[be]/MoriiCoffee.Infrastructure.Persistence/Configurations/StoreOpeningHoursConfiguration.cs` — index on StoreId, MaxLength constraints for OpenTime/CloseTime

- [ ] T013 Create `StoresRepository.cs` in `[be]/MoriiCoffee.Infrastructure.Persistence/Repositories/StoresRepository.cs` — extends `RepositoryBase<Store>`, implements `IStoresRepository`; implement `SlugExistsAsync` (case-insensitive check excluding optional id)

- [ ] T014 Add `IStoresRepository Stores` lazy property (using `??=` pattern) and private `StoresRepository? _stores` field to `[be]/MoriiCoffee.Infrastructure.Persistence/SeedWork/UnitOfWork/UnitOfWork.cs` — following existing `??=` lazy initialization pattern

- [ ] T015 Register `IStoresRepository` → `StoresRepository` in the DI container (find the infrastructure service registration file, typically `ServiceCollectionExtensions.cs` or similar in Infrastructure)

- [ ] T016 Add `DbSet<Store> Stores` and `DbSet<StoreOpeningHours> StoreOpeningHours` to `ApplicationDbContext.cs`; run EF Core migration: `dotnet ef migrations add AddStoreManagement --project [be]/MoriiCoffee.Infrastructure.Persistence --startup-project [be]/MoriiCoffee.Presentation`; apply: `dotnet ef database update`

- [ ] T017 Add seed data in `[be]/MoriiCoffee.Infrastructure.Persistence/` (or DataSeeder) — 5 stores matching current dummy data from `[fe]/src/data/stores.ts`, each with 7 `StoreOpeningHours` records (Mon–Sat 07:00–22:00 open, Sun may vary)

### Frontend Foundational

- [ ] T018 [P] Add `ApiStore` and `ApiStoreOpeningHours` TypeScript interfaces to `[fe]/src/types/api.ts` — all fields from contracts/api-contracts.md (id, name, slug, address, district, city, province, latitude, longitude, phone, email, coverImageUrl, isActive, displayOrder, distanceKm, openingHours, createdAt, updatedAt)

- [ ] T019 [P] Add admin store endpoint constants to `[fe]/src/constants/api-endpoints.ts` under `ADMIN.STORES`: LIST, CREATE, DETAIL(id), UPDATE(id), DELETE(id), STATUS(id), REORDER

- [ ] T020 [P] Add admin store routes to `[fe]/src/constants/routes.ts`: `ADMIN.STORES = "/admin/stores"`, `ADMIN.STORES_NEW = "/admin/stores/new"`, `ADMIN.STORES_EDIT: (id: string) => \`/admin/stores/edit/${id}\``

**Checkpoint**: Foundation complete — all domain entities, DTOs, infrastructure, migration, and frontend constants are ready. User story work can now begin.

---

## Phase 3: User Story 1 — Admin Creates and Manages Store Locations (Priority: P1) 🎯 MVP

**Goal**: Admin can create, read, update, and delete stores via the admin panel. The admin list page shows all stores with edit/delete actions. Each store form includes a 7-row opening hours editor.

**Independent Test**: Admin logs in → navigates to `/admin/stores` → creates a new store with all 7 days of opening hours → verifies it appears in the list → edits the name → soft-deletes it → confirms it's removed from the list.

### Backend — Commands

- [ ] T021 [P] Create `CreateStoreCommand.cs` + `CreateStoreCommandHandler.cs` + `CreateStoreCommandValidator.cs` in `[be]/MoriiCoffee.Application/Commands/Store/CreateStore/` — handler: slugify name if slug null/empty, check `SlugExistsAsync`, check name uniqueness via `FindByCondition`, call `Store.Create()`, add 7 `StoreOpeningHours` via `StoreOpeningHours.Create()`, save via `CommitAsync()`, return `StoreDto` (mapped + set `DistanceKm = null`); validator: name/address/city/phone required, lat -90..90, lng -180..180, openingHours must have exactly 7 items with unique dayOfWeek 0–6

- [ ] T022 [P] Create `UpdateStoreCommand.cs` + `UpdateStoreCommandHandler.cs` + `UpdateStoreCommandValidator.cs` in `[be]/MoriiCoffee.Application/Commands/Store/UpdateStore/` — handler: load store by id (throw NotFoundException if not found), check slug/name uniqueness excluding current store, call `store.Update()`, delete all existing `StoreOpeningHours` for this store (remove from context), add 7 fresh `StoreOpeningHours`, `CommitAsync()`, return `StoreDto`; validator: same as Create

- [ ] T023 [P] Create `DeleteStoreCommand.cs` + `DeleteStoreCommandHandler.cs` in `[be]/MoriiCoffee.Application/Commands/Store/DeleteStore/` — handler: load store by id (throw NotFoundException if not found), call `SoftDelete()` on entity (sets IsDeleted=true, DeletedAt=now), `CommitAsync()`; returns void

### Backend — Admin Queries

- [ ] T024 [P] Create `GetAdminStoresQuery.cs` + `GetAdminStoresQueryHandler.cs` in `[be]/MoriiCoffee.Application/Queries/Store/GetAdminStores/` — query params: PaginationFilter, bool? isActive, string? city, string? search; handler: `FindAll(false)` (includes non-active, excludes deleted), apply isActive/city/search filters, Include OpeningHours, OrderBy DisplayOrder, project to StoreDto via AutoMapper, return `Pagination<StoreDto>` via PagingHelper

- [ ] T025 [P] Create `GetAdminStoreByIdQuery.cs` + `GetAdminStoreByIdQueryHandler.cs` in `[be]/MoriiCoffee.Application/Queries/Store/GetAdminStoreById/` — handler: `FindByCondition(s => s.Id == id, false).Include(s => s.OpeningHours).FirstOrDefaultAsync()`, throw NotFoundException if null, map to StoreDto

### Backend — Admin Controller

- [ ] T026 Create `AdminStoresController.cs` in `[be]/MoriiCoffee.Presentation/Controllers/AdminStoresController.cs` — route `api/v1/admin/stores`, class-level `[Authorize(Roles = "ADMIN,STAFF")]`; implement: GET list (→ GetAdminStoresQuery), GET /{id} (→ GetAdminStoreByIdQuery), POST create (→ CreateStoreCommand, returns 201), PUT /{id}/update (→ UpdateStoreCommand, returns 200), DELETE /{id} (→ DeleteStoreCommand, returns 204) — skip status and reorder endpoints (added in US2/US5)

### Frontend — Feature Foundation

- [ ] T027 [P] Create `[fe]/src/features/stores/types.ts` — StoresQuery, AdminStoresQuery, CreateStoreRequest, CreateStoreOpeningHoursRequest, UpdateStoreStatusRequest, ReorderStoresRequest, StoreFormValues interfaces

- [ ] T028 [P] Create `[fe]/src/features/stores/api.ts` — implement: `getStores(q)`, `getStoreById(id)`, `getAdminStores(q)`, `getAdminStoreById(id)`, `createStore(body)`, `updateStore(id, body)`, `deleteStore(id)`; stub `updateStoreStatus` and `reorderStores` as TODO

- [ ] T029 [P] Create `[fe]/src/features/stores/hooks.ts` — implement `useAdminStores(query)` and `useAdminStore(id)` following the same pattern as `useAdminBlogPosts` in `[fe]/src/features/blogs/hooks.ts`; stub `useStores` and `useStore` as TODO

- [ ] T030 [P] Create `[fe]/src/features/stores/schema.ts` — Zod storeSchema: name (min 1, max 200), slug (optional), address (min 1, max 500), district/city/province, latitude (-90..90), longitude (-180..180), phone (min 1, max 20), email (optional email or empty string), coverImageUrl (optional url), isActive (bool), displayOrder (int min 0), openingHours (array of 7 items with dayOfWeek 0–6, openTime/closeTime regex `/^\d{2}:\d{2}$/`, isClosed bool)

- [ ] T031 [P] Create `[fe]/src/features/stores/index.ts` — re-export everything from api.ts, hooks.ts, types.ts, schema.ts, utils.ts (stub utils for now)

### Frontend — Admin Components

- [ ] T032 Create `[fe]/src/features/stores/components/store-form.tsx` — admin create/edit form using React Hook Form + Zod resolver from schema.ts; sections: Basic (name, slug auto-gen from name, address, district, city, province), Location (latitude, longitude number inputs), Contact (phone, email), Cover Image (URL input), Opening Hours (7-row table: day label | Closed checkbox | Open time input | Close time input — time inputs hidden/disabled when isClosed=true), Settings (displayOrder number, isActive toggle); on submit calls `onSave(values: StoreFormValues)` prop

- [ ] T033 Create `[fe]/src/features/stores/components/store-list-table.tsx` — DataTable column configuration for admin store list: Name, City, Phone, Status badge (Active/Inactive), Display Order, Actions (Edit button → `ROUTES.ADMIN.STORES_EDIT(id)`, Delete button → triggers ConfirmDialog); export `storeColumns` column array

### Frontend — Admin Pages

- [ ] T034 Add "Stores" nav item to `[fe]/src/app/admin/layout.tsx` — insert `{ href: ROUTES.ADMIN.STORES, label: "Stores", icon: MapPin }` after Banners and before Orders; import `MapPin` from `lucide-react`

- [ ] T035 Create `[fe]/src/app/admin/stores/page.tsx` — admin store list page; fetch via `useAdminStores()`; render: page header with "Add Store" button → `ROUTES.ADMIN.STORES_NEW`, city `<select>` filter + status `<select>` filter (controlled state), `<DataTable>` using `storeColumns` from store-list-table.tsx; handle delete via `deleteStore(id)` with ConfirmDialog + toast; handle status toggle stub (US2); Tabs component: "Stores" tab (list) + "Ordering" tab stub (US5)

- [ ] T036 Create `[fe]/src/app/admin/stores/new/page.tsx` — create store page; render `<StoreForm>` with empty default values (7 days pre-populated with default open 07:00–22:00, isClosed=false); on submit call `createStore(body)`, show success toast, redirect to `ROUTES.ADMIN.STORES`

- [ ] T037 Create `[fe]/src/app/admin/stores/edit/[id]/page.tsx` — edit store page; load store via `useAdminStore(id)`; render `<StoreForm>` pre-filled with store data; on submit call `updateStore(id, body)`, show success toast, redirect to `ROUTES.ADMIN.STORES`

**Checkpoint**: US1 complete. Admin can list/create/edit/delete stores. Test by navigating to `/admin/stores`, creating a store with 7-day hours, editing it, and deleting it.

---

## Phase 4: User Story 2 — Admin Toggles Store Visibility (Priority: P2)

**Goal**: Admin can activate/deactivate a store with a single toggle on the admin list page without going through the full edit form.

**Independent Test**: Admin clicks the status toggle on a store row in `/admin/stores` → status badge flips → navigating to `/stores` confirms the store is hidden/visible accordingly.

### Backend — Status Command

- [ ] T038 Create `UpdateStoreStatusCommand.cs` + `UpdateStoreStatusCommandHandler.cs` in `[be]/MoriiCoffee.Application/Commands/Store/UpdateStoreStatus/` — handler: load store by id (throw NotFoundException if not found), call `store.SetStatus(dto.IsActive)`, `CommitAsync()`, return `StoreDto`

- [ ] T039 Add `PATCH /{id}/status` endpoint to `[be]/MoriiCoffee.Presentation/Controllers/AdminStoresController.cs` — `[HttpPatch("{id:guid}/status"), Authorize(Roles = "ADMIN")]`, dispatches `UpdateStoreStatusCommand`, returns `ApiOkResponse(StoreDto)`

### Frontend — Status Toggle UI

- [ ] T040 Wire status toggle into `[fe]/src/features/stores/components/store-list-table.tsx` — add a toggle button (or inline badge click) in the Status column that calls `updateStoreStatus(id, { isActive: !current })` from api.ts, shows a loading state, shows success/error toast via `useAdminStores().refetch()`; implement `updateStoreStatus` in `[fe]/src/features/stores/api.ts`

**Checkpoint**: US2 complete. Toggle a store inactive in admin → verify it disappears from `/stores` (after US3 is done); for now verify API call returns 200 with updated store.

---

## Phase 5: User Story 3 — Public Visitor Finds a Store Near Them (Priority: P2)

**Goal**: Public `/stores` page shows live store data with open/closed status, geolocation-based sorting, city filtering, and text search. Home page preview uses live data.

**Independent Test**: Visit `/stores` — all active stores visible with open/closed badges. Click "Near Me" — list re-sorts by distance. Select a city — list filters. Type in search box — results filter. Visit home page — preview shows 3 live stores.

### Backend — Public Queries

- [ ] T041 Create `GetPublicStoresQuery.cs` + `GetPublicStoresQueryHandler.cs` in `[be]/MoriiCoffee.Application/Queries/Store/GetPublicStores/` — query params: PaginationFilter, double? latitude, double? longitude, double? radius, string? city, string? search; handler: `FindAll(false).Where(s => s.IsActive).Include(s => s.OpeningHours)`, apply city filter (case-insensitive contains), apply search filter (Name or Address contains), materialize to list; if latitude+longitude provided: compute Haversine distance for each store, optionally filter by radius, sort by distanceKm ascending, set `DistanceKm` on each StoreDto; if no geo params: sort by `DisplayOrder ASC`, `DistanceKm = null`; apply manual pagination (skip/take on the sorted list); return `Pagination<StoreDto>`

  Haversine formula (for handler):
  ```csharp
  static double HaversineKm(double lat1, double lon1, double lat2, double lon2) {
      const double R = 6371;
      var dLat = (lat2 - lat1) * Math.PI / 180;
      var dLon = (lon2 - lon1) * Math.PI / 180;
      var a = Math.Sin(dLat/2)*Math.Sin(dLat/2) +
              Math.Cos(lat1*Math.PI/180)*Math.Cos(lat2*Math.PI/180)*
              Math.Sin(dLon/2)*Math.Sin(dLon/2);
      return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
  }
  ```

- [ ] T042 Create `GetPublicStoreByIdQuery.cs` + `GetPublicStoreByIdQueryHandler.cs` in `[be]/MoriiCoffee.Application/Queries/Store/GetPublicStoreById/` — handler: `FindByCondition(s => s.Id == id && s.IsActive, false).Include(s => s.OpeningHours).FirstOrDefaultAsync()`, throw NotFoundException if null, map to StoreDto

- [ ] T043 Create `StoresController.cs` in `[be]/MoriiCoffee.Presentation/Controllers/StoresController.cs` — route `api/v1/stores`, no `[Authorize]`; implement: `GET /` (→ GetPublicStoresQuery with all query params), `GET /{id:guid}` (→ GetPublicStoreByIdQuery); wrap responses in `ApiOkResponse`

### Frontend — Utilities & Components

- [ ] T044 [P] Create `[fe]/src/features/stores/utils.ts` — implement:
  - `getTodayHours(hours: ApiStoreOpeningHours[]): ApiStoreOpeningHours | null` — finds entry where `dayOfWeek === new Date().getDay()`
  - `getOpeningStatus(hours: ApiStoreOpeningHours[]): string` — returns "Closed today" | "Opens at HH:MM AM/PM" | "Open · Closes at HH:MM AM/PM" | "Closed"
  - `formatTime(hhmm: string): string` — "HH:mm" 24h → "H:MM AM/PM" 12h
  - `formatTimeRange(openTime, closeTime): string` — "H:MM AM – H:MM PM"
  - `getDistanceLabel(km: number | null): string | null` — null → null; < 1 → "X m away"; ≥ 1 → "X.X km away"

- [ ] T045 [P] Create `[fe]/src/features/stores/components/store-hours-badge.tsx` — chip/badge component that accepts `hours: ApiStoreOpeningHours[]`, calls `getOpeningStatus()`, renders a colored badge: green for "Open", red for "Closed today"/"Closed", amber for "Opens at..."

- [ ] T046 [P] Create `[fe]/src/features/stores/components/store-opening-hours.tsx` — component that renders a full 7-day schedule table; accepts `hours: ApiStoreOpeningHours[]`; shows day name (Sun–Sat), time range via `formatTimeRange()` or "Closed", highlights today's row

- [ ] T047 [P] Create `[fe]/src/features/stores/components/store-city-filter.tsx` — accepts `cities: string[]` and `selected: string | null` and `onChange: (city: string | null) => void`; renders pill buttons or a `<select>` with "All Cities" default; purely presentational (no API calls)

- [ ] T048 Create `[fe]/src/features/stores/components/store-card.tsx` — store card for public list; accepts `store: ApiStore` and `isSelected?: boolean` and `onClick?: () => void`; renders: cover image (or placeholder), store name, address, city, phone, `<StoreHoursBadge>`, distance label via `getDistanceLabel(store.distanceKm)`, `<StoreOpeningHours>` expandable section; highlighted border/bg when `isSelected`

### Frontend — Public Hooks & API

- [ ] T049 Implement `useStores(query: StoresQuery)` and `useStore(id: string | null)` in `[fe]/src/features/stores/hooks.ts` — follow same pattern as `useAdminStores`; `useStores` accepts full `StoresQuery` including latitude/longitude/radius/city/search

### Frontend — Public Page & Home Preview

- [ ] T050 Refactor `[fe]/src/app/stores/page.tsx` — replace all static dummy data with `useStores({ takeAll: true })` hook; add state: `selectedCity`, `searchText`, `userLocation` (null | {lat, lng}), `selectedStoreId`; render: page header with search input, `<StoreCityFilter>` (cities derived from store list), "Near Me" button (triggers geolocation, sets userLocation state, calls `useStores` with lat/lng); two-column layout: left = filtered store list using `<StoreCard>` (filter client-side by city/search), right = map placeholder div (wired in US4); handle empty state (no stores); handle geolocation denied gracefully

- [ ] T051 Refactor `[fe]/src/components/home/store-locator-preview.tsx` — replace static `stores.slice(0, 3)` with `useStores({ size: 3 })` hook; render first 3 active stores using `<StoreCard>` (simplified, no geo); keep existing "View All" → `/stores` link

- [ ] T052 Add i18n keys to `[fe]/src/messages/en.json` (or wherever locale files are stored) — all `stores.*` keys from brainstorm.md section 7 (title, subtitle, searchPlaceholder, filterAllCities, nearMe, locating, locationDenied, noResults, noResultsDescription, openNow, closedNow, closesAt, opensAt, closedToday, distanceAway, hours, address, phone, todayHours)

- [ ] T053 Add i18n keys to `[fe]/src/messages/vi.json` — Vietnamese translations for all `stores.*` keys

**Checkpoint**: US3 complete. `/stores` shows live data, open/closed badges work, geolocation sorts by distance, city filter and search work client-side, home page preview shows 3 live stores.

---

## Phase 6: User Story 4 — Public Visitor Explores Stores on a Map (Priority: P3)

**Goal**: Interactive Google Map alongside the store list — pins for each active store, click-to-highlight sync between map and list, InfoWindow with store summary.

**Independent Test**: Map renders with pins for all active stores. Clicking a pin opens InfoWindow with store details and highlights the list card. Clicking a list card centers the map on that store's pin.

### Frontend — Map Component

- [ ] T054 Install `@googlemaps/js-api-loader` in `[fe]/` via `pnpm add @googlemaps/js-api-loader`

- [ ] T055 Create `[fe]/src/features/stores/components/store-map.tsx` — Google Maps component using `@googlemaps/js-api-loader`; accepts `stores: ApiStore[]`, `selectedStoreId: string | null`, `onStoreSelect: (id: string) => void`; on load: initialize map centered on Vietnam (fallback: 10.7769, 106.7009), add one `Marker` per store at store lat/lng; clicking a marker calls `onStoreSelect(store.id)` and opens an InfoWindow with store name, address, phone, today's hours summary (from `getOpeningStatus`); if `selectedStoreId` changes, pan map to that store and open its InfoWindow; if `NEXT_PUBLIC_GOOGLE_MAPS_KEY` is missing/undefined, render a fallback div ("Map not available") so the list view still works

### Frontend — Map Integration

- [ ] T056 Integrate `<StoreMap>` into `[fe]/src/app/stores/page.tsx` — replace the placeholder div in the right column with `<StoreMap stores={filteredStores} selectedStoreId={selectedStoreId} onStoreSelect={setSelectedStoreId} />`; ensure `selectedStoreId` state is shared between the list (`StoreCard` onClick) and the map component; when a card is clicked, update `selectedStoreId`; when map fires `onStoreSelect`, update `selectedStoreId` and scroll the matching card into view

**Checkpoint**: US4 complete. Map renders with pins, pin click highlights card, card click centers map. If API key absent, list still works.

---

## Phase 7: User Story 5 — Admin Reorders Store Display Sequence (Priority: P4)

**Goal**: Admin/Staff can drag-and-drop stores to change their display order on the public page. Save fires a bulk PATCH that updates DisplayOrder for all stores.

**Independent Test**: Admin opens Ordering tab → drags a store to new position → saves → navigates to `/stores` → public page reflects new order.

### Backend — Reorder Command

- [ ] T057 Create `ReorderStoresCommand.cs` + `ReorderStoresCommandHandler.cs` in `[be]/MoriiCoffee.Application/Commands/Store/ReorderStores/` — handler: for each `ReorderStoreItem` in dto.Items, load store by id (throw NotFoundException if any id missing), call `store.SetDisplayOrder(item.DisplayOrder)`, batch update all, single `CommitAsync()`, return void

- [ ] T058 Add `PATCH /reorder` endpoint to `[be]/MoriiCoffee.Presentation/Controllers/AdminStoresController.cs` — `[HttpPatch("reorder")]` (no extra role override — class-level ADMIN+STAFF applies), dispatches `ReorderStoresCommand`, returns `ApiOkResponse()`

### Frontend — Ordering Tab

- [ ] T059 Add drag-and-drop store ordering to the "Ordering" tab in `[fe]/src/app/admin/stores/page.tsx` — fetch all stores with `useAdminStores({ takeAll: true })`; render a sortable list (can use HTML5 drag-and-drop or a simple up/down button approach — keep it simple, no external DnD library required); track local order state; "Save Order" button calls `reorderStores({ items: orderedStores.map((s, i) => ({ id: s.id, displayOrder: i + 1 })) })`, shows success toast

- [ ] T060 Implement `reorderStores(body: ReorderStoresRequest)` in `[fe]/src/features/stores/api.ts` and add i18n keys for admin stores (`adminStores.*` from brainstorm.md section 7) to `[fe]/src/messages/en.json` and `[fe]/src/messages/vi.json`

**Checkpoint**: US5 complete. Reorder stores in admin, save, verify public page shows new order.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Cleanup, documentation, and final verification.

- [ ] T061 Delete `[fe]/src/data/stores.ts` — only after confirming `/stores` and home preview use live API data (US3 complete). Also remove any remaining import of `stores.ts` across the frontend codebase (check home page, stores page).

- [ ] T062 [P] Write summary doc (English) at `docs/explanations/summary-store-management-ENG.md` — cover: what was implemented and why, files created/modified, database changes (2 new tables), API changes (9 new endpoints), business rules (7 opening hours, soft-delete, geolocation sort), how to verify

- [ ] T063 [P] Write summary doc (Vietnamese) at `docs/explanations/summary-store-management-VN.md` — same content in Vietnamese

- [ ] T064 Run full Definition of Done checklist from `quickstart.md` — verify each item is checked: migration applied, 5 seed stores, all 9 endpoints return correct responses, soft-delete works, geolocation sort works, `/stores` shows live data, home preview uses live data, admin CRUD works end-to-end, ordering works, i18n keys present in en/vi

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user stories**
- **US1 Backend (T021–T026)**: Depends on Phase 2 — can parallel US3 backend
- **US1 Frontend (T027–T037)**: Depends on Phase 2 (T018–T020) — can parallel US3 frontend
- **US2 Backend (T038–T039)**: Depends on US1 backend (AdminStoresController exists)
- **US2 Frontend (T040)**: Depends on US1 frontend (store-list-table.tsx exists)
- **US3 Backend (T041–T043)**: Depends on Phase 2 — can parallel US1 backend
- **US3 Frontend (T044–T053)**: Depends on Phase 2 frontend (T018–T020) — can parallel US1 frontend
- **US4 Frontend (T054–T056)**: Depends on US3 frontend (stores/page.tsx exists)
- **US5 Backend (T057–T058)**: Depends on Phase 2 (AdminStoresController can be extended)
- **US5 Frontend (T059–T060)**: Depends on US1 frontend (admin stores page exists)
- **Polish (Phase 8)**: Depends on all desired user stories complete

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|-------|-----------|-----------------|
| US1 (P1) | Phase 2 complete | US3 backend/frontend |
| US2 (P2) | US1 backend (controller) + US1 frontend (table) | US3 |
| US3 (P2) | Phase 2 complete | US1 |
| US4 (P3) | US3 frontend (stores page) | US5 |
| US5 (P4) | US1 frontend (admin page) | US4 |

### Within Each User Story

- Backend commands before controller endpoints
- Backend queries before controller endpoints
- Frontend types/api/hooks before components
- Frontend components before pages
- Pages verified manually before marking story complete

---

## Parallel Opportunities

### Phase 2 — Parallel (once T002–T005 domain layer is done):

```
T006 StoreDto          ←─┐
T007 StoreOpeningHoursDto  │ All DTOs in parallel
T008 CreateStoreDto      ├─┤ after domain entities
T009 UpdateStoreStatusDto │
T010 StoreMapper       ←─┘

T011 StoreConfiguration   ←─┐  EF configs in parallel
T012 StoreOpeningHoursConfig ─┘  after domain entities

T013 StoresRepository.cs
T018 [fe] api.ts types    ─── All in parallel after T002–T005
T019 [fe] api-endpoints   ─┐
T020 [fe] routes.ts       ─┘
```

### Phase 3 — Parallel within US1:

```
Backend commands in parallel (T021, T022, T023)
Backend queries in parallel (T024, T025)
Frontend foundation in parallel (T027, T028, T029, T030, T031)
Frontend components in parallel (T032, T033) after frontend foundation
```

### Phase 5 — Parallel within US3:

```
T044 utils.ts     ─┐
T045 hours-badge  ─┤ All parallel after Phase 2
T046 opening-hrs  ─┤ (no inter-dependencies)
T047 city-filter  ─┘

T041 GetPublicStoresQuery  ─┐
T042 GetPublicStoreById    ─┘ Parallel backend queries

T052 en.json  ─┐  Parallel i18n
T053 vi.json  ─┘
```

---

## Implementation Strategy

### MVP First (US1 Only — Admin CRUD)

1. Complete Phase 1 (T001)
2. Complete Phase 2 Foundation (T002–T020)
3. Complete Phase 3 US1 Backend (T021–T026)
4. Complete Phase 3 US1 Frontend (T027–T037)
5. **STOP and VALIDATE**: Test admin create/edit/delete via admin panel
6. **Deploy/Demo**: Admin can manage stores — backend ready for public page

### Incremental Delivery

1. Foundation → Admin CRUD (US1) → **MVP**
2. Add Status Toggle (US2) → seamless admin workflow
3. Add Public Locator (US3) → customer-facing value delivered
4. Add Maps (US4) → enhanced discovery experience
5. Add Reorder (US5) → complete operational control

### Parallel Team Strategy

With 2 developers after Phase 2 complete:
- **Developer A**: US1 backend (T021–T026) + US2 backend (T038–T039) + US5 backend (T057–T058)
- **Developer B**: US1 frontend (T027–T037) + US3 frontend (T044–T053) + US4 frontend (T054–T056)
- Both: Phase 2 together, then split at Phase 3

---

## Task Count Summary

| Phase | Tasks | Notes |
|-------|-------|-------|
| Phase 1: Setup | 1 | Verify baseline |
| Phase 2: Foundational | 19 (T002–T020) | Backend + frontend blocking prerequisites |
| Phase 3: US1 Admin CRUD | 17 (T021–T037) | P1 — MVP |
| Phase 4: US2 Status Toggle | 3 (T038–T040) | P2 |
| Phase 5: US3 Public Locator | 13 (T041–T053) | P2 |
| Phase 6: US4 Maps | 3 (T054–T056) | P3 |
| Phase 7: US5 Reorder | 4 (T057–T060) | P4 |
| Phase 8: Polish | 4 (T061–T064) | Cleanup + docs |
| **Total** | **64 tasks** | |

---

## Notes

- `[P]` tasks = different files, no blocking dependencies on each other — run in parallel
- `[USn]` label maps each task to its user story for traceability
- Each phase checkpoint must pass before moving to the next story
- Commit after each logical group (e.g., "feat(stores): add Store domain entity", "feat(stores): add GetPublicStores query handler")
- Backend tasks follow Clean Architecture layer order: Domain → Application → Infrastructure → Presentation
- Frontend tasks follow: types/constants → api/hooks → utils → components → pages
