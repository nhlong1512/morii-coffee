# Redis Adoption Plan for Morii Coffee

## Delivery Goal

This plan breaks Redis adoption into small steps so the system does not change too many layers at once. The rollout is ordered by business value:

1. Catalog cache
2. Cart
3. Password reset ticket

---

## Phase 0: Redis Foundation

### Goal

Add the shared Redis infrastructure that later phases can build on.

### Tasks

1. Add packages to the correct project:
   - `source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj`
   - `StackExchange.Redis`
   - `Microsoft.Extensions.Caching.StackExchangeRedis`

2. Add configuration:
   - `source/MoriiCoffee.Presentation/appsettings.json`
   - `Redis:ConnectionString`
   - `Redis:InstanceName`

3. Add a strongly typed settings model if preferred:
   - `source/MoriiCoffee.Domain.Shared/Settings/RedisSettings.cs`

4. Register services in infrastructure:
   - `IConnectionMultiplexer`
   - `IDatabase`
   - optional `IDistributedCache`

5. Update development Docker setup:
   - add a `redis` service to `deploy/docker-compose.development.yml`
   - update API `depends_on` if needed

6. Add minimal health check or startup validation

### Deliverables

- The app can boot with a local Redis instance
- Redis settings can be overridden by environment
- Connection failures are logged clearly

### Risks

- There is no Redis service in the current development compose file
- If Redis is connected too aggressively during startup, local development may fail to boot when Redis is not running

### Recommendation

- Use lazy connection or reasonable retry behavior
- Do not fail the entire app just because catalog caching is not available yet

---

## Phase 1: Catalog Cache

### Goal

Cache the product read path first, because it is the easiest use case to validate and the lowest risk.

### Scope

- `GetPaginatedProductsQueryHandler`
- `GetProductByIdQueryHandler`
- optional stretch:
  - `GetVariantsByProductIdQueryHandler`

### Tasks

1. Create a small abstraction instead of over-generalizing early:
   - `IProductCatalogCache`
   - `ProductCatalogCache`

2. Cache DTO responses, not entities

3. Update query handlers to:
   - try cache first
   - hit the database on miss
   - write to cache with TTL

4. Add invalidation to command handlers:
   - create/update/delete product
   - create/update/delete variant
   - upload/delete/reorder image

5. Add metrics and logs:
   - cache hit
   - cache miss
   - cache invalidate

### Expected files to touch

- `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`
- `source/MoriiCoffee.Presentation/appsettings.json`
- `source/MoriiCoffee.Application/Queries/Product/GetPaginatedProducts/GetPaginatedProductsQueryHandler.cs`
- `source/MoriiCoffee.Application/Queries/Product/GetProductById/GetProductByIdQueryHandler.cs`
- related product / variant / image command handlers
- new files under `source/MoriiCoffee.Infrastructure/Services/Redis/`

### Acceptance checklist

- `GET /api/v1/products` returns correct data on both hit and miss
- `GET /api/v1/products/{id}` returns correct data on both hit and miss
- After product updates, detail and list cache are no longer stale
- If Redis is down, APIs still read from the database

---

## Phase 2: Cart on Redis

### Goal

Use Redis as the primary storage for authenticated user carts.

### Scope

- Authenticated carts only
- No guest cart yet
- No cart merge after login yet

### Tasks

1. Finalize cart DTOs / models:
   - `CartDto`
   - `CartItemDto`
   - request DTOs for add/update

2. Create services:
   - `ICartService`
   - `RedisCartService`

3. Create a new controller:
   - `CartController`

4. Add item logic:
   - validate that the variant exists
   - validate `IsAvailable`
   - snapshot `productName`, `variantName`, `thumbnailUrl`, `unitPrice`
   - merge quantity when the line already exists

5. Get cart logic:
   - return empty cart when no key exists
   - optionally enrich availability warnings

6. Update/remove/clear logic:
   - update JSON cart in Redis
   - reset TTL

7. Prepare hooks for later checkout work:
   - `GetCartForCheckoutAsync`
   - `ClearCartAsync` after successful order commit

8. Add tests:
   - unit test for quantity merge behavior
   - unit test for TTL reset behavior at the service level
   - controller tests if the team wants coverage there

### Expected new files

- `source/MoriiCoffee.Presentation/Controllers/CartController.cs`
- `source/MoriiCoffee.Application/SeedWork/DTOs/Cart/...`
- `source/MoriiCoffee.Application/SeedWork/Abstractions/ICartService.cs`
- `source/MoriiCoffee.Infrastructure/Services/Redis/RedisCartService.cs`

### Business dependencies

- Product and ProductVariant read models must expose enough data to snapshot price
- The future checkout feature should reuse the cart service

### Acceptance checklist

- User can add a variant to cart
- Adding the same variant again increases quantity
- Updating quantity to 0 removes the item
- Clearing the cart deletes the Redis key
- Cart survives API restart as long as the Redis key still exists

---

## Phase 3: Password Reset Ticket

### Goal

Move password reset to an opaque Redis ticket model while still using ASP.NET Identity for the final password change.

### Tasks

1. Create abstractions:
   - `IPasswordResetTicketStore`
   - `RedisPasswordResetTicketStore`

2. Update `ForgotPasswordCommandHandler`:
   - generate internal Identity token
   - generate `resetTicket`
   - store the Redis ticket
   - send email containing the ticket

3. Update `ResetPasswordCommandHandler`:
   - read the ticket from Redis
   - resolve the user
   - call `UserManager.ResetPasswordAsync`
   - delete the key on success

4. Update DTO / API contract:
   - preferred: remove `Email` from `ResetPasswordDto`
   - compatibility mode: temporarily accept `Email` but ignore it in the backend

5. Update email template or reset URL builder if needed

6. Add minimal abuse protection:
   - per-email cooldown or lightweight rate limiting in Redis
   - optional, but strongly recommended in the same phase

### Expected files to touch

- `source/MoriiCoffee.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs`
- `source/MoriiCoffee.Application/Commands/Auth/ResetPassword/ResetPasswordCommandHandler.cs`
- `source/MoriiCoffee.Application/SeedWork/DTOs/Auth/ResetPasswordDto.cs`
- `source/MoriiCoffee.Infrastructure/Services/Email/BrevoEmailService.cs` if reset URL shape changes
- new files under `source/MoriiCoffee.Infrastructure/Services/Redis/`

### Acceptance checklist

- Forgot-password still always returns 200
- Reset email contains an opaque ticket
- Ticket expires after 15 minutes
- Successful reset makes the ticket unusable afterward
- Invalid or expired ticket returns a clear error

---

## Testing Plan

### Unit tests

- Catalog cache hit/miss behavior
- Invalidation service for product keys
- Cart merge/update/remove logic
- Password reset ticket consume-once logic

### Integration tests

- Product query reads from cache after the first request
- Product update invalidates cache
- Forgot/reset password works end to end with real Redis or a test container

### Manual smoke tests

1. Run app + local Redis
2. Call `GET /api/v1/products` twice and confirm the second call hits cache
3. Update a product, then fetch list/detail again and confirm new data
4. Add an item to cart, restart the API, then fetch cart again
5. Request password reset, use the reset link, then try reusing the same link

---

## Recommended PR Order

1. PR 1: Redis infrastructure, config, and Docker compose
2. PR 2: Product/catalog cache
3. PR 3: Redis cart APIs
4. PR 4: Redis password reset ticket

Reasoning:

- each PR stays small
- rollback is simpler
- frontend cart work and auth reset work are not blocked by catalog cache delivery

---

## Decisions to Confirm Before Coding

1. Should first-phase cart support guests or remain auth-only
2. Should reset-password accept a breaking change removing `Email`, or should compatibility mode be kept temporarily
3. What should Redis failure behavior be for cart/reset:
   - fail fast
   - or partial fallback

### Practical recommendation

- Cart: auth-only in phase one
- Reset password: short compatibility window
- Redis failure:
  - catalog falls back to database
  - cart/reset fail clearly

---

## Definition of Done

- Redis is configured and works in local development
- Catalog APIs have cache plus invalidation
- Cart APIs work on Redis for authenticated users
- Password reset flow uses one-time, TTL-based Redis tickets
- Tests cover the key risk paths
- Documentation is updated so frontend and backend can align on the rollout

---

## Implementation Notes (2026-04-24)

### What was implemented

All three user stories were delivered in a single branch `010-apply-redis`.

#### Packages added
- `StackExchange.Redis 2.8.24`
- `Microsoft.Extensions.Caching.StackExchangeRedis 10.0.5`

#### New infrastructure files
| File | Purpose |
|------|---------|
| `Services/Redis/ProductCatalogCache.cs` | US1 – read-through list+detail cache |
| `Services/Redis/ProductCatalogCacheModels.cs` | US1 – Redis key constants and cache entry records |
| `Services/Redis/RedisCartService.cs` | US2 – per-user cart document |
| `Services/Redis/RedisPasswordResetTicketStore.cs` | US3 – opaque one-time reset tickets |

#### New application files
| File | Purpose |
|------|---------|
| `SeedWork/Abstractions/IProductCatalogCache.cs` | US1 cache abstraction |
| `SeedWork/Abstractions/ICartService.cs` | US2 cart abstraction |
| `SeedWork/Abstractions/IPasswordResetTicketStore.cs` | US3 ticket store abstraction |
| `SeedWork/DTOs/Cart/CartDto.cs`, `CartItemDto.cs` | US2 cart response DTOs |
| `SeedWork/DTOs/Cart/AddCartItemDto.cs`, `UpdateCartItemDto.cs` | US2 request DTOs |
| `SeedWork/DTOs/Auth/PasswordResetTicketDto.cs` | US3 ticket payload |
| `SeedWork/Helpers/CatalogCacheKeyHelper.cs` | US1 deterministic key builder |
| `SeedWork/Exceptions/ServiceUnavailableException.cs` | 503 exception for Redis failures |
| `Commands/Cart/*` + `Queries/Cart/*` | US2 MediatR slices |

#### Modified files
- `Queries/Product/GetPaginatedProductsQueryHandler.cs` – read-through cache
- `Queries/Product/GetProductByIdQueryHandler.cs` – read-through cache
- `Commands/Product/{Create,Update,Delete}ProductCommandHandler.cs` – cache invalidation
- `Commands/ProductVariant/{Create,Update,Delete}ProductVariantCommandHandler.cs` – cache invalidation
- `Commands/Product/{UploadProductImages,DeleteProductImage,ReorderProductImages}CommandHandler.cs` – cache invalidation
- `Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs` – issues opaque ticket
- `Commands/Auth/ResetPassword/ResetPasswordCommandHandler.cs` – consumes opaque ticket
- `SeedWork/DTOs/Auth/ResetPasswordDto.cs` – `Token` → `Ticket` (Email optional compat field)
- `Presentation/Controllers/AuthController.cs` – maps `Ticket` field
- `Presentation/Controllers/CartController.cs` – new authenticated cart endpoints
- `Presentation/Middlewares/ErrorWrappingMiddleware.cs` – handles 503
- `Presentation/Program.cs` – Redis startup connectivity validation
- `Infrastructure/DependencyInjection.cs` – Redis multiplexer + service registrations

### Local verification

```bash
# Start services (Docker Compose starts Redis on port 6379)
cd deploy && bash run-docker-development.sh

# Verify API connects to Redis on startup — look for:
# [INF] Redis connectivity check passed.

# Smoke test catalog cache
curl "http://localhost:8002/api/v1/products" | jq .
# Second call should log a cache HIT in the API logs

# Smoke test cart
TOKEN=$(curl -s -X POST http://localhost:8002/api/v1/auth/signin -H 'Content-Type: application/json' \
  -d '{"identity":"test@example.com","password":"Test1234!"}' | jq -r '.data.accessToken')
curl -H "Authorization: Bearer $TOKEN" http://localhost:8002/api/v1/cart | jq .

# Smoke test password reset
curl -X POST http://localhost:8002/api/v1/auth/forgot-password \
  -H 'Content-Type: application/json' -d '{"email":"test@example.com"}'
# Check email for link containing ?ticket= (not ?token=)
```
