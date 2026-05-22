# Feature Specification: Store Management

**Feature Branch**: `015-store-management`
**Created**: 2026-05-22
**Status**: Draft

---

## User Scenarios & Testing

### User Story 1 — Admin Creates and Manages Store Locations (Priority: P1)

An admin needs to create, update, and deactivate Morii Coffee physical branch locations. For each store, the admin sets the name, address, geographic coordinates, contact details, and a structured 7-day opening schedule (so the public site can compute "open now" automatically).

**Why this priority**: Without store data in the system, the public locator page has nothing to show. All other stories depend on stores existing.

**Independent Test**: An admin logs in, navigates to the admin Stores section, creates one store with all 7 days of opening hours, verifies it appears in the admin list, edits the name, then soft-deletes it — confirming it disappears from the public list but remains accessible to admins.

**Acceptance Scenarios**:

1. **Given** an admin is logged in, **When** they submit a valid store creation form (name, address, city, coordinates, phone, 7-day hours), **Then** the store is saved and immediately visible in the admin store list.
2. **Given** a store exists, **When** an admin submits an edit with updated fields, **Then** the store reflects all changes.
3. **Given** a store exists, **When** an admin deletes it, **Then** the store no longer appears on the public page, but remains visible in the admin list (soft-deleted, not physically removed).
4. **Given** two stores would have the same name or slug, **When** admin tries to save, **Then** the system rejects the duplicate and shows a clear error.
5. **Given** an admin submits a form missing required fields (name, city, coordinates, phone, or opening hours), **Then** the form shows validation errors before submission.
6. **Given** an admin submits opening hours with fewer or more than 7 entries, **When** the request is sent, **Then** the system rejects it with a validation error.

---

### User Story 2 — Admin Toggles Store Visibility (Priority: P2)

An admin can quickly activate or deactivate a store without going through the full edit form — a single status toggle on the list page.

**Why this priority**: Operational necessity for managing temporary closures or new openings without full edit overhead.

**Independent Test**: Admin clicks the status toggle on a store in the list. The status badge updates immediately. Navigating to `/stores` confirms the store is now hidden (if deactivated) or visible (if activated).

**Acceptance Scenarios**:

1. **Given** an active store exists, **When** admin toggles it to inactive, **Then** the store disappears from the public `/stores` page immediately.
2. **Given** an inactive store exists, **When** admin toggles it to active, **Then** the store appears on the public `/stores` page.
3. **Given** admin toggles a store status, **When** successful, **Then** a confirmation toast is shown with the new status.

---

### User Story 3 — Public Visitor Finds a Store Near Them (Priority: P2)

A visitor to the public `/stores` page wants to find the closest Morii Coffee location. They can optionally share their device location, filter by city, and search by name or address. Each store card shows real-time "open" or "closed" status based on the structured hours.

**Why this priority**: This is the primary customer-facing value of the feature. It directly drives foot traffic to physical stores.

**Independent Test**: A visitor opens `/stores`, sees a list of active stores with open/closed status badges. They click "Near Me", grant location permission, and the list re-sorts by proximity with distance labels. Filtering by city narrows the list. Searching by name shows matching results.

**Acceptance Scenarios**:

1. **Given** active stores exist, **When** a visitor opens `/stores`, **Then** all active stores are listed with name, address, phone, and open/closed status.
2. **Given** a visitor grants location permission, **When** the page detects coordinates, **Then** stores are sorted by distance ascending and each card shows a distance label (e.g., "1.2 km away").
3. **Given** a visitor denies location permission, **When** detection fails, **Then** the full list is shown without distance information and without any error state.
4. **Given** a visitor selects a city filter, **When** the filter is applied, **Then** only stores in that city are shown without a page reload.
5. **Given** a visitor types in the search box, **When** they search "District 1", **Then** only stores with matching name or address appear.
6. **Given** today is Sunday and a store has its Sunday schedule marked as closed, **When** a visitor views the store card, **Then** it shows "Closed today".
7. **Given** the current time is before a store's opening time, **When** a visitor views the card, **Then** it shows "Opens at HH:MM".
8. **Given** no stores match a search or filter, **When** the result is empty, **Then** a friendly "No stores found" message is displayed.
9. **Given** the home page loads, **When** the store preview section renders, **Then** it displays the first 3 active stores from live data (no hardcoded values).

---

### User Story 4 — Public Visitor Explores Stores on a Map (Priority: P3)

A visitor can view active store locations as pins on an interactive map alongside the list. Clicking a map pin highlights the corresponding list card and shows a summary popup. Clicking a list card centers the map on that store.

**Why this priority**: Enhances discovery but not critical for MVP — the list view alone delivers the core need.

**Independent Test**: The map renders with a pin for each active store. Clicking any pin shows a popup with store details. The corresponding list card scrolls into view and is visually highlighted.

**Acceptance Scenarios**:

1. **Given** active stores exist with coordinates, **When** the map loads, **Then** each store shows a pin at its correct geographic position.
2. **Given** a visitor clicks a map pin, **When** the popup opens, **Then** it shows the store's name, address, phone number, and today's hours.
3. **Given** a visitor clicks a map pin, **When** the popup opens, **Then** the corresponding card in the list is highlighted and scrolled into view.
4. **Given** a visitor clicks a list card, **When** selected, **Then** the map centers on that store's pin.
5. **Given** the map provider is unavailable or misconfigured, **When** the page loads, **Then** the list view remains fully functional without the map.

---

### User Story 5 — Admin Reorders Store Display Sequence (Priority: P4)

An admin or staff member can rearrange the order in which stores appear on the public page, using a drag-and-drop or numeric ordering interface.

**Why this priority**: Operational control, but stores still appear on the public page without this — ordering is secondary.

**Independent Test**: Admin opens the Ordering tab on the admin stores page, drags a store to a new position, saves, then verifies the public `/stores` page reflects the updated order.

**Acceptance Scenarios**:

1. **Given** multiple stores exist, **When** an admin changes the display order and saves, **Then** the public page shows stores in the new order.
2. **Given** a staff member accesses the stores admin page, **When** they attempt to reorder, **Then** the reorder action succeeds (staff has reorder permission).
3. **Given** a staff member attempts to create or delete a store, **When** the action is submitted, **Then** the system rejects it with a permission error.

---

### Edge Cases

- What happens when geolocation returns coordinates far from Vietnam? The system shows all stores without distance filtering, with no error.
- What happens when a store has no cover image? A placeholder or default visual state is shown.
- What happens when a store's opening hours span midnight (e.g., open 22:00 to 02:00)? Cross-midnight hours are out of scope — open/closed logic assumes same-day time windows only.
- What happens when an admin submits exactly 6 or 8 opening hours entries? The system rejects the request — exactly 7 entries are required.
- What happens when the map provider API key is missing? The map section is skipped gracefully; the list view remains fully functional.
- What happens when a visitor accesses `/stores` with no stores in the database? An empty state with a friendly message is shown.
- What happens if a store's slug conflicts on update? The system rejects the update and returns a clear uniqueness error.

---

## Requirements

### Functional Requirements

**Public Store Locator:**

- **FR-001**: The system MUST display all active, non-deleted stores on the public locator page.
- **FR-002**: The system MUST show open/closed status for each store, computed from structured 7-day opening hours and the visitor's current local time, without any additional user action.
- **FR-003**: The system MUST support geolocation-based sorting — when a visitor provides their coordinates, stores are sorted by proximity ascending and each item shows a distance label.
- **FR-004**: The system MUST degrade gracefully when geolocation is denied or unavailable — showing the full list without distance data and no error state.
- **FR-005**: The system MUST support client-side city filtering — deriving unique city values from the fetched store list and filtering without additional server requests.
- **FR-006**: The system MUST support name and address text search across the store list.
- **FR-007**: The system MUST render an interactive map with one marker per active store, supporting click-to-highlight interactions between the map pins and the list cards.
- **FR-008**: The home page store preview section MUST display up to 3 active stores sourced from live data, replacing all static dummy data.

**Admin Store Management:**

- **FR-009**: Admins MUST be able to create stores providing: name, optional slug (auto-generated from name if omitted), address, optional district, city, optional province, latitude, longitude, phone, optional email, optional cover image URL, active status, display order, and exactly 7 opening hours entries (one per day of week, 0=Sunday to 6=Saturday).
- **FR-010**: Store names and slugs MUST be unique across all non-deleted stores.
- **FR-011**: Admins MUST be able to perform a full update of any store's details, including replacing all opening hours.
- **FR-012**: Admins MUST be able to soft-delete stores — deleted stores are hidden from public views but remain accessible in admin views.
- **FR-013**: Admins MUST be able to toggle a store's active status as a standalone operation (without a full update).
- **FR-014**: Admins and Staff MUST be able to update the display order of multiple stores in a single batch operation.
- **FR-015**: Create, full-update, and delete operations are restricted to Admin role only. Staff role may view all stores and perform reorder.
- **FR-016**: The admin interface MUST provide paginated store lists filterable by active status and city.

### Key Entities

- **Store**: A physical Morii Coffee branch location. Has a unique name, unique slug (auto-generated from name if omitted), street address, optional district, city, optional province, geographic coordinates (latitude/longitude), phone number, optional email, optional cover image URL, active flag, display order integer, and exactly 7 opening hours child records. Supports soft-delete — deleted stores are excluded from public results but retained in the database and visible to admins.

- **StoreOpeningHours**: A child record belonging to one Store. Represents one day of the week (0=Sunday through 6=Saturday). Contains an open time and close time in HH:mm 24-hour format, plus a boolean closed flag for days when the store does not operate. Exactly 7 records exist per store — one per day.

---

## Success Criteria

### Measurable Outcomes

- **SC-001**: Visitors can find a nearby Morii Coffee store in under 30 seconds from page load when location is granted — without needing to scroll or search manually.
- **SC-002**: Open/closed status is computed and displayed on each store card with no additional loading state or user action after the page loads.
- **SC-003**: The public stores page delivers all active stores in a single request within 2 seconds under normal conditions.
- **SC-004**: An admin can create a new store with full opening hours in under 3 minutes via the admin form.
- **SC-005**: Store status changes (activate/deactivate) are reflected on the public page within one page refresh.
- **SC-006**: Zero hardcoded store data remains in the codebase after implementation — all store information is served from the database.
- **SC-007**: Admin store list supports filtering by city and active status, with results updating without a full page reload.
- **SC-008**: All public-facing store content is available in both English and Vietnamese via the internationalization system.

---

## Assumptions

- The map API key is already provisioned in the environment. The map feature (US4) depends on this being valid and correctly scoped for the domain.
- Cross-midnight opening hours (e.g., open from 22:00 to 02:00) are out of scope — open/closed computation assumes same-day time windows.
- Distance computation uses straight-line (Haversine) distance, not driving distance.
- For the initial rollout, city filtering is client-side. If the store count grows significantly in the future, server-side filtering is already supported by the API design.
- Cover image upload is out of scope — the form accepts a URL input. Integration with the existing file upload service can be added later.
- A public store detail page (`/stores/{id}`) is out of scope — the public view is list + map only.
- Initial seed data with at least 5 store records (replacing the existing static dummy data) is included in the backend deliverable.
