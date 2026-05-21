# Admin Blog Category Contracts

## Authorization

- `ADMIN`: full access
- `STAFF`: read and reorder only

## 1. List admin blog categories

### Request

`GET /api/v1/admin/blog-categories`

### Query parameters

- `page`
- `size`
- `takeAll`
- `search`

### Behavior

- returns non-deleted categories
- intended for internal category management

## 2. Create blog category

### Request

`POST /api/v1/admin/blog-categories`

### Authorization

- `ADMIN`

### Request body

```json
{
  "name": "Coffee Guides",
  "slug": "coffee-guides",
  "description": "How-to and educational content",
  "displayOrder": 1,
  "isActive": true
}
```

### Validation expectations

- slug unique
- display order non-negative

## 3. Update blog category

### Request

`PUT /api/v1/admin/blog-categories/{id}`

### Authorization

- `ADMIN`

## 4. Soft-delete blog category

### Request

`DELETE /api/v1/admin/blog-categories/{id}`

### Authorization

- `ADMIN`

### Behavior

- soft delete only
- reject delete when any non-deleted blog post is still linked

### Expected validation error shape

```json
{
  "statusCode": 400,
  "message": "Bad Request",
  "errors": [
    "Cannot delete category because it is still assigned to one or more blog posts."
  ]
}
```

## 5. Reorder blog categories

### Request

`PATCH /api/v1/admin/blog-categories/reorder`

### Authorization

- `ADMIN`, `STAFF`

### Request body

```json
{
  "items": [
    { "id": "guid-1", "displayOrder": 1 },
    { "id": "guid-2", "displayOrder": 2 }
  ]
}
```

### Behavior

- updates category ordering for admin and public navigation consumers
