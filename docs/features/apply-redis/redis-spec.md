# Redis Adoption Spec for Morii Coffee

## Objective

This document defines how Redis should be adopted in Morii Coffee for 3 practical use cases, based on the current codebase:

1. Menu and pricing cache for product read flows
2. Server-side shopping cart storage
3. Password reset email flow using short-lived, one-time Redis tickets

Redis is not a replacement for SQL Server in this plan. SQL Server remains the source of truth for long-lived business data. Redis is used only for high-read or short-lived data.

---

## Current Codebase State

### 1. Product / menu

- The API already exposes `GET /api/v1/products` in `source/MoriiCoffee.Presentation/Controllers/ProductsController.cs`
- The current query handler is `GetPaginatedProductsQueryHandler`
- The handler currently loads data from EF Core, includes categories, then maps in memory
- Full write paths already exist for create / update / delete on products and variants

This makes the catalog the best first candidate for read-through caching with explicit invalidation on writes.

### 2. Cart

- Cart is not implemented yet in `source/`
- The repo already contains cart/order specs and design notes:
  - `specs/009-cart-payment/spec.md`
  - `docs/cart-order-break-down.md`

Because of that, cart is a good feature to design on Redis from the start instead of first building SQL persistence and optimizing later.

### 3. Password reset

- The current flow is:
  - `POST /api/v1/auth/forgot-password`
  - `POST /api/v1/auth/reset-password`
- `ForgotPasswordCommandHandler` currently uses `UserManager.GeneratePasswordResetTokenAsync`
- `ResetPasswordCommandHandler` currently uses `UserManager.ResetPasswordAsync`
- Reset emails are sent through `BrevoEmailService`

The current flow is already safe and workable. Redis for this use case should be treated as a deliberate redesign to improve token lifecycle control, one-time usage, and reduce direct client exposure to encoded Identity tokens.

---

## Redis Scope

### In scope

- Redis connection, settings, and health check
- Menu cache for product listing and product detail
- Redis-backed cart for authenticated users
- Redis-backed password reset tickets for email reset flow
- Basic logging, metrics, and cache invalidation

### Out of scope

- Guest cart merge
- Payment webhook idempotency
- Refresh token storage
- Global rate limiting
- Distributed locks / inventory reservation
- Caching every query in the system

---

## Proposed Architecture

### Redis integration

- Packages:
  - `StackExchange.Redis`
  - `Microsoft.Extensions.Caching.StackExchangeRedis`
- New configuration in `appsettings*.json`:
  - `Redis:ConnectionString`
  - `Redis:InstanceName`
- DI registrations:
  - `IConnectionMultiplexer`
  - `IDatabase`
  - dedicated abstractions for cache, cart, and password reset

### General principles

- Do not cache every query through a generic pipeline in the first phase
- Only cache use cases that have a clear invalidation strategy
- On cache miss, read from the database and populate Redis
- After successful write commands, remove the related cache entries
- Redis failures should not take down read APIs when a safe DB fallback exists

---

## Feature 1: Menu and Price Cache

### Goal

Reduce latency and database load for the catalog APIs that are expected to be read heavily:

- `GET /api/v1/products`
- `GET /api/v1/products/{id}`
- Future expansion candidates:
  - `GET /api/v1/products/{productId}/variants`
  - `GET /api/v1/categories`
  - `GET /api/v1/banners`

### Cache design

#### 1. Product list cache

- Cache payload: `Pagination<ProductSummaryDto>`
- Proposed key:
  - `catalog:products:page:{page}:size:{size}:category:{categoryId-or-none}:featured:{bool-or-none}`
- TTL:
  - 5 minutes

Reasoning:
- The catalog can change through admin operations, but not at extremely high frequency
- 5 minutes keeps the stale window small even if an invalidation path is missed

#### 2. Product detail cache

- Cache payload: `ProductDto`
- Proposed key:
  - `catalog:products:{productId}`
- TTL:
  - 10 minutes

#### 3. Variant list cache

- Cache payload: `List<ProductVariantDto>`
- Proposed key:
  - `catalog:products:{productId}:variants`
- TTL:
  - 10 minutes

### Invalidation strategy

Cache should be removed only after the DB commit succeeds in the following command handlers:

- `CreateProductCommandHandler`
- `UpdateProductCommandHandler`
- `DeleteProductCommandHandler`
- `CreateProductVariantCommandHandler`
- `UpdateProductVariantCommandHandler`
- `DeleteProductVariantCommandHandler`
- `UploadProductImagesCommandHandler`
- `DeleteProductImageCommandHandler`
- `ReorderProductImagesCommandHandler`

### Invalidation rules

- If a product changes:
  - remove `catalog:products:{id}`
  - remove prefix `catalog:products:page:`
  - remove `catalog:products:{id}:variants` if detail pricing depends on variant data
- If a variant changes:
  - remove `catalog:products:{productId}`
  - remove `catalog:products:{productId}:variants`
  - remove prefix `catalog:products:page:` because summary pricing or availability may change
- If images or thumbnail change:
  - remove `catalog:products:{productId}`
  - remove prefix `catalog:products:page:`

### Functional requirements

- `FR-MC-001`: The system MUST cache paginated product responses in Redis.
- `FR-MC-002`: The system MUST cache product detail responses in Redis.
- `FR-MC-003`: The system MUST invalidate impacted cache keys immediately after successful product or variant writes.
- `FR-MC-004`: The system MUST fall back to the database when Redis is unavailable.
- `FR-MC-005`: Cached product responses MUST never outlive their configured TTL.

### Success criteria

- `SC-MC-001`: P95 latency for `GET /api/v1/products` is meaningfully lower than the baseline local/dev benchmark.
- `SC-MC-002`: Product writes do not return stale detail data beyond one request cycle after successful invalidation.
- `SC-MC-003`: If Redis is down, catalog APIs still return data from the database instead of failing with 500 due to the cache layer.

### Design notes

- Do not cache raw EF entities
- Cache only final DTO responses
- Do not use a generic caching pipeline for every query in the first phase because invalidation for `GetPaginatedProductsQueryHandler` depends on many write paths

---

## Feature 2: Redis-backed Shopping Cart

### Goal

Implement the cart on Redis, aligned with the existing design documents and without introducing SQL schema for short-lived cart state.

### Phase scope

- Authenticated user cart only
- One cart per user
- Guest cart and post-login merge are deferred

Reasoning:
- The repo already has JWT auth and stable user identity handling
- Auth-only cart reduces first-phase complexity significantly
- Guest cart can be added later after the core flow is stable

### Cart data

#### Redis key

- `cart:user:{userId}`

#### TTL

- 7 days
- reset TTL on every add / update / remove / clear action

#### Payload

The cart should snapshot enough information to render quickly and consistently:

- `userId`
- `items[]`
  - `productId`
  - `variantId`
  - `productName`
  - `variantName`
  - `thumbnailUrl`
  - `unitPrice`
  - `quantity`
  - `lineTotal`
- `grandTotal`
- `updatedAt`

### Business rules

- Adding the same variant increases quantity instead of creating a duplicate line
- `unitPrice` is snapshotted at add-to-cart time
- When cart is read:
  - availability warning data may be enriched
  - snapshotted price should not auto-change
- At checkout:
  - availability and current price must be revalidated
  - if the price changed, checkout should fail and ask the client to let the user review the cart again

### Target API

- `GET /api/v1/cart`
- `POST /api/v1/cart/items`
- `PUT /api/v1/cart/items/{variantId}`
- `DELETE /api/v1/cart/items/{variantId}`
- `DELETE /api/v1/cart`

### Functional requirements

- `FR-CART-001`: The system MUST store authenticated user carts in Redis, one cart per user.
- `FR-CART-002`: The system MUST reset cart TTL on every successful cart mutation.
- `FR-CART-003`: The system MUST snapshot product and variant pricing at add-to-cart time.
- `FR-CART-004`: The system MUST merge duplicate variants by increasing quantity.
- `FR-CART-005`: The system MUST allow removing one line item or clearing the full cart.
- `FR-CART-006`: The system MUST return an empty cart when no Redis key exists.
- `FR-CART-007`: The system MUST delete the Redis cart after successful checkout.

### Success criteria

- `SC-CART-001`: An authenticated user can add, update, remove, and clear a cart without SQL persistence.
- `SC-CART-002`: The cart survives API restarts as long as the Redis key has not expired.
- `SC-CART-003`: A successful checkout deletes the cart key immediately after the order commit succeeds.

### Design notes

- In this codebase, cart should start with a dedicated service instead of a heavily abstracted generic repository pattern
- Redis String(JSON) is sufficient for the first phase and faster to iterate on
- Redis Hash/List structures are not necessary until partial mutation complexity actually justifies them

---

## Feature 3: Password Reset Email via Redis Ticket

### Goal

Move the password reset flow to an opaque Redis ticket model:

- short-lived token
- one-time use
- no direct Identity reset token exposed to the frontend

### Comparison with the current flow

#### Current flow

- Generate ASP.NET Identity token
- Encode token and email into the reset URL
- Client sends back `email + token + newPassword`

#### Proposed flow

- Generate an opaque `resetTicket`
- Store a Redis record containing reset metadata
- Email only carries the `resetTicket`
- Client submits `resetTicket + newPassword`

### Redis key and TTL

- Key:
  - `auth:password-reset:{ticket}`
- TTL:
  - 15 minutes

### Proposed payload

- `userId`
- `email`
- `identityResetToken` or `identityResetTokenHash`
- `requestedAt`
- `consumedAt` nullable

### Safe implementation approach

#### Recommended option for this repo

Keep `UserManager.ResetPasswordAsync` as the final password reset mechanism, but stop exposing the raw Identity token to the client.

Flow:

1. `ForgotPassword`
   - find user by email
   - generate internal Identity reset token
   - generate random `resetTicket`
   - store Redis entry: `resetTicket -> userId + identityToken`
   - send email containing `resetTicket`

2. `ResetPassword`
   - receive `resetTicket + newPassword`
   - read Redis record
   - call `UserManager.ResetPasswordAsync(user, identityToken, newPassword)`
   - if successful, delete the Redis key immediately

This keeps the strongest parts of the current design:

- ASP.NET Identity still performs the actual password reset
- the client no longer holds the raw Identity token
- TTL and one-time usage are clearly enforced in Redis

### Proposed endpoint shape

#### New phase

- `POST /api/v1/auth/forgot-password`
  - request stays the same: `email`
- `POST /api/v1/auth/reset-password`
  - new request:
    - `token`
    - `newPassword`

### Breaking change

`ResetPasswordDto` currently requires:

- `Email`
- `Token`
- `NewPassword`

If the system fully switches to Redis ticket flow, `Email` is no longer required for reset confirmation. That is a breaking API change for the frontend and should be rolled out deliberately.

### Functional requirements

- `FR-PR-001`: The system MUST generate a cryptographically secure opaque reset ticket.
- `FR-PR-002`: The system MUST store password reset tickets in Redis with a 15-minute TTL.
- `FR-PR-003`: The system MUST not reveal whether the email exists in the system.
- `FR-PR-004`: The system MUST allow each reset ticket to be used only once.
- `FR-PR-005`: The system MUST delete the Redis ticket immediately after a successful password reset.
- `FR-PR-006`: The system MUST reject expired, missing, or already-consumed reset tickets.

### Success criteria

- `SC-PR-001`: The reset link expires automatically after TTL without any cleanup job.
- `SC-PR-002`: After a successful reset, the same ticket cannot be reused.
- `SC-PR-003`: The forgot-password API always returns the same response whether the email exists or not.

### Design notes

- Do not hash the password reset ticket on the client
- The ticket can be hashed before storing in Redis if the team wants to reduce exposure risk from logs or memory dumps
- If the team wants zero frontend breaking change initially, `Email` can remain in the reset request for a short compatibility window even if the backend no longer uses it

---

## Recommended Rollout Order

1. Product/menu cache
2. Redis cart
3. Redis password reset ticket

Reasoning:

- Product cache is low risk and shows value immediately
- Cart is the most natural Redis use case for the upcoming commerce flow
- Password reset is an auth-flow redesign, so it should come after Redis infrastructure is already stable

---

## Risks and Important Decisions

### 1. Redis outage

- Product cache should fall back to the database
- Cart and reset tickets are stateful Redis use cases
- If Redis is unavailable:
  - cart APIs should fail clearly rather than silently losing state
  - forgot/reset password should fail clearly rather than emailing a ticket that was never stored

### 2. Cache invalidation

- This is the main risk in product caching
- The first phase should keep invalidation explicit inside command handlers
- Avoid broad wildcard invalidation if Redis is shared across environments or apps

### 3. Breaking contract for reset password

- Frontend and backend need alignment before removing `Email` from `ResetPasswordDto`
- A short compatibility window is reasonable: accept both old and new payloads

### 4. Cart price consistency

- Snapshotted cart price does not mean checkout will always succeed
- Checkout must still revalidate current price and availability before order creation

---

## Final Recommendation

Morii Coffee should adopt Redis in 3 priority layers:

1. `Catalog cache`: speed up existing product/menu APIs
2. `Cart store`: use Redis as the source of truth for authenticated shopping carts
3. `Password reset ticket`: use Redis for TTL-based, one-time reset flow while still relying on ASP.NET Identity for the final password change

This approach fits the current codebase and is lower risk than trying to introduce Redis across the entire query layer from day one.
