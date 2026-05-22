# Data Model: My Wishlist

**Branch**: `014-my-wishlist` | **Date**: 2026-05-22

---

## Backend Domain Entity

### `WishlistItem` (new)

**Location**: `source/MoriiCoffee.Domain/Aggregates/WishlistAggregate/WishlistItem.cs`  
**Table**: `WishlistItems`

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `Guid` | PK | Auto-generated |
| `UserId` | `Guid` | FK → AspNetUsers.Id, NOT NULL | Cascade delete on user delete |
| `ProductId` | `Guid` | FK → Products.Id, NOT NULL | Cascade delete on product delete |
| `AddedAt` | `DateTime` | NOT NULL | UTC, set at insert |

**Unique constraint**: `(UserId, ProductId)` — prevents duplicate wishlist entries.

**Index**: Clustered on `(UserId, ProductId)` — primary query pattern is "get all items for user".

```csharp
[Table("WishlistItems")]
public class WishlistItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation (not serialized)
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
```

---

## Backend DTOs

### `WishlistItemDto` (new)

**Location**: `source/MoriiCoffee.Application/SeedWork/DTOs/Wishlist/WishlistItemDto.cs`

```csharp
public class WishlistItemDto
{
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductSlug { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool InStock { get; set; }
    public DateTime AddedAt { get; set; }
}
```

### `WishlistDto` (new)

```csharp
public class WishlistDto
{
    public List<WishlistItemDto> Items { get; set; } = [];
    public DateTime? UpdatedAt { get; set; }
}
```

### `MergeGuestWishlistDto` (new)

```csharp
public class MergeGuestWishlistDto
{
    public List<GuestWishlistItemDto> GuestItems { get; set; } = [];
}

public class GuestWishlistItemDto
{
    public Guid ProductId { get; set; }
}
```

---

## Frontend Types

### `WishlistItem` (upgrade from `string[]`)

**Location**: `src/stores/wishlist-store.ts`

```typescript
export interface WishlistItem {
  productId: string;
  name: string;
  slug: string;
  price: number;
  image: string;
  inStock: boolean;
  addedAt: string;  // ISO 8601
}
```

### API Types (new)

**Location**: `src/types/api.ts`

```typescript
export interface ApiWishlistItem {
  productId: string;
  productName: string;
  productSlug: string;
  basePrice: number;
  thumbnailUrl: string | null;
  inStock: boolean;
  addedAt: string;
}

export interface ApiWishlist {
  items: ApiWishlistItem[];
  updatedAt: string | null;
}

export interface ApiMergeWishlistRequest {
  guestItems: Array<{ productId: string }>;
}
```

---

## State Machine: WishlistStore

### Store shape

```typescript
interface WishlistState {
  // Data
  items: WishlistItem[];
  pendingIds: Set<string>;        // in-flight productIds, not persisted

  // Sync state
  storageMode: "guest" | "authenticated";
  isReady: boolean;
  syncError: string | null;

  // Actions
  addItem: (item: WishlistItem) => Promise<void>;
  removeItem: (productId: string) => Promise<void>;
  isInWishlist: (productId: string) => boolean;
  clearWishlist: () => Promise<void>;
  totalItems: () => number;
  initializeForSession: (isAuthenticated: boolean) => Promise<void>;
  resetAfterLogout: () => void;
}
```

### Persisted state (localStorage, key: `"morii-wishlist"`)

```typescript
partialize: (state) => ({
  items: state.items,
  storageMode: state.storageMode,
})
```

`pendingIds`, `isReady`, `syncError` are runtime-only.

---

## Database Migration

**Name**: `AddWishlistItems` (EF Core migration)

**Changes**:
1. Create table `WishlistItems` with columns: Id, UserId, ProductId, AddedAt
2. Add FK: `WishlistItems.UserId → AspNetUsers.Id` (cascade delete)
3. Add FK: `WishlistItems.ProductId → Products.Id` (cascade delete)
4. Add unique constraint: `UQ_WishlistItems_UserId_ProductId`
5. Add index: `IX_WishlistItems_UserId` for efficient user-scoped queries

---

## Entity Relationships

```
User (AspNetUsers)
  └──< WishlistItem (WishlistItems)  [one-to-many, cascade delete]
           └──> Product (Products)   [many-to-one, cascade delete]
```

- One user can have many wishlist items
- Each wishlist item references exactly one product
- If a user is deleted, all their wishlist items are deleted (CASCADE)
- If a product is deleted, all wishlist items for that product are deleted (CASCADE)
