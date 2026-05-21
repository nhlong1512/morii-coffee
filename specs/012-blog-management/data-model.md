# Phase 1 — Data Model

**Feature**: 012-blog-management  
**Date**: 2026-05-21

This document defines the new blog-domain entities, relationships, validation rules, and state transitions required by the feature.

---

## ER overview

```text
┌──────────────────┐     N     1 ┌──────────────────────┐ 1     N ┌────────────────────────┐
│ BlogPost         │────────────►│ BlogPostCategory     │◄────────│ BlogCategory           │
│ (aggregate root) │             │ (join entity)        │         │ (aggregate root)       │
└──────────────────┘             └──────────────────────┘         └────────────────────────┘
```

---

## 1. `BlogPost` — new aggregate root

**File**: `source/MoriiCoffee.Domain/Aggregates/BlogPostAggregate/BlogPost.cs`

One row per editorial article managed in the internal CMS.

### Proposed fields

| Field | Type | Constraints | Purpose |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique blog post identifier |
| `Title` | `string` | required, max 200 | Human-readable post title |
| `Slug` | `string` | required, unique, max 200 | Public detail-page identifier |
| `Excerpt` | `string?` | nullable, max 1000 | Summary text for lists/cards |
| `ContentJson` | `string?` | nullable, persisted as JSON/text | Canonical editor representation |
| `ContentHtml` | `string` | required for published content | Rendered storefront snapshot |
| `CoverImageUrl` | `string?` | nullable, max 500 | Public CDN URL of cover image |
| `CoverImageFileName` | `string?` | nullable, max 500 | Internal storage key/object name |
| `SeoTitle` | `string?` | nullable, max 200 | Optional search/social override |
| `SeoDescription` | `string?` | nullable, max 500 | Optional meta description |
| `Status` | `EBlogPostStatus` | required | Publication lifecycle state |
| `IsFeatured` | `bool` | required | Whether post is included in featured sections |
| `DisplayOrder` | `int` | required, non-negative | Curated ordering control |
| `PublishedAt` | `DateTime?` | nullable | First public publish timestamp |
| `CreatedAt` | `DateTime` | inherited | Audit timestamp |
| `UpdatedAt` | `DateTime?` | inherited | Audit timestamp |
| `IsDeleted` | `bool` | inherited | Soft-delete marker |

### Relationships

- one `BlogPost` has many `BlogPostCategory` rows
- one `BlogPost` can belong to many `BlogCategory` rows through the join entity

### Proposed domain methods

- `Create(...)`
- `UpdateContent(...)`
- `UpdateMetadata(...)`
- `Publish()`
- `Archive()`
- `MoveToDraft()`
- `SetFeatured(bool isFeatured)`
- `SetDisplayOrder(int displayOrder)`
- `ReplaceCategories(IEnumerable<Guid> categoryIds)`

### Invariants

- `Slug` must be unique across non-deleted posts
- `DisplayOrder` cannot be negative
- `Published` posts must have non-empty `Title`, `Slug`, `ContentHtml`, and at least one linked category
- `PublishedAt` is set on first publish and retained for history unless business later requests reset-on-republish
- `IsFeatured = true` only affects storefront output when `Status = Published`

---

## 2. `BlogCategory` — new aggregate root

**File**: `source/MoriiCoffee.Domain/Aggregates/BlogCategoryAggregate/BlogCategory.cs`

Represents a curated grouping of blog posts for admin organization and storefront navigation.

### Proposed fields

| Field | Type | Constraints | Purpose |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique category identifier |
| `Name` | `string` | required, max 100 | Display name |
| `Slug` | `string` | required, unique, max 150 | Public category identifier |
| `Description` | `string?` | nullable, max 500 | Optional admin/storefront description |
| `DisplayOrder` | `int` | required, non-negative | Ordering control |
| `IsActive` | `bool` | required | Whether category is publicly available |
| `CreatedAt` | `DateTime` | inherited | Audit timestamp |
| `UpdatedAt` | `DateTime?` | inherited | Audit timestamp |
| `IsDeleted` | `bool` | inherited | Soft-delete marker |

### Relationships

- one `BlogCategory` has many `BlogPostCategory` rows
- one `BlogCategory` can be linked to many `BlogPost` rows

### Proposed domain methods

- `Create(...)`
- `Update(...)`
- `SetActive(bool isActive)`
- `SetDisplayOrder(int displayOrder)`

### Invariants

- `Slug` must be unique across non-deleted categories
- `DisplayOrder` cannot be negative
- deletion is blocked while any non-deleted blog post remains linked
- inactive categories are excluded from public category responses

---

## 3. `BlogPostCategory` — new join entity

**File**: `source/MoriiCoffee.Domain/Aggregates/BlogPostAggregate/Entities/BlogPostCategory.cs`

Represents the many-to-many relationship between posts and categories.

### Proposed fields

| Field | Type | Constraints | Purpose |
|---|---|---|---|
| `Id` | `Guid` or composite key | PK | Relationship identity |
| `BlogPostId` | `Guid` | FK → `BlogPost.Id` | Linked post |
| `BlogCategoryId` | `Guid` | FK → `BlogCategory.Id` | Linked category |
| `CreatedAt` | `DateTime` | inherited/explicit | Optional auditability of assignment |
| `UpdatedAt` | `DateTime?` | inherited/explicit | Optional auditability of assignment |

### Constraints

- unique pair on (`BlogPostId`, `BlogCategoryId`)
- cascade removal when the post is deleted or assignments are replaced

---

## 4. Enum: `EBlogPostStatus`

**File**: `source/MoriiCoffee.Domain.Shared/Enums/Blog/EBlogPostStatus.cs`

```csharp
public enum EBlogPostStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
```

### State transitions

```text
Draft ───────► Published
  │              │
  │              ├────────► Archived
  └──────────────┴────────► Draft

Archived ─────► Draft
Archived ─────► Published
```

Rules:

- `Published` is the only public state
- moving from `Published` to `Draft` or `Archived` immediately removes the post from public visibility
- `Archived` is for intentionally hidden-but-retained content

---

## 5. Read-model / DTO expectations

### `BlogCategoryDto`

Fields:

- `Id`
- `Name`
- `Slug`
- `Description`
- `DisplayOrder`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

### `BlogPostSummaryDto`

Fields:

- `Id`
- `Title`
- `Slug`
- `Excerpt`
- `CoverImageUrl`
- `Status`
- `IsFeatured`
- `DisplayOrder`
- `PublishedAt`
- `CreatedAt`
- `UpdatedAt`
- `Categories`

### `BlogPostDetailDto`

Extends summary with:

- `ContentHtml`
- `ContentJson`
- `SeoTitle`
- `SeoDescription`

---

## 6. Query/filter models

### Public blog list filter

Expected filter dimensions:

- page
- size
- takeAll
- category slug
- text search
- sort option

### Admin blog list filter

Expected filter dimensions:

- page
- size
- takeAll
- status
- category id
- text search

These should extend or align with the existing pagination conventions already used by the repo.

---

## 7. Storage and schema notes

### Database engine

Current backend persistence uses PostgreSQL through Npgsql.

### Recommended column shapes

| Field | Recommended DB type |
|---|---|
| `ContentJson` | `jsonb` or `text` |
| `ContentHtml` | `text` |
| `Excerpt` | `text` |
| `SeoDescription` | `varchar(500)` or `text` |
| `CoverImageUrl` | `varchar(500)` |
| `CoverImageFileName` | `varchar(500)` |

### Indexes

`BlogPost`:

- unique index on `Slug`
- index on `Status`
- index on `PublishedAt`
- index on `DisplayOrder`
- index on `IsFeatured`

`BlogCategory`:

- unique index on `Slug`
- index on `IsActive`
- index on `DisplayOrder`

`BlogPostCategory`:

- unique index on (`BlogPostId`, `BlogCategoryId`)
- index on `BlogCategoryId`

---

## 8. Validation rules summary

### Blog post validation

- title required
- slug unique
- display order non-negative
- category ids must exist and not be deleted
- published posts require at least one category
- published posts require non-empty HTML content

### Blog category validation

- name required
- slug unique
- display order non-negative
- deletion blocked while in use

---

## 9. Test impact summary

Phase 2 implementation should include:

- command handler tests for create/update/delete/status/reorder post flows
- command handler tests for create/update/delete/reorder category flows
- query handler tests for public and admin list/detail behavior
- authorization coverage for `ADMIN` vs `STAFF`
- validation coverage for duplicate slugs, blocked category deletion, publish-ready checks, and visibility filtering
