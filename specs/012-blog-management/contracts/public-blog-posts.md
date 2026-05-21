# Public Blog Contracts

## 1. List published blog posts

### Request

`GET /api/v1/blog-posts`

### Query parameters

- `page`
- `size`
- `takeAll`
- `categorySlug`
- `search`
- `sort`

### Behavior

- returns only non-deleted posts with `status = Published`
- category filter matches linked blog category slug
- search applies to title and excerpt in MVP

### Response shape

```json
{
  "statusCode": 200,
  "message": "Success",
  "data": {
    "items": [
      {
        "id": "guid",
        "title": "string",
        "slug": "string",
        "excerpt": "string or null",
        "coverImageUrl": "string or null",
        "status": "Published",
        "isFeatured": true,
        "displayOrder": 1,
        "publishedAt": "2026-05-21T12:00:00Z",
        "createdAt": "2026-05-21T10:00:00Z",
        "updatedAt": "2026-05-21T11:00:00Z",
        "categories": []
      }
    ],
    "metadata": {
      "currentPage": 1,
      "totalPages": 1,
      "takeAll": false,
      "pageSize": 10,
      "totalCount": 1,
      "payloadSize": 1,
      "hasPrevious": false,
      "hasNext": false
    }
  }
}
```

## 2. Get published blog post by slug

### Request

`GET /api/v1/blog-posts/{slug}`

### Behavior

- returns one published blog post
- returns `404` when slug is missing or not publicly visible

### Response shape

```json
{
  "statusCode": 200,
  "message": "Success",
  "data": {
    "id": "guid",
    "title": "string",
    "slug": "string",
    "excerpt": "string or null",
    "coverImageUrl": "string or null",
    "status": "Published",
    "isFeatured": false,
    "displayOrder": 10,
    "publishedAt": "2026-05-21T12:00:00Z",
    "createdAt": "2026-05-21T10:00:00Z",
    "updatedAt": "2026-05-21T11:00:00Z",
    "categories": [],
    "contentHtml": "<p>...</p>",
    "contentJson": "{...}",
    "seoTitle": "string or null",
    "seoDescription": "string or null"
  }
}
```

## 3. Get featured published posts

### Request

`GET /api/v1/blog-posts/featured?take=3`

### Behavior

- returns featured posts only
- returns only `Published` posts
- ordered by `displayOrder asc`, then `publishedAt desc`

## 4. Get public blog categories

### Request

`GET /api/v1/blog-categories?activeOnly=true`

### Behavior

- returns non-deleted categories only
- public callers normally use active categories only

### Response item shape

```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string or null",
  "displayOrder": 1,
  "isActive": true,
  "createdAt": "2026-05-21T10:00:00Z",
  "updatedAt": "2026-05-21T11:00:00Z"
}
```
