# MoriiCoffee API — Development Roadmap

This document tracks the planned features for the MoriiCoffee API, organized by phase.
Phase 1 (core catalog CRUD) is complete. All subsequent phases are planned.

---

## Phase 1 — Core Catalog API ✅ Complete

**Goal:** Establish a production-quality repository structure with working CRUD APIs.

### Implemented
- [x] Clean Architecture with 6 layers: `Domain.Shared`, `Domain`, `Application`, `Infrastructure.Persistence`, `Infrastructure`, `Presentation`
- [x] Domain-Driven Design (DDD) — aggregate roots, entities, value objects
- [x] CQRS via MediatR — separate Commands and Queries
- [x] MediatR pipeline behaviors: Exception → Logging → Performance → Validation
- [x] FluentValidation per command
- [x] AutoMapper profiles
- [x] EF Core with SQL Server — soft delete, date tracking interceptor, query filters
- [x] Repository pattern + Unit of Work
- [x] Serilog structured logging
- [x] Swagger / OpenAPI documentation with JWT placeholder
- [x] Global exception-handling middleware
- [x] Database seeding with sample coffee menu data (Bogus)
- [x] Category CRUD (`/api/v1/categories`)
- [x] Product CRUD with pagination and filters (`/api/v1/products`)
- [x] ProductVariant CRUD with size/price management (`/api/v1/variants`)

---

## Phase 2 — Authentication & Authorization 🔐

**Goal:** Secure all write operations behind JWT authentication.

### Planned
- [ ] User aggregate (`Users` table: Id, Email, PasswordHash, Role, RefreshToken)
- [ ] `POST /api/v1/auth/register` — register a new account
- [ ] `POST /api/v1/auth/login` — issue JWT access token + refresh token
- [ ] `POST /api/v1/auth/refresh` — exchange refresh token for new access token
- [ ] `POST /api/v1/auth/logout` — revoke refresh token
- [ ] Role-based authorization: `Admin`, `Staff`, `Customer`
- [ ] Protect write endpoints with `[Authorize(Roles = "Admin")]`
- [ ] Add `CreatedBy` / `UpdatedBy` audit fields to entities
- [ ] Password hashing with BCrypt

### Dependencies
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `BCrypt.Net-Next`

---

## Phase 3 — Order Management 🛒

**Goal:** Allow customers to place and track orders.

### Planned
- [ ] Order aggregate (`Orders`, `OrderItems` tables)
- [ ] Order status enum: `Pending`, `Confirmed`, `Preparing`, `Ready`, `Completed`, `Cancelled`
- [ ] `POST /api/v1/orders` — create order from cart
- [ ] `GET /api/v1/orders/{id}` — get order by ID
- [ ] `GET /api/v1/orders` — list orders (with filters: status, date range)
- [ ] `PATCH /api/v1/orders/{id}/status` — update order status
- [ ] `DELETE /api/v1/orders/{id}` — cancel order
- [ ] Stock management — decrement `StockQuantity` on order, increment on cancel
- [ ] Order total calculation including discounts

---

## Phase 4 — Caching with Redis 🚀

**Goal:** Improve read performance for high-traffic catalog endpoints.

### Planned
- [ ] Redis integration via `StackExchange.Redis`
- [ ] Cache-aside pattern for category list and product detail queries
- [ ] Cache invalidation on write operations (commands)
- [ ] `ICacheService` abstraction in Domain layer
- [ ] Configurable TTL per cache key type
- [ ] Cache-warming on startup (seed hot data)

### Candidates for caching
- `GET /api/v1/categories` — category list (rarely changes)
- `GET /api/v1/products/{id}` — individual product with variants
- `GET /api/v1/products` (popular first page)

---

## Phase 5 — Background Jobs with Hangfire ⏰

**Goal:** Handle async and scheduled tasks reliably.

### Planned
- [ ] Hangfire integration with SQL Server persistence
- [ ] Hangfire dashboard (admin-only)
- [ ] Fire-and-forget jobs: send order confirmation email after order placed
- [ ] Recurring jobs: daily stock report, cleanup of soft-deleted records after 90 days
- [ ] Scheduled jobs: promotional pricing activation/deactivation

### Dependencies
- `Hangfire.Core`
- `Hangfire.SqlServer`
- `Hangfire.AspNetCore`

---

## Phase 6 — Real-Time Updates with SignalR 📡

**Goal:** Push live order status updates to clients without polling.

### Planned
- [ ] `OrderStatusHub` — notify clients when order status changes
- [ ] `KitchenHub` — notify kitchen staff when new orders arrive
- [ ] Group-based messaging: each customer sees only their orders
- [ ] Integrate SignalR events into Order status update command handler

### Dependencies
- `Microsoft.AspNetCore.SignalR`

---

## Phase 7 — Promotions & Discounts 🎟️

**Goal:** Support coupon codes and product-level promotions.

### Planned
- [ ] Coupon aggregate (`Coupons` table: Code, DiscountType, DiscountValue, MinOrderAmount, ExpiresAt, MaxUses)
- [ ] `POST /api/v1/coupons` — create coupon
- [ ] `POST /api/v1/orders/apply-coupon` — validate and apply coupon to cart
- [ ] Product promotion fields: `OriginalPrice`, `PromotionPrice`, `PromotionEndsAt`
- [ ] Automatic promotion expiry via Hangfire

---

## Phase 8 — Media & File Upload 📷

**Goal:** Allow product image management.

### Planned
- [ ] `ProductImages` CRUD API — add/remove/reorder product images
- [ ] Image upload endpoint with validation (type, size)
- [ ] Cloud storage integration (Azure Blob Storage or Cloudinary)
- [ ] Image CDN URL generation
- [ ] Thumbnail generation on upload

---

## Phase 9 — Analytics & Reporting 📊

**Goal:** Provide business insights for admins.

### Planned
- [ ] `GET /api/v1/admin/reports/sales` — daily/weekly/monthly revenue
- [ ] `GET /api/v1/admin/reports/top-products` — best-selling products
- [ ] `GET /api/v1/admin/reports/low-stock` — variants with low stock
- [ ] Materialized views or read-optimized queries

---

## Technical Debt & Improvements

- [ ] Add integration tests (xUnit + Testcontainers for SQL Server)
- [ ] Add unit tests for Application layer command handlers
- [ ] Add FluentValidation validators for query filter parameters
- [ ] API versioning strategy (`api/v1`, `api/v2`)
- [ ] Rate limiting (`Microsoft.AspNetCore.RateLimiting`)
- [ ] Health checks (`/healthz`) for monitoring
- [ ] Docker + Docker Compose setup for local development
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] OpenAPI spec export for frontend code generation (Orval / openapi-generator)
