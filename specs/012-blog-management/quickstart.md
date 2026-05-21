# Quickstart — Blog Management

**Feature**: 012-blog-management

This is the engineer-facing verification guide for the blog management feature after implementation lands. It assumes the backend is running locally and the feature's migration has already been applied.

## 1. Preconditions

- PostgreSQL-backed backend is running locally
- latest database migration has been applied
- at least one `ADMIN` user and one `STAFF` user exist
- the shared file upload service is configured

## 2. Build and test first

```bash
rtk dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental
rtk dotnet test source/MoriiCoffee.Application.Tests/MoriiCoffee.Application.Tests.csproj
```

Expected result:

- build succeeds with no errors
- blog command/query/validator tests pass

## 3. Upload a cover image

Use the shared upload endpoint with the blog public container.

Endpoint:

- `POST /api/v1/files/upload`

Example request shape:

- `multipart/form-data`
- `file=<image>`
- `bucketName=blogs`

Expected result:

- response returns a public URL and object name
- returned URL can be stored on the blog post
- returned `blob.name` should also be stored as `coverImageFileName`

## 4. Create a draft blog post as admin

Admin flow to verify:

1. sign in as `ADMIN`
2. create a draft post with:
   - title
   - optional slug
   - excerpt
   - content JSON
   - content HTML
   - cover image URL
   - categories
   - SEO fields
   - featured flag
   - display order
3. verify the response returns the saved post

Expected result:

- post is visible in admin list
- post is not visible in public list while still in `Draft`

## 5. Publish and verify storefront visibility

Admin flow:

1. publish the draft post
2. request the public blog list
3. request the public blog detail by slug

Expected result:

- post appears in public list
- slug resolves successfully
- `publishedAt` is populated

## 6. Verify unpublish and archive behavior

Admin flow:

1. move a published post back to `Draft`
2. check public list/detail again
3. move the same post to `Archived`
4. re-check public list/detail

Expected result:

- post disappears from all public surfaces immediately after leaving `Published`
- post remains available in admin

## 7. Verify featured and ordering behavior

Admin or staff flow:

1. mark selected posts as featured
2. reorder posts using display order
3. request the featured-posts endpoint

Expected result:

- only featured published posts are returned
- results respect display order first

## 8. Verify category management

Admin flow:

1. create a category
2. attach it to one or more posts
3. attempt to delete it while still in use
4. remove the category from all linked posts
5. delete it again

Expected result:

- first delete attempt is rejected with a clear validation message
- second delete attempt succeeds after all links are removed

## 9. Verify role boundaries

### As `STAFF`

Verify staff can:

- view admin blog post list/detail
- view admin blog category list
- reorder posts
- reorder categories

Verify staff cannot:

- create posts
- edit post content
- change post status
- delete posts
- create/update/delete categories

### As `ADMIN`

Verify admin can perform all of the above management operations successfully.

## 10. Verify soft-delete behavior

Admin flow:

1. soft-delete a post
2. request public list/detail
3. request normal admin list

Expected result:

- deleted post no longer appears publicly
- deleted post does not appear in normal admin list unless a dedicated recovery/admin query is later added

## 11. Suggested manual API verification set

Minimum endpoint set to exercise after implementation:

- `GET /api/v1/blog-posts?page=1&size=10`
- `GET /api/v1/blog-posts/{slug}`
- `GET /api/v1/blog-posts/featured?take=3`
- `GET /api/v1/blog-categories?activeOnly=true`
- `GET /api/v1/admin/blog-posts?page=1&size=10`
- `GET /api/v1/admin/blog-posts/{id}`
- `POST /api/v1/admin/blog-posts`
- `PUT /api/v1/admin/blog-posts/{id}`
- `PATCH /api/v1/admin/blog-posts/{id}/status`
- `PATCH /api/v1/admin/blog-posts/reorder`
- `DELETE /api/v1/admin/blog-posts/{id}`
- `GET /api/v1/admin/blog-categories?page=1&size=10`
- `POST /api/v1/admin/blog-categories`
- `PUT /api/v1/admin/blog-categories/{id}`
- `PATCH /api/v1/admin/blog-categories/reorder`
- `DELETE /api/v1/admin/blog-categories/{id}`

## 12. Current automated verification status

Automated checks completed in implementation:

- presentation project builds successfully
- full `MoriiCoffee.Application.Tests` suite passes
- blog-focused command/query/mapper/authorization tests are included

Manual verification still recommended for:

- auth-protected endpoints with real `ADMIN` and `STAFF` tokens
- file upload round-trip for cover images
- end-to-end storefront rendering of `contentHtml`
