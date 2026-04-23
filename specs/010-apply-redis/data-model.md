# Data Model: Redis-Backed Core Flows

## 1. Catalog List Cache Entry

**Purpose**: Stores a paginated customer-facing product list for a specific filter combination.

**Key fields**

| Field | Type | Notes |
|-------|------|-------|
| `cacheKey` | string | Derived from page, page size, category filter, and featured filter |
| `items` | collection | Product summary DTO payload returned to the client |
| `pageNumber` | integer | Requested page |
| `pageSize` | integer | Requested page size |
| `totalCount` | integer | Total products matching the filter |
| `createdAtUtc` | datetime | Cache-write timestamp for observability |
| `expiresAtUtc` | datetime | Logical expiration window |

**Validation rules**

- Filter combinations must produce deterministic key values.
- Entry payload must represent client DTOs, never EF-tracked entities.
- Entry lifetime must not exceed the configured catalog list TTL.

**Relationships**

- Many list-cache entries can exist for the same underlying products.
- Entries are referenced indirectly through a tracked key-set used during invalidation.

## 2. Catalog Detail Cache Entry

**Purpose**: Stores the full customer-facing representation of one product detail view.

**Key fields**

| Field | Type | Notes |
|-------|------|-------|
| `productId` | guid | Product identifier |
| `product` | object | Product detail DTO including variants and images |
| `createdAtUtc` | datetime | Cache-write timestamp |
| `expiresAtUtc` | datetime | Logical expiration window |

**Validation rules**

- Product must exist and not be deleted at the time the cache entry is created.
- Variant pricing in the cached payload must match the mapped read model at cache-write time.

**Relationships**

- One detail-cache entry exists per product key.
- Product, variant, and image writes can invalidate the same detail entry.

## 3. Catalog Cache Key Registry

**Purpose**: Tracks active paginated list keys so write-side invalidation can remove them without scanning Redis.

**Key fields**

| Field | Type | Notes |
|-------|------|-------|
| `registryKey` | string | Stable Redis set key for catalog list entries |
| `members` | collection of string | Individual list-cache keys |
| `updatedAtUtc` | datetime | Last maintenance timestamp |

**Validation rules**

- Registry members must contain only active list-cache keys.
- Removing a list entry should also remove the key from the registry when feasible.

## 4. Customer Cart

**Purpose**: Represents one authenticated customer’s active order-in-progress.

**Key fields**

| Field | Type | Notes |
|-------|------|-------|
| `userId` | guid | Authenticated user identifier |
| `items` | collection of Cart Line | Selected variants |
| `grandTotal` | decimal | Sum of line totals |
| `updatedAtUtc` | datetime | Last successful cart mutation |
| `expiresAtUtc` | datetime | Activity-based expiry window |

**Validation rules**

- One active cart per authenticated user.
- Cart expires after inactivity according to the configured TTL.
- Grand total must equal the sum of current line totals stored in the cart snapshot.

**State transitions**

- `Empty` → `Active` when the first item is added.
- `Active` → `Active` when items are updated, removed, or merged.
- `Active` → `Empty` when all items are removed or the cart key is deleted.
- `Active` → `Consumed` when checkout succeeds and the key is deleted.
- `Active` → `Expired` when TTL elapses without refresh.

## 5. Cart Line

**Purpose**: Represents one selected product variant inside the cart snapshot.

**Key fields**

| Field | Type | Notes |
|-------|------|-------|
| `productId` | guid | Parent product identifier |
| `variantId` | guid | Selected variant identifier |
| `productName` | string | Snapshotted display name |
| `variantName` | string | Snapshotted option name |
| `thumbnailUrl` | string? | Snapshotted display image |
| `unitPrice` | decimal | Snapshotted price at add-to-cart time |
| `quantity` | integer | Customer-selected quantity |
| `lineTotal` | decimal | `unitPrice * quantity` |

**Validation rules**

- `quantity` must be greater than zero for stored lines.
- A cart may contain at most one line per `variantId`.
- Variant must exist and be available when added or updated.

## 6. Password Reset Ticket

**Purpose**: Authorizes a single password reset attempt through an opaque, short-lived ticket.

**Key fields**

| Field | Type | Notes |
|-------|------|-------|
| `ticketId` | string | Opaque identifier sent to the client |
| `userId` | guid | Account being recovered |
| `email` | string | Stored for audit/support context if needed |
| `identityToken` | string | Server-side reset credential used for final password reset |
| `createdAtUtc` | datetime | Ticket issuance time |
| `expiresAtUtc` | datetime | Hard expiration |
| `consumedAtUtc` | datetime? | Set when reset succeeds |

**Validation rules**

- Ticket must be unexpired and unconsumed to be used.
- Ticket must be deleted or marked unusable immediately after successful reset.
- Only the latest active ticket for a user should remain valid.

**State transitions**

- `Issued` → `Consumed` on successful reset.
- `Issued` → `Expired` when TTL elapses.
- `Issued` → `Superseded` when a newer ticket is created for the same user.
