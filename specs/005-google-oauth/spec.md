# Feature Specification: Google OAuth External Authentication

**Feature Branch**: `005-google-oauth`
**Created**: 2026-03-28
**Status**: Draft
**Input**: User description: "External authentication with Google OAuth provider for MoriiCoffee users"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Quick Sign-In with Google Account (Priority: P1)

New and existing users need to sign in to MoriiCoffee using their existing Google account without creating a separate password. Users click "Sign in with Google", authenticate through Google's login page, and are automatically logged into MoriiCoffee.

**Why this priority**: This is the core OAuth flow. Without this, the entire feature is non-functional. Enables frictionless onboarding for users who prefer not to create yet another password.

**Independent Test**: Can be fully tested by clicking "Sign in with Google" button, completing Google authentication, and verifying successful login to MoriiCoffee with access to authenticated features. Delivers immediate value by allowing passwordless authentication.

**Acceptance Scenarios**:

1. **Given** a new user visits MoriiCoffee, **When** they click "Sign in with Google" and complete Google authentication, **Then** a new customer account is created automatically with their Google email and profile information
2. **Given** an existing MoriiCoffee user with email matching their Google account, **When** they sign in with Google for the first time, **Then** their Google account is linked to their existing MoriiCoffee account without creating a duplicate
3. **Given** a user completes Google authentication successfully, **When** the system processes the callback, **Then** the user receives access and refresh tokens and is redirected to the requested page

---

### User Story 2 - Automatic Account Creation and Role Assignment (Priority: P2)

When a new user signs in with Google, the system automatically creates a customer account with information from their Google profile (name, email) and assigns the CUSTOMER role, granting immediate access to customer features.

**Why this priority**: While important for new users, existing users can already authenticate via P1. This ensures new Google users can immediately browse products and place orders without additional registration steps.

**Independent Test**: Can be tested by using a Google account that has never been used with MoriiCoffee, completing sign-in, and verifying a new customer account exists with CUSTOMER role assigned.

**Acceptance Scenarios**:

1. **Given** a user signs in with Google for the first time, **When** the account is created, **Then** the user's email, full name, and phone number (if provided by Google) are stored in the user profile
2. **Given** a new Google user account is created, **When** the system assigns roles, **Then** the CUSTOMER role is automatically assigned to enable product browsing and order placement
3. **Given** a new account is created via Google sign-in, **When** the process completes, **Then** a welcome email is sent to the user's email address

---

### User Story 3 - Seamless Token Management and Session Handling (Priority: P3)

After successful Google authentication, users receive access and refresh tokens that maintain their authenticated session. The tokens are securely stored and automatically used for subsequent API requests.

**Why this priority**: This is infrastructure for maintaining authenticated sessions. While critical for the overall flow, it's automatic once P1 and P2 are working. Users don't directly interact with tokens.

**Independent Test**: Can be tested by signing in with Google, making authenticated API requests with the received access token, and verifying the refresh token can obtain new access tokens when the original expires.

**Acceptance Scenarios**:

1. **Given** a user completes Google authentication, **When** tokens are generated, **Then** both access token and refresh token are returned to the client in a secure cookie with 5-minute expiration
2. **Given** a user's access token has expired, **When** they use their refresh token to request new tokens, **Then** a new access token is issued without requiring re-authentication through Google
3. **Given** a user signs in with Google multiple times, **When** tokens are stored, **Then** each new refresh token replaces the previous one to prevent token accumulation

---

### Edge Cases

- What happens when a user denies Google's permission request during OAuth flow?
- How does the system handle users whose Google account has no verified email address?
- What occurs if a user's Google account is linked to an inactive or deleted MoriiCoffee account?
- How does the system respond when Google's OAuth service is temporarily unavailable?
- What happens if a user attempts to link the same Google account to multiple MoriiCoffee accounts?
- How does the system handle redirect URI mismatches or invalid OAuth state parameters?
- What occurs when a user navigates away during the Google authentication flow and returns later?
- How does the system handle expired or revoked Google OAuth tokens?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST initiate Google OAuth flow when user requests external authentication with Google provider
- **FR-002**: System MUST redirect users to Google's authentication page with appropriate OAuth parameters (client ID, redirect URI, scopes, state)
- **FR-003**: System MUST process OAuth callback from Google and exchange authorization code for user profile information
- **FR-004**: System MUST create new user accounts automatically when a Google email does not exist in the database
- **FR-005**: System MUST link Google accounts to existing user accounts when email addresses match
- **FR-006**: System MUST assign CUSTOMER role to all new users created via Google authentication
- **FR-007**: System MUST extract and store user profile information from Google (email, full name, phone number) during account creation
- **FR-008**: System MUST send welcome email to new users created through Google sign-in
- **FR-009**: System MUST generate access tokens and refresh tokens after successful Google authentication
- **FR-010**: System MUST store refresh tokens securely in the database associated with the Google login provider
- **FR-011**: System MUST return authentication tokens to the client in a secure, HttpOnly cookie with short expiration (5 minutes)
- **FR-012**: System MUST validate OAuth state parameter to prevent CSRF attacks
- **FR-013**: System MUST handle OAuth errors gracefully (user denial, invalid state, missing email) with appropriate error messages
- **FR-014**: System MUST prevent duplicate account creation when a user signs in with Google multiple times
- **FR-015**: System MUST support configurable redirect URLs for different environments (development, production)

### Key Entities

- **External Login Association**: Links a MoriiCoffee user account to their Google account. Contains Google's provider key (unique Google user ID), login provider name ("Google"), and associated MoriiCoffee user ID.
- **User Account**: Customer account in MoriiCoffee system. For Google users, includes email, full name, phone number (optional), account status, assigned roles, and timestamps. Email serves as the unique identifier matching across Google and MoriiCoffee.
- **Authentication Tokens**: Access token (short-lived) for API authentication and refresh token (long-lived) for obtaining new access tokens. Refresh tokens are stored in database linked to Google login provider.
- **OAuth Callback Data**: Temporary data received from Google including authorization code, state parameter for CSRF protection, and user profile claims (email, name, phone).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete Google sign-in and access MoriiCoffee authenticated features in under 30 seconds (including Google authentication time)
- **SC-002**: 100% of Google sign-in attempts with valid Google accounts result in either successful authentication or clear error message
- **SC-003**: New users created via Google sign-in automatically receive CUSTOMER role and can immediately browse products
- **SC-004**: Access tokens issued after Google authentication successfully authorize API requests for their configured lifetime
- **SC-005**: Refresh tokens can successfully obtain new access tokens without requiring re-authentication through Google
- **SC-006**: No duplicate accounts are created when the same Google email attempts sign-in multiple times
- **SC-007**: OAuth state parameter validation prevents 100% of CSRF attack attempts
- **SC-008**: Welcome emails are delivered to new Google users within 1 minute of account creation

## Assumptions

- Google OAuth 2.0 service availability is 99.9% or higher (industry standard for Google services)
- All Google users have a verified email address associated with their account
- MoriiCoffee application is accessible via HTTPS in production (required for secure OAuth flow)
- Frontend application can handle cookie-based token storage and extraction
- Users have enabled cookies in their browsers (standard OAuth requirement)
- Google Cloud Console is configured with correct OAuth credentials and authorized redirect URIs before deployment
- Email service is functional for sending welcome emails to new users
- Existing MoriiCoffee users who want to link their Google account have matching email addresses
- Token expiration times are configurable and follow security best practices (short access tokens, long refresh tokens)

## Out of Scope

- Integration with other OAuth providers (Facebook, Microsoft, Apple) beyond Google
- Two-factor authentication (2FA) through Google account
- Google One Tap sign-in widget
- Automatic Google account linking based on criteria other than email address
- Administrative interface for viewing or managing external login associations
- OAuth token revocation workflow for security incidents
- Custom Google OAuth scopes beyond standard profile and email
- Google Workspace (G Suite) domain-restricted authentication
- Migration tool for converting password-based accounts to Google-only accounts
- Analytics or metrics tracking for authentication method preferences
- Account merging when multiple MoriiCoffee accounts exist with the same email

## Dependencies

- Google Cloud Console project with OAuth 2.0 credentials configured
- Application configuration values (Google Client ID, Client Secret) stored securely
- HTTPS access to MoriiCoffee API for OAuth callback handling
- Email service availability for sending welcome emails to new users
- Frontend application capability to initiate OAuth flow and handle callback cookies
