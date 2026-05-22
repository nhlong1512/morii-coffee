# Feature Specification: My Wishlist

**Feature Branch**: `014-my-wishlist`  
**Created**: 2026-05-22  
**Status**: Ready for Planning  
**Input**: Wishlist feature for Morii Coffee — customers can heart products to save to wishlist with guest/authenticated sync

---

## User Scenarios & Testing

### User Story 1 - Guest User Saves Products to Wishlist (Priority: P1)

As a guest customer browsing products, I want to tap the heart button to save products I love without signing in, so I can come back to them later.

**Why this priority**: This is the core MVP feature and the entry point for all wishlist interactions. Without this, the entire feature has no value.

**Independent Test**: Can be fully tested by:
1. Guest browsing the product catalog
2. Clicking heart icons on product cards
3. Navigating to `/wishlist` 
4. Verifying saved products display with all product info (name, price, image)

**Acceptance Scenarios**:

1. **Given** a guest user viewing a product card, **When** they click the heart icon, **Then** the heart fills with color and the product is added to their local wishlist
2. **Given** a guest user with an empty wishlist, **When** they visit `/wishlist`, **Then** they see an empty state with a "Browse Products" button
3. **Given** a guest user with saved products, **When** they visit `/wishlist`, **Then** they see a grid of all saved products with name, price, image, and remove button
4. **Given** a guest user on the wishlist page, **When** they click the remove button on a product, **Then** that product is removed and the grid updates immediately
5. **Given** a guest user with 3 saved products, **When** they check the item count badge in the header, **Then** it shows "3"

---

### User Story 2 - Authenticated User Syncs Wishlist on Login (Priority: P1)

As a returning customer signing in, I want my guest wishlist to merge with my server wishlist so I don't lose products I saved before signing in.

**Why this priority**: This is critical for retention — customers will abandon the feature if data is lost on login. It's also foundational for the authenticated experience.

**Independent Test**: Can be fully tested by:
1. Guest adding 3 products to wishlist
2. Signing in with an account that has 2 existing wishlist items
3. Verifying all 5 unique products are now in the merged wishlist

**Acceptance Scenarios**:

1. **Given** a guest with 3 saved products who signs in, **When** the page loads, **Then** a merge happens silently and all guest + server items are combined (no duplicates)
2. **Given** a guest with 2 items and server wishlist with 2 items (1 duplicate), **When** merged, **Then** the result has 3 unique items total
3. **Given** an authenticated user viewing the wishlist, **When** they check the store state, **Then** `storageMode` is "authenticated" and all updates sync to the server
4. **Given** an authenticated user clicking the heart on a product, **When** the request completes, **Then** the product is persisted on the server (survives page refresh)

---

### User Story 3 - Add Wishlist Product Directly to Cart (Priority: P1)

As a customer viewing my wishlist, I want to add products directly to my cart from the wishlist page without visiting product detail pages.

**Why this priority**: This completes the user journey — saving products is only valuable if they can easily move to purchase.

**Independent Test**: Can be fully tested by:
1. Open wishlist page with saved products
2. Click "Add to Cart" button on a product
3. Verify product appears in cart

**Acceptance Scenarios**:

1. **Given** a user on the wishlist page with in-stock products, **When** they click "Add to Cart", **Then** the product is added to their cart and a toast notification appears
2. **Given** a user on the wishlist page with out-of-stock products, **When** they hover over an out-of-stock item, **Then** the "Add to Cart" button is disabled
3. **Given** a user with multiple products in wishlist, **When** they click "Add All to Cart", **Then** all in-stock products are added to cart with a single action

---

### User Story 4 - Out of Stock Visibility on Wishlist (Priority: P2)

As a customer viewing my wishlist, I want to see which products are out of stock without visiting the product detail page, so I can know which ones to check back for later.

**Why this priority**: Improves user experience by surfacing inventory status upfront, though not blocking the core wishlist feature.

**Independent Test**: Can be fully tested by:
1. Wishlist contains mix of in-stock and out-of-stock products
2. "Out of Stock" badge displays correctly on each product card
3. "Add to Cart" button is disabled for out-of-stock items

**Acceptance Scenarios**:

1. **Given** a wishlist item with `inStock: false`, **When** the wishlist page loads, **Then** an "Out of Stock" badge is visible on that product card
2. **Given** an out-of-stock item, **When** the user hovers over it, **Then** the "Add to Cart" button shows a disabled state with a tooltip "Out of stock"
3. **Given** a user with all out-of-stock items in wishlist, **When** they click "Add All to Cart", **Then** no products are added and a message appears "No in-stock items to add"

---

### User Story 5 - Heart Button on Product Detail (Priority: P2)

As a customer on a product detail page, I want to save the product to my wishlist using a heart button next to the "Add to Cart" button.

**Why this priority**: Provides wishlist access from the product detail page, though the product card access (P1) covers the primary use case.

**Independent Test**: Can be fully tested by:
1. Navigate to product detail page
2. Click heart button next to "Add to Cart"
3. Verify product appears in wishlist

**Acceptance Scenarios**:

1. **Given** a user on a product detail page, **When** they click the heart icon, **Then** the heart fills and the product is saved to wishlist
2. **Given** a product already in the wishlist, **When** the user visits its detail page, **Then** the heart is already filled to show it's wishlisted

---

### User Story 6 - Wishlist Icon in Header (Priority: P2)

As a customer anywhere on the site, I want to see a heart icon in the header with a count badge so I can quickly jump to my wishlist.

**Why this priority**: Improves discoverability and navigation, but the `/wishlist` route alone satisfies core functionality.

**Independent Test**: Can be fully tested by:
1. Add products to wishlist
2. Header shows heart icon with count
3. Clicking header icon navigates to `/wishlist`

**Acceptance Scenarios**:

1. **Given** a user with 5 saved products, **When** they look at the header, **Then** the heart icon displays a badge with "5"
2. **Given** an empty wishlist, **When** the user checks the header, **Then** the badge shows "0" or hides
3. **Given** a user clicking the header heart icon, **When** the click registers, **Then** they are navigated to `/wishlist`

---

### User Story 7 - Logout Clears Guest Wishlist (Priority: P3)

As a customer signing out, I want my session wishlist cleared so my data isn't visible if someone else uses this device.

**Why this priority**: Security consideration, but not blocking — users expect this behavior with authenticated apps.

**Independent Test**: Can be fully tested by:
1. Authenticated user with wishlisted items signs out
2. Local store is cleared
3. Guest user cannot see previous user's wishlist

**Acceptance Scenarios**:

1. **Given** an authenticated user with wishlist items who logs out, **When** the store resets, **Then** the items are cleared from the local state
2. **Given** a logged-out user, **When** they add items to wishlist, **Then** new items are stored locally and `storageMode` is "guest"

---

### Edge Cases

- **Out of stock handling**: Products marked out of stock remain in wishlist but have disabled "Add to Cart" button — no auto-removal
- **Deleted products**: If a product is deleted from catalog, the wishlist entry is either omitted from API response or returned with minimal data; frontend gracefully handles missing product info
- **Double-tap prevention**: Rapid clicking the same heart button is prevented via `pendingIds` tracking — button stays disabled until in-flight request completes
- **Multi-tab sync**: Guest wishlist changes in tab A automatically sync to tab B via localStorage `storage` event (built-in Zustand behavior)
- **Price changes**: Wishlist always displays current `basePrice` from server, not historical price; no "price changed" notification needed for MVP
- **Large wishlists**: Performance target: render 100+ items with smooth scroll and sub-500ms API response

---

## Requirements

### Functional Requirements

- **FR-001**: System MUST allow guest users to add/remove products to a local wishlist using a heart button on product cards
- **FR-002**: System MUST persist guest wishlist to browser localStorage so data survives page refresh
- **FR-003**: System MUST provide a `/wishlist` page that displays all saved products with product snapshots (name, price, image, stock status)
- **FR-004**: System MUST allow authenticated users to add/remove wishlist items with server sync (POST/DELETE to backend API)
- **FR-005**: System MUST automatically merge guest wishlist into server wishlist when an unauthenticated user signs in (POST /v1/wishlist/merge)
- **FR-006**: System MUST prevent duplicate wishlist entries across guest ↔ authenticated transition
- **FR-007**: System MUST display real-time out-of-stock status on wishlist items and disable "Add to Cart" for out-of-stock products
- **FR-008**: System MUST prevent double-tap/rapid requests on the same product using per-productId pending state tracking
- **FR-009**: System MUST clear wishlist store on user logout so previous user's data is not accessible
- **FR-010**: System MUST display a wishlist count badge in the header (mirrors cart pattern)
- **FR-011**: System MUST support "Add All to Cart" action to add all in-stock items to cart from wishlist page
- **FR-012**: System MUST show empty state on wishlist page when no items exist
- **FR-013**: System MUST use optimistic updates for add/remove actions with automatic rollback on API failure
- **FR-014**: System MUST support heart button variant on product detail pages
- **FR-015**: System MUST translate all wishlist strings (empty state, buttons, badges) via i18n for both English and Vietnamese

### Key Entities

- **WishlistItem**: Represents a product snapshot in the wishlist
  - Attributes: `productId`, `name`, `slug`, `price`, `image`, `inStock`, `addedAt`
  - Relationships: Linked to Product (via productId), belongs to Wishlist (one-to-many)

- **Wishlist**: Container for a user's saved products
  - Attributes: `items` (WishlistItem[]), `storageMode` ("guest" | "authenticated"), `syncError`, `pendingIds`, `isReady`
  - Relationships: One per authenticated user, per-device for guest users

- **ApiWishlistItem** (API contract): What backend returns
  - Attributes: `productId`, `productName`, `productSlug`, `basePrice`, `thumbnailUrl`, `inStock`, `addedAt`

---

## Success Criteria

### Measurable Outcomes

- **SC-001**: Users can add a product to wishlist and see it persisted within 100ms (optimistic update appears immediately)
- **SC-002**: Wishlist page loads and renders 100 products within 500ms (API response time + client-side render)
- **SC-003**: Guest users who sign in retain all pre-login wishlist items with zero data loss during merge
- **SC-004**: Wishlist feature reduces clicks to purchase by 40% compared to re-browsing catalog (measured via analytics if available)
- **SC-005**: 95% of wishlist add/remove operations succeed on first attempt without requiring retry
- **SC-006**: Multi-tab wishlist updates sync automatically within 500ms when changes occur in one tab
- **SC-007**: Out-of-stock status displays accurately within 2 hours of product status change on backend
- **SC-008**: Zero crashes or errors when rendering wishlist with 10+ products simultaneously
- **SC-009**: "Add All to Cart" action completes (all in-stock items added) within 1 second
- **SC-010**: Wishlist feature is fully accessible (WCAG AA) for keyboard and screen reader users
- **SC-011**: All wishlist UI strings render correctly in both English and Vietnamese
- **SC-012**: Feature works seamlessly on desktop (1920px+), tablet (768px), and mobile (375px) viewports

---

## Assumptions

1. **Backend API exists**: All 5 wishlist endpoints (GET /v1/wishlist, POST /v1/wishlist/items, DELETE /v1/wishlist/items/{productId}, DELETE /v1/wishlist, POST /v1/wishlist/merge) are available and functional
2. **Cart integration ready**: Cart store already has `addItem()` and `removeItem()` methods compatible with the proposed "Add to Cart" action
3. **Auth flow established**: `useAuthStore` has `isAuthenticated` state and login/logout hooks; `initializeForSession()` is called on auth state changes
4. **Product data available**: Backend returns accurate `inStock` status and product snapshots (name, price, image) in wishlist API responses
5. **i18n infrastructure ready**: `i18n` library (e.g., next-intl or i18next) is configured and can load new message namespaces
6. **localStorage available**: Guest wishlist relies on browser localStorage; assume it's available (no need for fallback to sessionStorage for MVP)
7. **No backend wishlist history**: Wishlist items are simple key-value pairs; no need to track modification history or analytics
8. **Optimistic updates acceptable**: Users accept rollback on failure; feature does not require optimistic rollback animations for MVP

---

## Dependencies & Constraints

- **Timeline**: Report expected to complete within 2 sprints (10 working days) assuming backend endpoints are ready
- **External dependencies**: Brevo/SendGrid for email (not needed for wishlist), MinIO for file storage (not needed for wishlist)
- **Database constraints**: Assume Database can handle wishlist queries efficiently (indexing on userId, productId)
- **Browser support**: Must support all modern browsers (Chrome 90+, Safari 14+, Firefox 88+); IE 11 not required
