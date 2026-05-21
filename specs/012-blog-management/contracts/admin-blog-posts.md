# Admin Blog Post Contracts

## Authorization

- `ADMIN`: full access
- `STAFF`: read and reorder only

## 1. List admin blog posts

### Request

`GET /api/v1/admin/blog-posts`

### Query parameters

- `page`
- `size`
- `takeAll`
- `status`
- `categoryId`
- `search`

### Behavior

- includes `Draft`, `Published`, and `Archived`
- excludes soft-deleted posts from standard listings
- default sort: `updatedAt desc`

## 2. Get admin blog post by ID

### Request

`GET /api/v1/admin/blog-posts/{id}`

### Behavior

- available to `ADMIN` and `STAFF`
- returns full detail model for internal management

## 3. Create blog post

### Request

`POST /api/v1/admin/blog-posts`

### Authorization

- `ADMIN`

### Request body

```json
{
  "title": "Brewing Better Mornings",
  "slug": "brewing-better-mornings",
  "excerpt": "A short summary",
  "contentHtml": "<p>Rich content</p>",
  "contentJson": "{\"type\":\"doc\"}",
  "coverImageUrl": "https://cdn.example.com/blogs/cover.jpg",
  "coverImageFileName": "blogs/uuid/cover.jpg",
  "categoryIds": ["guid-1", "guid-2"],
  "seoTitle": "SEO title",
  "seoDescription": "SEO description",
  "isFeatured": true,
  "displayOrder": 1,
  "status": "Draft"
}
```

### Validation expectations

- slug unique
- category ids valid
- publish-ready rules enforced if `status = Published`

## 4. Update blog post

### Request

`PUT /api/v1/admin/blog-posts/{id}`

### Authorization

- `ADMIN`

### Behavior

- full update of editable fields
- same validation rules as create

## 5. Soft-delete blog post

### Request

`DELETE /api/v1/admin/blog-posts/{id}`

### Authorization

- `ADMIN`

### Behavior

- soft delete only
- returns `204 No Content`

## 6. Update blog post status

### Request

`PATCH /api/v1/admin/blog-posts/{id}/status`

### Authorization

- `ADMIN`

### Request body

```json
{
  "status": "Published"
}
```

### Behavior

- supports transitions among `Draft`, `Published`, and `Archived`
- leaving `Published` removes the post from public visibility immediately

## 7. Reorder blog posts

### Request

`PATCH /api/v1/admin/blog-posts/reorder`

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

- batch updates display order
- intended for curated internal ordering flows
