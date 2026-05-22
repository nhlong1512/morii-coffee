# API Contracts: Store Management (015)

**Base URL**: `http://localhost:8002/api`
**Response envelope**: `{ "statusCode": 200, "message": "Success", "data": <T> }`
**Auth**: JWT Bearer token in `Authorization` header

---

## Public Endpoints (no auth required)

### GET /v1/stores

List all active, non-deleted stores.

**Query parameters**:

| Param | Type | Description |
|-------|------|-------------|
| `page` | `int` | Page number (default: 1) |
| `size` | `int` | Page size (default: 10) |
| `takeAll` | `bool` | Fetch all records ignoring paging |
| `latitude` | `double?` | User latitude → triggers distance sort + `distanceKm` |
| `longitude` | `double?` | User longitude |
| `radius` | `double?` | Search radius in km (only applies when lat/lng provided) |
| `city` | `string?` | Filter by city (case-insensitive contains) |
| `search` | `string?` | Text search on Name + Address |

**Response**: `200 OK` — `ApiOkResponse(Pagination<StoreDto>)`

```json
{
  "statusCode": 200,
  "message": "Success",
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Morii Coffee - District 1",
        "slug": "district-1",
        "address": "42 Nguyen Hue Boulevard",
        "district": "District 1",
        "city": "Ho Chi Minh City",
        "province": null,
        "latitude": 10.7739,
        "longitude": 106.7029,
        "phone": "+84 28 1234 5678",
        "email": null,
        "coverImageUrl": null,
        "isActive": true,
        "displayOrder": 1,
        "distanceKm": 1.4,
        "openingHours": [
          { "id": "...", "dayOfWeek": 0, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
          { "id": "...", "dayOfWeek": 1, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
          { "id": "...", "dayOfWeek": 2, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
          { "id": "...", "dayOfWeek": 3, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
          { "id": "...", "dayOfWeek": 4, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
          { "id": "...", "dayOfWeek": 5, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
          { "id": "...", "dayOfWeek": 6, "openTime": "07:00", "closeTime": "22:00", "isClosed": false }
        ],
        "createdAt": "2025-01-01T00:00:00Z",
        "updatedAt": null
      }
    ],
    "metadata": {
      "currentPage": 1,
      "totalPages": 1,
      "pageSize": 10,
      "totalCount": 5,
      "payloadSize": 5,
      "hasPrevious": false,
      "hasNext": false,
      "takeAll": false
    }
  }
}
```

**Notes**:
- `distanceKm` is `null` when no lat/lng params provided
- Results ordered by `DisplayOrder ASC` by default; by `distanceKm ASC` when lat/lng provided
- Only `IsActive = true`, `IsDeleted = false` stores returned

---

### GET /v1/stores/{id}

Single active store detail.

**Path**: `{id}` — Guid

**Response**: `200 OK` — `ApiOkResponse(StoreDto)` (same shape as list item)

**Errors**:
- `404 Not Found` — if store not found, `IsDeleted = true`, or `IsActive = false`

---

## Admin Endpoints (require auth)

### GET /v1/admin/stores

Admin store list — includes inactive stores, excludes soft-deleted.

**Auth**: `ADMIN` or `STAFF` role required

**Query parameters**:

| Param | Type | Description |
|-------|------|-------------|
| `page` | `int` | Page number |
| `size` | `int` | Page size |
| `takeAll` | `bool` | Fetch all |
| `isActive` | `bool?` | `true`/`false`/omit for all |
| `city` | `string?` | City filter |
| `search` | `string?` | Name/address search |

**Response**: `200 OK` — `ApiOkResponse(Pagination<StoreDto>)` — same envelope as public

---

### GET /v1/admin/stores/{id}

Admin store detail by ID.

**Auth**: `ADMIN` or `STAFF`

**Response**: `200 OK` — `ApiOkResponse(StoreDto)`

**Errors**:
- `404 Not Found` — store not found or soft-deleted

---

### POST /v1/admin/stores

Create a new store.

**Auth**: `ADMIN` only

**Request body**:

```json
{
  "name": "Morii Coffee - District 3",
  "slug": null,
  "address": "15 Nam Ky Khoi Nghia",
  "district": "District 3",
  "city": "Ho Chi Minh City",
  "province": null,
  "latitude": 10.7769,
  "longitude": 106.6922,
  "phone": "+84 28 9876 5432",
  "email": "d3@moriicoffee.vn",
  "coverImageUrl": null,
  "isActive": true,
  "displayOrder": 2,
  "openingHours": [
    { "dayOfWeek": 0, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
    { "dayOfWeek": 1, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
    { "dayOfWeek": 2, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
    { "dayOfWeek": 3, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
    { "dayOfWeek": 4, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
    { "dayOfWeek": 5, "openTime": "07:00", "closeTime": "22:00", "isClosed": false },
    { "dayOfWeek": 6, "openTime": "07:00", "closeTime": "22:00", "isClosed": false }
  ]
}
```

**Validation**:
- `name`: required, max 200 chars, unique
- `address`, `city`, `phone`: required
- `latitude`: -90 to 90
- `longitude`: -180 to 180
- `openingHours`: exactly 7 items, `dayOfWeek` 0–6 each unique, `openTime`/`closeTime` format `HH:mm`
- `slug`: auto-generated from name if null/empty; must be unique if provided

**Response**: `201 Created` — `ApiCreatedResponse(StoreDto)`

**Errors**:
- `400 Bad Request` — validation failure (missing fields, invalid coords, wrong hours count)
- `409 Conflict` — name or slug already exists

---

### PUT /v1/admin/stores/{id}

Full update. Same body as POST. Replaces all 7 opening hours records.

**Auth**: `ADMIN` only

**Response**: `200 OK` — `ApiOkResponse(StoreDto)`

**Errors**:
- `404 Not Found`
- `400 Bad Request`
- `409 Conflict` — name/slug conflict with another store

---

### DELETE /v1/admin/stores/{id}

Soft-delete a store (`IsDeleted = true`, `DeletedAt = now`).

**Auth**: `ADMIN` only

**Response**: `204 No Content`

**Errors**:
- `404 Not Found`

---

### PATCH /v1/admin/stores/{id}/status

Toggle active status without full update.

**Auth**: `ADMIN` only

**Request body**:
```json
{ "isActive": false }
```

**Response**: `200 OK` — `ApiOkResponse(StoreDto)`

**Errors**:
- `404 Not Found`

---

### PATCH /v1/admin/stores/reorder

Bulk update display order for multiple stores.

**Auth**: `ADMIN` or `STAFF`

**Request body**:
```json
{
  "items": [
    { "id": "3fa85f64-...", "displayOrder": 1 },
    { "id": "4cb96a75-...", "displayOrder": 2 },
    { "id": "5dc07b86-...", "displayOrder": 3 }
  ]
}
```

**Response**: `200 OK` — `ApiOkResponse()` (no data payload)

**Errors**:
- `400 Bad Request` — empty items list
- `404 Not Found` — any ID in the list does not exist

---

## Frontend TypeScript Types (mirroring backend DTOs)

```typescript
// src/types/api.ts additions
interface ApiStore {
  id: string;
  name: string;
  slug: string;
  address: string;
  district: string | null;
  city: string;
  province: string | null;
  latitude: number;
  longitude: number;
  phone: string;
  email: string | null;
  coverImageUrl: string | null;
  isActive: boolean;
  displayOrder: number;
  distanceKm: number | null;
  openingHours: ApiStoreOpeningHours[];
  createdAt: string;
  updatedAt: string | null;
}

interface ApiStoreOpeningHours {
  id: string;
  dayOfWeek: number;   // 0=Sunday … 6=Saturday
  openTime: string;    // "HH:mm"
  closeTime: string;   // "HH:mm"
  isClosed: boolean;
}
```
