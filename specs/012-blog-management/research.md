# Phase 0 — Research & Decisions

**Feature**: 012-blog-management  
**Date**: 2026-05-21

This document resolves the implementation-shaping decisions behind the blog management feature and feeds Phase 1 design.

---

## R-001: Use dedicated blog aggregates instead of reusing existing catalog categories

**Decision**: Create a dedicated `BlogPost` aggregate and a dedicated `BlogCategory` aggregate. Do not reuse the existing product `Category` aggregate.

**Rationale**:

- Product categories and blog categories serve different business purposes.
- Blog categories need their own deletion, visibility, and relationship rules.
- Reusing product categories would create naming collisions, mixed admin semantics, and higher regression risk.
- The repo already uses additive bounded contexts effectively, so a dedicated module is the lowest-risk option.

**Alternatives considered**:

- Reuse product categories: rejected because it would blur product/catalog concerns with editorial concerns.
- Store categories as plain strings on posts: rejected because it would weaken validation and admin management.

---

## R-002: Keep public and admin API surfaces separate

**Decision**: Expose separate public and admin controllers/endpoints for blog posts and blog categories.

**Rationale**:

- Public consumers should only ever see published content.
- Admin consumers need drafts, archived items, soft-delete awareness, and role checks.
- Separate surfaces keep authorization and query rules straightforward.
- This matches how the project already uses explicit controllers and endpoint intent.

**Alternatives considered**:

- Single controller with role-based branches: rejected because it would mix visibility concerns and make contracts harder for frontend consumers.

---

## R-003: Rich text persistence model is JSON as source of truth plus HTML snapshot

**Decision**: Persist both `contentJson` and `contentHtml`, with `contentJson` as the canonical write model and `contentHtml` as the rendered snapshot for storefront output.

**Rationale**:

- Editor round-tripping is more reliable from structured JSON than from HTML.
- The storefront benefits from directly renderable HTML for performance and SEO.
- This gives Morii room to evolve editor behavior later without losing structured content.

**Alternatives considered**:

- HTML only: rejected because future editing and schema evolution become brittle.
- JSON only: rejected because the public storefront would need to render structured content on every read path.

---

## R-004: Reuse the shared file upload capability and add a `blogs` public container

**Decision**: Reuse the existing generic file upload workflow and add a blog-specific public container/folder constant such as `FileContainers.BLOGS`.

**Rationale**:

- The repo already has a shared file upload pattern used by other modules.
- Reuse avoids duplicate validation, storage, and response logic.
- A dedicated blog asset container still keeps editorial media separated from products, banners, and user avatars.

**Alternatives considered**:

- Create a blog-specific upload endpoint: rejected because it adds unnecessary surface area for a minimal CMS.

---

## R-005: Treat admin-supplied HTML as trusted in MVP, document sanitization hardening for follow-up

**Decision**: For MVP, accept admin-generated HTML as trusted content and persist it without introducing a new sanitization subsystem in this feature.

**Rationale**:

- This is an internal CMS with a tightly controlled editor surface.
- The repo does not already contain a general HTML sanitization subsystem.
- Adding and validating a sanitizer now would widen scope beyond the agreed minimal feature.

**Alternatives considered**:

- Introduce a server-side HTML sanitizer immediately: rejected for MVP because it adds a new cross-cutting subsystem and policy surface.
- Strip HTML and store plain text only: rejected because it defeats the rich-text requirement.

**Follow-up note**: If Morii later broadens content authorship or editor capabilities, HTML sanitization should become a dedicated hardening task.

---

## R-006: Role split is `ADMIN` for content management and `STAFF` for operational curation

**Decision**: `ADMIN` receives full blog management rights. `STAFF` receives read access plus reorder rights only.

**Rationale**:

- This matches the product decision already made for Morii.
- It preserves a small content-authority surface while still allowing staff to help with merchandising and ordering.
- It fits the existing role model already present in the repo (`ADMIN`, `STAFF`).

**Alternatives considered**:

- Admin-only for everything: rejected because staff operational curation was explicitly requested.
- Per-blog-module roles such as author/editor/admin: rejected as out of scope and too complex for Morii.

---

## R-007: Slug generation is server-assisted and uniqueness is enforced centrally

**Decision**: Allow clients to send a slug, but when absent or blank, generate it from title on the backend and enforce uniqueness in the backend plus database constraint/index.

**Rationale**:

- Backend-enforced uniqueness prevents race conditions and inconsistent client behavior.
- Title-based default slug generation keeps admin UX simple.
- This matches how Morii already treats canonical server-side business rules in other modules.

**Alternatives considered**:

- Require frontend to always generate slug: rejected because canonical uniqueness belongs on the server.
- Always auto-generate without allowing manual override: rejected because editorial use sometimes needs curated slugs.

---

## R-008: Category deletion must be blocked, not silently detached

**Decision**: Block blog category deletion while any non-deleted post remains linked to it, and return a clear validation error.

**Rationale**:

- This was explicitly chosen in business scope.
- Silent detachment would hide content organization changes and create storefront inconsistencies.
- Blocking deletion is safer and easier for admins to reason about.

**Alternatives considered**:

- Cascade detach categories from posts: rejected because it is operationally surprising.
- Hard-delete category and keep orphaned links: rejected as invalid data behavior.

---

## R-009: Public visibility rules are driven solely by post state

**Decision**: Only posts in `Published` state are publicly visible. Featured sections and category listings must also filter by `Published`.

**Rationale**:

- Keeps storefront visibility simple and predictable.
- Avoids needing extra visibility flags or approval states.
- Matches the minimal internal CMS direction.

**Alternatives considered**:

- Additional hidden state or approval gates: rejected as outside agreed scope.

---

## R-010: Ordering is explicit and curated, not inferred

**Decision**: Keep `displayOrder` as an explicit editorial ordering mechanism for posts and categories. Public featured sections use `displayOrder` first, with publication recency as a tie-breaker.

**Rationale**:

- The business wants direct control over which content appears first.
- It reduces ambiguity for frontend implementation.
- It matches existing patterns in banners and categories.

**Alternatives considered**:

- Recency-only ordering: rejected because it reduces merchandising control.
- Separate featured-order field: rejected as unnecessary duplication for MVP.

---

## Summary

The design decisions above keep the feature aligned with Morii's current architecture:

- additive module design
- reused shared infrastructure
- explicit role boundaries
- simple visibility rules
- minimal new cross-cutting concerns

All Phase 0 uncertainties are resolved. Phase 1 can proceed without open clarifications.
