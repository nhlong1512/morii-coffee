# Quickstart: Redis-Backed Core Flows

## Goal

Validate the Redis-backed catalog cache, authenticated cart, and password reset ticket flow in a local development environment before implementation is considered complete.

## Prerequisites

- .NET 10 SDK installed
- Docker available for local service dependencies
- Valid development configuration for SQL Server, email sending, and storage
- Redis configuration added to `source/MoriiCoffee.Presentation/appsettings.json` and any local overrides

## Start dependencies

1. Start the local development services, including Redis, from the repository root.
2. Confirm the API can connect to SQL Server and Redis at startup.
3. Launch the API in Development mode.

## Smoke test 1: Catalog caching

1. Call `GET /api/v1/products` twice with the same query parameters.
2. Confirm both responses are successful and equivalent.
3. Confirm logs show an initial cache miss followed by a cache hit.
4. Update a product or product variant through an existing write endpoint.
5. Call the same product list and product detail endpoints again.
6. Confirm the updated data is returned and the stale cache was invalidated.
7. Temporarily stop Redis and repeat the product list request.
8. Confirm the API still returns data from the primary database path.

## Smoke test 2: Authenticated cart

1. Authenticate as a customer and capture the bearer token.
2. Call `GET /api/v1/cart` and confirm an empty cart is returned for a new user.
3. Add an available product variant through `POST /api/v1/cart/items`.
4. Add the same variant again and confirm quantity increases rather than duplicating the line.
5. Update the item quantity through `PUT /api/v1/cart/items/{variantId}`.
6. Restart the API while leaving Redis running.
7. Call `GET /api/v1/cart` again and confirm the cart contents are preserved.
8. Clear the cart through `DELETE /api/v1/cart` and confirm the cart is empty afterward.

## Smoke test 3: Password reset tickets

1. Request password reset through `POST /api/v1/auth/forgot-password` for an existing account.
2. Confirm the public response remains privacy-safe.
3. Inspect the reset email or generated link and verify it contains an opaque ticket rather than an exposed reset token.
4. Submit `POST /api/v1/auth/reset-password` with the ticket and a valid new password.
5. Confirm the password resets successfully.
6. Repeat the same reset request with the same ticket.
7. Confirm the second attempt fails because the ticket is already consumed.

## Test suite focus

- Application tests for catalog cache hit/miss/fallback behavior
- Application tests for cart add, merge, update, remove, and TTL refresh behavior
- Application tests for forgot/reset password handlers using Redis-backed tickets
- Any new infrastructure tests or contract validations added for Redis serialization and endpoint shape
