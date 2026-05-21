# Feature Specification: Blog Management

**Feature Branch**: `012-blog-management`  
**Created**: 2026-05-21  
**Status**: Draft  
**Input**: User description: "Build a minimal internal blog CMS for Morii Coffee using the agreed business scope around post management, categories, publish state, featured content, ordering, and public blog display."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Admin manages blog post lifecycle (Priority: P1)

An administrator creates, edits, publishes, unpublishes, archives, and soft-deletes blog posts from an internal admin area. Each post can have a title, slug, summary, body content, cover image, categories, featured flag, and display order. Once published, the post becomes available on the storefront without requiring a separate approval workflow.

**Why this priority**: This is the core business capability. Without post lifecycle management, Morii has no usable CMS and cannot publish editorial content.

**Independent Test**: An administrator creates a new post, saves it as a draft, updates it, publishes it, unpublishes it, archives it, and confirms the post appears or disappears from the public site based on its state.

**Acceptance Scenarios**:

1. **Given** an administrator is working on a new post, **When** they save it as a draft, **Then** the post is stored in the admin area and does not appear on the public site.
2. **Given** a valid draft post, **When** an administrator publishes it, **Then** the post becomes visible on the public site and records the time it was first published.
3. **Given** a published post, **When** an administrator changes it back to draft or archived, **Then** the post is removed from the public site immediately while remaining available in admin.
4. **Given** an existing post, **When** an administrator soft-deletes it, **Then** the post is no longer returned in standard admin or public listings but remains retained for audit and recovery purposes.

---

### User Story 2 - Customer browses published blog content (Priority: P1)

A storefront visitor can browse a list of published blog posts, open a post by its slug, view featured posts, and filter content by category. Only content intentionally published by Morii should ever be publicly visible.

**Why this priority**: Publishing content only matters if customers can discover and read it reliably on the storefront.

**Independent Test**: Publish several posts across categories, mark some as featured, then verify that a storefront visitor can browse only published content, filter by category, open a post detail page, and never see drafts or archived posts.

**Acceptance Scenarios**:

1. **Given** multiple posts exist in different states, **When** a storefront visitor opens the blog listing, **Then** they see only published posts.
2. **Given** a published post has a unique slug, **When** a storefront visitor opens that slug, **Then** they see the full public version of the post.
3. **Given** featured published posts exist, **When** the storefront requests featured content, **Then** only featured published posts are returned in the intended order.
4. **Given** a visitor filters by category, **When** they browse the blog listing, **Then** only published posts linked to that category are shown.

---

### User Story 3 - Admin manages blog categories safely (Priority: P2)

An administrator manages blog categories used for organizing storefront content. Categories can be created, updated, activated, deactivated, reordered, and removed only when no current blog post still depends on them.

**Why this priority**: Categories are important for navigation and editorial organization, but Morii can still publish a minimal set of posts before deeper category workflows are complete.

**Independent Test**: Create categories, assign them to posts, attempt to delete one that is still in use, confirm deletion is blocked with a clear message, then remove the category from all linked posts and delete it successfully.

**Acceptance Scenarios**:

1. **Given** an administrator creates a category, **When** they save it, **Then** it becomes available for post assignment and public navigation when active.
2. **Given** a category is still linked to one or more posts, **When** an administrator attempts to delete it, **Then** the system blocks the deletion and explains why.
3. **Given** a category is no longer linked to any posts, **When** an administrator deletes it, **Then** the category is removed from active use without breaking existing content rules.

---

### User Story 4 - Staff supports operational curation without editing content (Priority: P3)

Staff members who do not have full administrative permissions can review blog and category lists and help maintain ordering for curated presentation areas, without being allowed to create, edit, publish, or delete content.

**Why this priority**: This supports day-to-day merchandising and coordination while keeping content authority narrow and simple.

**Independent Test**: Sign in as a staff user, confirm blog and category lists are visible, confirm order changes can be made, and confirm content creation, editing, status changes, and deletion are denied.

**Acceptance Scenarios**:

1. **Given** a staff user opens the internal blog management area, **When** they view posts and categories, **Then** they can inspect the current records and ordering.
2. **Given** a staff user needs to adjust display order, **When** they reorder posts or categories, **Then** the new order is saved successfully.
3. **Given** a staff user attempts to create, edit, publish, or delete content, **When** they submit the action, **Then** the system denies the operation.

### Edge Cases

- What happens when two posts attempt to use the same slug?
- How does the system behave when a published post is reassigned away from its last category?
- What happens when a post is marked as featured but is not published?
- How does the system behave when an inactive category is still linked to existing posts?
- What happens when an administrator attempts to delete a category that is still referenced by archived or draft posts?
- How does the public site respond when a visitor requests a slug that belongs to a draft, archived, or soft-deleted post?
- What happens when display ordering values collide across multiple posts or categories?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow administrators to create blog posts with title, slug, summary, body content, cover image, category assignments, featured flag, display order, and search metadata.
- **FR-002**: The system MUST allow administrators to update all editable fields of an existing blog post.
- **FR-003**: The system MUST support the blog post states `Draft`, `Published`, and `Archived`.
- **FR-004**: The system MUST allow administrators to save incomplete work as a draft.
- **FR-005**: The system MUST allow administrators to move a blog post from draft to published when the post meets publish-ready rules.
- **FR-006**: The system MUST allow administrators to move a published post back out of public visibility without permanently deleting it.
- **FR-007**: The system MUST soft-delete blog posts rather than permanently removing them from records by default.
- **FR-008**: The system MUST ensure each blog post slug is unique.
- **FR-009**: The system MUST allow a blog post to be linked to multiple categories.
- **FR-010**: The system MUST allow administrators to mark a post as featured.
- **FR-011**: The system MUST allow administrators and staff with curation access to set and update display order for posts.
- **FR-012**: The system MUST expose only published blog posts on the public storefront.
- **FR-013**: The system MUST ensure drafts, archived posts, and soft-deleted posts are never shown on the public storefront.
- **FR-014**: The system MUST allow storefront users to view a published post by its slug.
- **FR-015**: The system MUST allow storefront users to browse published posts by category.
- **FR-016**: The system MUST allow storefront users to browse featured published posts separately from the main listing.
- **FR-017**: The system MUST allow administrators to create, update, activate, deactivate, reorder, and delete blog categories.
- **FR-018**: The system MUST ensure each category slug is unique.
- **FR-019**: The system MUST block category deletion while any existing non-deleted blog post remains linked to that category.
- **FR-020**: The system MUST provide a clear explanation when category deletion is blocked because of active links.
- **FR-021**: The system MUST record creation and last-updated timestamps for each post and category.
- **FR-022**: The system MUST record the first published timestamp for a post when it becomes publicly visible.
- **FR-023**: The system MUST allow administrators full content management rights within the blog module.
- **FR-024**: The system MUST allow staff users to view internal blog/category records and adjust ordering, but MUST NOT allow them to create, edit content, change publish state, or delete records.
- **FR-025**: The system MUST keep the blog module free of editorial approval workflow in this feature.

### Key Entities *(include if feature involves data)*

- **Blog Post**: A piece of editorial content managed internally by Morii, with lifecycle state, public slug, body content, cover image, categories, featured flag, ordering, and audit timestamps.
- **Blog Category**: A grouping used to organize blog posts for admin management and public navigation, with active state, display order, and safe deletion rules.
- **Category Assignment**: The relationship that connects one blog post to one or more categories and determines how content is organized.
- **Publication State**: The visibility state of a blog post that controls whether it is internal-only or publicly accessible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An administrator can create a new draft post and save it successfully in under 3 minutes on first attempt.
- **SC-002**: A valid post can be moved from draft to public visibility in under 1 minute once its content is ready.
- **SC-003**: 100% of draft, archived, and soft-deleted posts remain hidden from public blog listings and public detail pages during acceptance testing.
- **SC-004**: 100% of attempted category deletions for categories still linked to posts are blocked with a clear user-facing explanation during acceptance testing.
- **SC-005**: Staff users can complete ordering tasks without gaining any content editing or publish rights in 100% of role-based acceptance tests.
- **SC-006**: A storefront visitor can open a published blog post by slug and navigate featured or categorized blog content in under 2 minutes without encountering unpublished content.
- **SC-007**: The feature can support at least 50 published posts and 20 categories while preserving correct filtering, ordering, and visibility behavior in staging validation.

## Assumptions

- The blog CMS is intended for internal Morii operations rather than external contributors.
- Author-byline ownership, editorial review, and scheduled publishing are not required for this feature.
- A post may remain uncategorized while in draft, but published content is expected to be categorized for storefront organization.
- Cover images are optional for draft content but recommended for publicly visible content.
- Display order is used for curated presentation and may coexist with recency-based browsing.

## Dependencies

- Existing authentication and role management remain available for distinguishing administrators and staff users.
- Existing internal admin access patterns remain available to host blog management screens.
- The storefront already has or will add a blog section that can consume published content once this feature is implemented.

## Out of Scope

- Content approval workflow
- Post revision history and rollback
- Scheduled publish and expiry windows
- Author profiles and contributor management
- Reader comments or reactions
- Personalized content recommendations
- Multi-language editorial content
- External publishing integrations
