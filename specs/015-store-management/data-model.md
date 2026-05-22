# Data Model: Store Management (015)

**Date**: 2026-05-22
**Branch**: `015-store-management`

---

## Entities

### Store (Aggregate Root)

**Table**: `Stores`
**Base class**: `AggregateRoot` → `EntityBase` (provides `IsDeleted`, `CreatedAt`, `UpdatedAt`, `DeletedAt`)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, required | Auto-generated on create |
| `Name` | `string` | Required, MaxLength(200), Unique | Display name of the branch |
| `Slug` | `string` | Required, MaxLength(200), Unique | URL-safe identifier; auto-generated from Name if omitted |
| `Address` | `string` | Required, MaxLength(500) | Street address |
| `District` | `string?` | MaxLength(100) | Optional district/ward |
| `City` | `string` | Required, MaxLength(100), Indexed | City for filtering |
| `Province` | `string?` | MaxLength(100) | Optional province |
| `Latitude` | `double` | Required | Geographic coordinate |
| `Longitude` | `double` | Required | Geographic coordinate |
| `Phone` | `string` | Required, MaxLength(20) | Contact phone number |
| `Email` | `string?` | MaxLength(100) | Optional contact email |
| `CoverImageUrl` | `string?` | MaxLength(500) | Optional full URL to cover image |
| `IsActive` | `bool` | Default: `true`, Indexed | Controls public visibility |
| `DisplayOrder` | `int` | Default: `0`, Indexed | Sort order on public page |
| `OpeningHours` | `ICollection<StoreOpeningHours>` | Navigation | Exactly 7 child records |
| *(inherited)* | `bool IsDeleted` | — | Soft-delete flag |
| *(inherited)* | `DateTime CreatedAt` | — | Auto-set |
| *(inherited)* | `DateTime? UpdatedAt` | — | Auto-set on update |
| *(inherited)* | `DateTime? DeletedAt` | — | Set on soft-delete |

**Indexes**:
- `UNIQUE (Slug)` — enforced at DB and application level
- `INDEX (City)` — supports city filtering queries
- `INDEX (IsActive)` — public endpoint filter
- `INDEX (DisplayOrder)` — public endpoint ordering

**Business rules**:
- Name and Slug must be unique across non-deleted stores
- Slug is auto-generated from Name (slugify: lowercase, spaces → `-`, special chars removed) if not provided
- `IsActive = false` hides store from public endpoints; admin can still see it
- Soft-delete (`IsDeleted = true`) excludes from all queries via `RepositoryBase.FindAll()`

---

### StoreOpeningHours (Child Entity)

**Table**: `StoreOpeningHours`
**Base class**: `EntityBase` (no soft-delete — always replaced with parent)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK | Auto-generated |
| `StoreId` | `Guid` | FK → `Stores.Id`, Indexed | Parent store reference |
| `Store` | `Store` | Navigation | Back-reference |
| `DayOfWeek` | `int` | 0–6 (0=Sunday, 6=Saturday) | Day of the week |
| `OpenTime` | `string` | MaxLength(5), format `HH:mm` | 24-hour opening time |
| `CloseTime` | `string` | MaxLength(5), format `HH:mm` | 24-hour closing time |
| `IsClosed` | `bool` | Default: `false` | True if store does not operate this day |

**Cardinality**: Exactly 7 records per Store (one per day). Enforced in application layer (validation) and by the replace-all update strategy.

**Business rules**:
- When `IsClosed = true`, `OpenTime` and `CloseTime` are ignored for "open now" computation
- On Store full update (`PUT`), all 7 `StoreOpeningHours` are deleted and re-inserted (cascade delete)
- `OpenTime`/`CloseTime` assume same-day windows only; cross-midnight is out of scope

---

## DTOs

### `StoreDto` (returned by all queries)

```
Id             : Guid
Name           : string
Slug           : string
Address        : string
District       : string?
City           : string
Province       : string?
Latitude       : double
Longitude      : double
Phone          : string
Email          : string?
CoverImageUrl  : string?
IsActive       : bool
DisplayOrder   : int
DistanceKm     : double?     ← computed in handler, NOT mapped by AutoMapper
OpeningHours   : StoreOpeningHoursDto[]
CreatedAt      : DateTime
UpdatedAt      : DateTime?
```

### `StoreOpeningHoursDto`

```
Id         : Guid
DayOfWeek  : int
OpenTime   : string
CloseTime  : string
IsClosed   : bool
```

### `CreateStoreDto` / `UpdateStoreDto` (input)

```
Name           : string         (required)
Slug           : string?        (optional; auto-generated from Name if null/empty)
Address        : string         (required)
District       : string?
City           : string         (required)
Province       : string?
Latitude       : double         (required)
Longitude      : double         (required)
Phone          : string         (required)
Email          : string?
CoverImageUrl  : string?
IsActive       : bool           (default: true)
DisplayOrder   : int
OpeningHours   : CreateStoreOpeningHoursDto[]   (exactly 7 items required)
```

### `CreateStoreOpeningHoursDto`

```
DayOfWeek  : int     (0–6)
OpenTime   : string  ("HH:mm")
CloseTime  : string  ("HH:mm")
IsClosed   : bool
```

### `UpdateStoreStatusDto`

```
IsActive : bool
```

### `ReorderStoresDto`

```
Items : List<ReorderStoreItem>
  └── Id           : Guid
      DisplayOrder : int
```

---

## State Transitions

```
[Active]  ←──PATCH /status─→  [Inactive]
   │                               │
   └──── DELETE ──────────────────►[Soft-Deleted]
                                   (never shown publicly;
                                    visible in admin with filter)
```

- A soft-deleted store cannot be reactivated via status toggle (it's excluded by `FindAll()`)
- There is no "restore" operation in scope for this feature

---

## EF Core Relationship

```
Store  1 ──── 7  StoreOpeningHours
              (OnDelete: Cascade)
```

- Store deletion (via `UnitOfWork.CommitAsync()` after soft-delete flag) does NOT cascade delete `StoreOpeningHours` — soft-delete only sets `IsDeleted = true` on the `Store` row.
- Physical row deletion (not used in this system) would cascade via `OnDelete(DeleteBehavior.Cascade)`.
- On `PUT /admin/stores/{id}` (full update): handler explicitly deletes all 7 `StoreOpeningHours` rows for the store, then inserts 7 new ones.

---

## Migration Impact

**New tables**: `Stores`, `StoreOpeningHours`

**No changes** to existing tables.

**EF Core migration command** (from Infrastructure project):
```bash
dotnet ef migrations add AddStoreManagement \
  --project source/MoriiCoffee.Infrastructure.Persistence \
  --startup-project source/MoriiCoffee.Presentation
```
