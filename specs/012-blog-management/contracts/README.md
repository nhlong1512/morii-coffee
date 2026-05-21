# Blog Management Contracts

This directory captures the backend-facing API contracts that the Morii storefront and admin frontend can integrate against once implementation begins.

Contracts included:

- `public-blog-posts.md`
- `admin-blog-posts.md`
- `admin-blog-categories.md`

Shared conventions:

- All responses use the existing `ApiResponse` envelope:
  - `statusCode`
  - `message`
  - `data`
- Paginated endpoints use the existing `Pagination<T>` shape:
  - `items`
  - `metadata`
- Public endpoints return published content only.
- Admin endpoints follow role rules from the feature spec:
  - `ADMIN`: full content/category management
  - `STAFF`: read + reorder only

Upload note:

- Cover image upload reuses the shared file upload capability and is not defined as a blog-specific contract in this folder.
- Implementation should add a dedicated public file container/folder for blog assets.
