# Research: Store Management (015)

**Date**: 2026-05-22
**Branch**: `015-store-management`

---

## Decision 1: Backend Entity Pattern

**Decision**: Use `AggregateRoot` for `Store`, `EntityBase` (via `RepositoryBase<T>`) for `StoreOpeningHours`.

**Rationale**: Matches the existing pattern — `Banner`, `Order`, `BlogPost` all extend `AggregateRoot`. Child records (e.g., `OrderItem`) extend `EntityBase` directly. Store has its own lifecycle (create/update/delete) and domain identity, making it an aggregate root. `StoreOpeningHours` is always created/replaced with its parent Store.

**Alternatives considered**: N/A — pattern is mandated by the codebase.

**Evidence**: `source/MoriiCoffee.Domain/SeedWork/AggregateRoot/AggregateRoot.cs`, `Banner.cs`, `Order.cs`

---

## Decision 2: Repository Pattern

**Decision**: `StoresRepository` extends `RepositoryBase<Store>` and implements `IStoresRepository`. Add `IStoresRepository Stores` to `IUnitOfWork`. Use lazy initialization (`??=`) in `UnitOfWork` implementation.

**Rationale**: Every existing repository follows `RepositoryBase<T>` + interface pattern. `UnitOfWork` uses lazy `??=` initialization for each repository property. `FindAll(trackChanges: false)` already filters `DeletedAt == null` so soft-delete is transparent.

**Evidence**: `UnitOfWork.cs` (lazy `??=`), `RepositoryBase.cs` (`FindAll` with `DeletedAt == null`).

---

## Decision 3: Distance Computation (Geolocation)

**Decision**: Compute Haversine distance **in memory** after EF query returns results.

**Rationale**: Store count is small (< 100 for MVP). PostgreSQL spatial extensions (PostGIS) would require additional infrastructure setup. In-memory Haversine is correct and fast enough. The query handler will: (1) execute EF query with city/search filters, (2) materialize to list, (3) if lat/lng provided, compute distance and sort ascending, (4) paginate the result.

**Alternatives considered**: PostGIS `ST_Distance` — rejected because it requires PostGIS extension on the database and adds migration complexity. Raw SQL Haversine expression in LINQ — possible but less readable; keep for future optimization if needed.

---

## Decision 4: Opening Hours Strategy

**Decision**: Store exactly 7 `StoreOpeningHours` child records per `Store` (one per `DayOfWeek` 0–6). Replace all 7 on update via cascade delete + re-insert.

**Rationale**: The spec requires exactly 7 entries. On `PUT /admin/stores/{id}` (full update), the handler will remove all existing `StoreOpeningHours` for the store and insert 7 fresh ones. This avoids complex merge logic. EF Core cascade delete (`OnDelete(DeleteBehavior.Cascade)`) handles cleanup.

**Alternatives considered**: Upsert per day — more complex, requires tracking which days changed. Rejected in favor of simplicity (replace-all on update).

---

## Decision 5: AutoMapper for Store

**Decision**: `StoreMapper` does a simple `CreateMap<Store, StoreDto>()` and `CreateMap<StoreOpeningHours, StoreOpeningHoursDto>()`. No CDN URL transformation needed.

**Rationale**: `CoverImageUrl` is stored as a plain URL string (or null) — the admin form accepts a URL directly. Unlike `Banner.ImageUrl` (MinIO key resolved via CDN), `Store.CoverImageUrl` is already a full URL. The mapper requires no constructor injection beyond the standard AutoMapper `Profile` base.

**Alternatives considered**: CDN resolution — not needed since URL is stored verbatim.

---

## Decision 6: Frontend Feature Folder Location

**Decision**: `src/features/stores/` in the Next.js frontend codebase with the standard file set: `api.ts`, `hooks.ts`, `types.ts`, `schema.ts`, `utils.ts`, `index.ts`, `components/`.

**Rationale**: Every other feature (blogs, banners, wishlist) uses this same structure. Consistency is mandatory.

**Evidence**: `src/features/blogs/` structure confirmed by agent research.

---

## Decision 7: City Filtering (Client-Side)

**Decision**: City filter is client-side — derive unique city values from the fetched store list, filter in-browser. The API supports `city` query param for server-side filtering but the public page will not use it.

**Rationale**: With < 100 stores, fetching all and filtering client-side is acceptable and simpler. Avoids a second API request. The admin page will use the server-side `city` param for large filtered lists.

---

## Decision 8: Admin Nav Item Position

**Decision**: Insert "Stores" between "Banners" and "Orders" in the admin sidebar `navItems` array. Use `MapPin` icon from `lucide-react`.

**Rationale**: Logical grouping — Stores is a physical/operational concept, similar to Banners (marketing) and adjacent to Orders (operational). `MapPin` is already imported in the existing stores page.

**Evidence**: `src/app/admin/layout.tsx` navItems: `[Dashboard, Products, Blogs, Banners, Orders, Users, Promotions]`

---

## Decision 9: Slug Auto-Generation

**Decision**: Backend auto-generates slug from name using a slugify helper (lowercase, replace spaces with `-`, remove special chars) if `CreateStoreDto.Slug` is null/empty.

**Rationale**: Same pattern as `BlogPost` slug generation. Ensures URL-safe identifiers without requiring admin input.

---

## Known Constraints & Notes

- `DistanceKm` on `StoreDto` is **not** mapped via AutoMapper — it is set manually in the query handler after Haversine computation.
- `StoreOpeningHours` does NOT extend `AggregateRoot` (no soft-delete needed — it is always replaced with parent).
- The `[Table("StoreOpeningHours")]` child entity: `IsClosed = true` means time inputs are irrelevant; the "open now" logic on the frontend ignores `OpenTime`/`CloseTime` when `IsClosed = true`.
- Public endpoint returns `IsActive = true` stores only; admin endpoint returns all (including inactive) but excludes soft-deleted.
- Existing `STORES.LIST` and `STORES.DETAIL` endpoints already defined in `src/constants/api-endpoints.ts` — just need admin endpoints added.
- The frontend codebase is at `morii-coffee-fe/` within the repository, not `src/` at root level.
