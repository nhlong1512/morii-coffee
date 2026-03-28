# Feature Specification: Restrict Authentication Identity to Email Only

**Feature Branch**: `004-email-only-auth`
**Created**: 2026-03-28
**Status**: Draft
**Input**: User description: "Please follow as @docs/features/identity-email-only/identity-email-only-spec.md to write spec for this feature"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign In with Email Only (Priority: P1)

Users need to sign in to their account using only their email address as the identity field. Phone numbers will no longer be accepted as a valid login credential.

**Why this priority**: This is the core authentication change. Without this, the entire feature fails its primary objective. All existing and new users must use email-based authentication.

**Independent Test**: Can be fully tested by attempting to sign in with an email address (should succeed) and attempting to sign in with a phone number (should fail with validation error). Delivers immediate security and UX consistency.

**Acceptance Scenarios**:

1. **Given** a registered user with email "user@example.com", **When** they submit sign-in with identity="user@example.com" and correct password, **Then** authentication succeeds and they receive valid access tokens
2. **Given** a registered user who previously used phone number for sign-in, **When** they submit sign-in with identity="0123456789", **Then** authentication fails with 400 Bad Request and error message "Email address required for sign-in"
3. **Given** a user at the sign-in form, **When** they submit an invalid email format (e.g., "notanemail"), **Then** request is rejected with 400 Bad Request and error message "Invalid email format"

---

### User Story 2 - Sign Up with Email as Primary Identity (Priority: P2)

Users registering new accounts must provide an email address as their primary authentication identity. Phone number remains an optional profile field.

**Why this priority**: While important for new user onboarding, existing users can still sign in with email. This ensures new accounts align with the new authentication model from day one.

**Independent Test**: Can be tested by creating a new account with email and verifying that email is the only identity used for subsequent authentication. Phone number should be stored but not used for login.

**Acceptance Scenarios**:

1. **Given** a new user at registration, **When** they provide email "newuser@example.com" and optional phone "0987654321", **Then** account is created with email as authentication identity and phone stored as profile data only
2. **Given** a newly registered user, **When** they attempt to sign in with their phone number, **Then** authentication fails even though phone is stored in their profile
3. **Given** a new user at registration, **When** they provide an already-registered email, **Then** registration fails with appropriate error message

---

### User Story 3 - Maintain Phone Number as Profile Field (Priority: P3)

Users can still view and update their phone number in their profile, but it serves informational purposes only and has no role in authentication.

**Why this priority**: This is a data preservation concern rather than authentication functionality. Existing phone numbers should remain accessible but cannot be used for login.

**Independent Test**: Can be tested by viewing user profile data and verifying phone number is present, then attempting to use it for authentication (should fail).

**Acceptance Scenarios**:

1. **Given** a user with phone number in profile, **When** they view their profile, **Then** phone number is displayed correctly
2. **Given** a user updating their profile, **When** they change their phone number, **Then** new phone number is saved but still cannot be used for authentication
3. **Given** a user profile API response, **When** retrieved, **Then** phoneNumber field is included as a profile attribute

---

### Edge Cases

- What happens when a user attempts to sign in with an empty identity field?
- How does the system handle sign-in attempts with email addresses containing special characters or unicode?
- What happens to existing sessions of users who were authenticated with phone number before the change?
- How does the system respond to sign-in attempts with correctly formatted phone numbers (e.g., +1-234-567-8900)?
- What happens if a user has both email and phone number, but only phone number was previously used for authentication?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept only email addresses as valid identity values in the sign-in request (`POST /api/v1/auth/signin`)
- **FR-002**: System MUST reject sign-in attempts with phone numbers as identity, returning 400 Bad Request with clear error message
- **FR-003**: System MUST validate that the identity field contains a properly formatted email address before processing sign-in
- **FR-004**: System MUST remove any phone number lookup logic from authentication and identity resolution processes
- **FR-005**: System MUST continue to store phone numbers as profile fields during sign-up but not use them for authentication
- **FR-006**: System MUST ensure all password reset and account recovery flows use email only, not phone number
- **FR-007**: System MUST maintain phone number as an optional field in user profiles that can be viewed and updated
- **FR-008**: System MUST update API documentation to reflect that identity field accepts email only
- **FR-009**: System MUST ensure refresh token flows do not rely on phone number for identity resolution

### Key Entities

- **User Identity**: Represents the authentication credential used to sign in. After this change, email is the sole identity type for authentication. Phone number exists as profile metadata only.
- **User Profile**: Contains user information including email (auth identity) and phone number (informational field). Phone number has no authentication role.
- **Sign-In Request**: The authentication request payload containing identity (email only) and password fields.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of sign-in attempts with email addresses as identity are processed correctly (success or appropriate failure)
- **SC-002**: 100% of sign-in attempts with phone numbers as identity are rejected with 400 Bad Request status
- **SC-003**: All existing authentication endpoints (sign-in, password reset, token refresh) function without phone number identity resolution
- **SC-004**: User profiles continue to display phone numbers correctly, but phone numbers cannot be used for authentication
- **SC-005**: API documentation accurately reflects that identity field accepts email only

## Assumptions

- Email addresses are already unique in the user database
- All existing users have email addresses associated with their accounts
- Email validation logic already exists in the system or can be implemented using standard patterns
- Breaking change documentation will be communicated to API consumers separately
- Phone number uniqueness constraint (if present) can remain in place as it's still a valid profile field
- Existing sessions/tokens remain valid during the transition; invalidation is handled separately if needed

## Out of Scope

- Migration of users who only have phone numbers (assumes all users have emails)
- User communication/notification about the authentication change
- Phone number verification or validation logic
- Two-factor authentication using phone numbers
- SMS-based password reset flows
- International phone number format standardization
