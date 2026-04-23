# Research: Redis-Backed Core Flows

## Decision 1: Use targeted Redis services instead of a generic query caching pipeline

**Decision**: Introduce explicit Redis abstractions for catalog caching, cart storage, and password reset tickets rather than adding a generic MediatR pipeline or repository-wide caching layer.

**Rationale**: The existing codebase has only a few flows with clear cacheability and invalidation rules. Product list invalidation depends on multiple product, variant, and image write handlers, while cart and reset flows are not query caching problems at all. A narrow abstraction keeps failure behavior, TTL policy, and invalidation semantics explicit.

**Alternatives considered**:
- Generic MediatR cache behavior: rejected because it hides invalidation complexity and would over-cache unrelated queries.
- Repository-level caching: rejected because repository methods are too low-level for view-specific cache keys and TTL rules.

## Decision 2: Represent paginated catalog cache keys through a tracked key set

**Decision**: Cache product list responses per filter combination and maintain a Redis set that tracks active list-cache keys for bulk invalidation when catalog writes succeed.

**Rationale**: The catalog has paginated and filtered list variants, so invalidation needs to remove all affected list responses safely. Tracking keys in a dedicated set avoids expensive wildcard key scans and keeps the invalidation contract deterministic.

**Alternatives considered**:
- Redis key scan by prefix: rejected because it is operationally noisy and less predictable under load.
- Very short TTL without invalidation: rejected because it increases the stale-data window after admin writes.

## Decision 3: Store each authenticated cart as a single JSON document

**Decision**: Store one cart document per authenticated user under a stable Redis key and rewrite that document on successful cart mutations.

**Rationale**: Cart size is small, the whole cart is returned frequently, and business rules operate on the complete cart view. A single document keeps serialization simple, supports TTL refresh per mutation, and reduces partial-update edge cases.

**Alternatives considered**:
- Redis hash per cart line: rejected because it complicates total calculation, TTL handling, and full-cart reads for limited benefit.
- SQL-backed cart tables: rejected because the feature explicitly targets short-lived, restart-safe session data rather than long-lived transactional records.

## Decision 4: Scope cart support to authenticated users only in this feature

**Decision**: Implement one active cart per authenticated user and defer guest carts and merge behavior.

**Rationale**: The spec already constrains first delivery to authenticated carts. This keeps the feature aligned with the current auth setup and avoids introducing anonymous session identity, merge conflict rules, and extra frontend coupling in the same rollout.

**Alternatives considered**:
- Guest and authenticated carts together: rejected because it meaningfully expands scope and testing surface.
- Stateless client-only cart: rejected because it does not survive device changes or support server-side validation.

## Decision 5: Keep catalog reads available without Redis, but require Redis for cart and reset-ticket flows

**Decision**: Product queries fall back to the primary database when Redis is unavailable. Cart and password reset ticket operations return controlled failures if Redis is unavailable because those features have no correct primary-store fallback in this phase.

**Rationale**: Catalog acceleration is an optimization over existing read paths, so graceful degradation is possible. Cart persistence and opaque reset tickets rely on Redis as their authoritative short-lived store, and silent fallback would either lose session continuity or reintroduce the old exposed-token behavior.

**Alternatives considered**:
- Fail the entire API if Redis is unavailable: rejected because it would make catalog reads less resilient than they are today.
- Transparently fall back to the old password-reset token flow: rejected because it creates two behaviors for one feature and complicates rollout/testing.

## Decision 6: Preserve public password-reset API compatibility during rollout

**Decision**: Keep the existing forgot/reset endpoints, introduce an opaque ticket in the reset URL, and allow the reset request body to continue carrying `email` temporarily even though the backend will resolve the account from the ticket.

**Rationale**: This minimizes frontend breakage while enabling the backend to switch to one-time Redis-backed reset sessions. It also allows staged cleanup after the opaque-ticket flow is verified in production-like environments.

**Alternatives considered**:
- Immediate contract break removing `email` from the reset body: rejected because it forces coordinated frontend/backend rollout with no functional benefit to end users.
- Keep exposing the Identity token directly: rejected because it does not meet the new security and lifecycle-control goals.
