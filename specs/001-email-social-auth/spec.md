# Feature Specification: Email Integration and Social Login Planning

**Feature Branch**: `001-email-social-auth`
**Created**: 2026-03-23
**Status**: Draft
**Input**: User description: "Part 1: Implement Email Sending via SendGrid for welcome emails and password reset. Part 2: Plan OAuth2 social login with Google and Meta"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Welcome Email on Sign Up (Priority: P1)

When a new user successfully creates an account through the coffee shop's registration system, they receive a welcoming email that confirms their account creation and provides a direct link to start shopping.

**Why this priority**: First impressions matter. A welcome email immediately after sign-up confirms to the user that their account was created successfully, builds trust, and encourages immediate engagement with the storefront. This is a critical touchpoint in the user onboarding journey.

**Independent Test**: Can be fully tested by creating a new user account via the signup endpoint and verifying that an email arrives at the registered email address containing the user's name, welcome message, and a clickable link to the storefront.

**Acceptance Scenarios**:

1. **Given** a visitor completes the sign-up form with valid email and password, **When** the account is successfully created, **Then** a welcome email is sent to their registered email address within 1 minute
2. **Given** a welcome email is sent, **When** the user opens the email, **Then** they see their username, a friendly welcome message in the Morii Coffee brand tone, and a prominent call-to-action button linking to the storefront
3. **Given** the email service fails to send, **When** user account creation occurs, **Then** the account is still created successfully and the user can sign in immediately (email failure does not block registration)

---

### User Story 2 - Password Reset Email (Priority: P2)

When a user forgets their password and requests a reset, they receive an email containing a secure, time-limited link to create a new password.

**Why this priority**: Password recovery is essential for user retention. Without a working forgot-password flow, users who lose access to their accounts cannot recover them, leading to frustration and potentially lost customers. While slightly lower priority than welcome emails (P1), it's still critical for maintaining user access.

**Independent Test**: Can be fully tested by submitting a forgot-password request with a registered email address and verifying that a reset email arrives containing a valid, time-limited reset link that successfully allows password change.

**Acceptance Scenarios**:

1. **Given** a registered user clicks "Forgot Password" and enters their email, **When** they submit the request, **Then** a password reset email is sent to their email address within 1 minute
2. **Given** a password reset email is sent, **When** the user opens the email, **Then** they see a clear "Reset Password" call-to-action button linking to the password reset page with a secure token and their email pre-filled
3. **Given** a password reset link is used, **When** the user clicks it within the expiry window, **Then** they are directed to a password reset form that accepts their new password and successfully updates their account
4. **Given** a password reset link has expired, **When** the user clicks it after the expiry time, **Then** they see an error message indicating the link has expired and are prompted to request a new reset link
5. **Given** an invalid email address is submitted to forgot-password, **When** the request is processed, **Then** the system returns success without revealing whether the email exists (security best practice)

---

### User Story 3 - Social Login Planning (Priority: P3)

As part of the future roadmap, users will be able to sign in using their existing Google or Meta (Facebook) accounts through OAuth2, eliminating the need to remember another password and reducing registration friction.

**Why this priority**: While valuable for user convenience and reducing sign-up friction, social login is not immediately critical for the MVP. Users can already register and sign in with email/password. Social login becomes more valuable as the user base grows and conversion optimization becomes a focus.

**Independent Test**: This is a planning story only (not implemented in Part 1). Success is measured by having a comprehensive, ready-to-execute implementation plan covering endpoints, OAuth2 flows, database schema changes, frontend components, configuration, and edge cases.

**Acceptance Scenarios**:

1. **Given** the implementation plan is complete, **When** reviewed by a developer, **Then** all necessary backend endpoints are identified with clear request/response contracts
2. **Given** the implementation plan is complete, **When** reviewed by a developer, **Then** the OAuth2 authorization code flow is documented end-to-end with clear handoffs between frontend, OAuth provider, and backend
3. **Given** the implementation plan is complete, **When** reviewed by a developer, **Then** all domain entity changes (User fields for external provider tracking) are specified with migration requirements
4. **Given** the implementation plan is complete, **When** reviewed by a developer, **Then** edge cases (email conflicts, denied consent, unverified provider emails) have documented handling strategies

---

### Edge Cases

- **SendGrid API rate limits**: What happens when SendGrid rate limit is exceeded? System logs the error and queues the email for retry (via Hangfire background job retry mechanism), but does not block the primary operation (sign-up or password reset request).

- **Invalid email addresses**: What happens when a user registers with an invalid or non-existent email? The account is created successfully (email validation happens at format level only), and the welcome email send fails silently. The user can still use the account.

- **Concurrent password reset requests**: What happens when a user requests multiple password resets before using the first link? Each request generates a new token. Only the most recently generated token is valid; older tokens are invalidated automatically by Identity's token provider.

- **Email already registered (social login)**: What happens when a user tries to sign in with Google/Meta using an email that already has a local account? The system links the social provider to the existing account (identified by email) and allows the user to sign in via either method going forward.

- **Email not verified by OAuth provider**: What happens when Google/Meta returns an email that is not verified? The system rejects the social login and prompts the user to either verify their email with the provider or register with a different method.

- **User denies OAuth consent**: What happens when a user cancels the Google/Meta authorization screen? The OAuth flow returns an error callback to the frontend, which displays a user-friendly message and redirects back to the sign-in page without creating an account.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST send a welcome email to newly registered users immediately after successful account creation via POST /api/v1/auth/signup
- **FR-002**: System MUST send a password reset email containing a time-limited reset link when a user requests password recovery via POST /api/v1/auth/forgot-password
- **FR-003**: Welcome emails MUST include the user's username, a personalized welcome message, and a call-to-action button linking to the storefront homepage
- **FR-004**: Password reset emails MUST include a secure reset link in the format `https://{frontend_url}/reset-password?token={resetToken}&email={email}`, a call-to-action button labeled "Reset Password", and a clear expiry notice
- **FR-005**: System MUST use SendGrid as the transactional email delivery service, configured via environment variables for API key, sender email, and sender name
- **FR-006**: Email send failures MUST NOT block the primary operation (sign-up must succeed even if welcome email fails; forgot-password must return success even if email fails to send)
- **FR-007**: System MUST log all email send attempts and failures for monitoring and debugging purposes
- **FR-008**: Email templates MUST align with the Morii Coffee brand identity including brand colors, typography, and tone of voice
- **FR-009**: System MUST validate password reset token expiry and reject expired tokens with a clear error message
- **FR-010**: System MUST handle SendGrid API failures gracefully by logging errors and allowing the application to continue functioning

### Planning Requirements (Social Login - Part 2)

- **FR-P01**: Implementation plan MUST identify all new API endpoints required for OAuth2 social login (e.g., POST /api/v1/auth/social-login or provider-specific endpoints)
- **FR-P02**: Implementation plan MUST document the complete OAuth2 authorization code flow from frontend redirect through provider callback to JWT issuance
- **FR-P03**: Implementation plan MUST specify all changes to the User domain entity including new fields for ExternalProvider, ExternalProviderId, and account linking logic
- **FR-P04**: Implementation plan MUST identify all command handlers, repositories, and infrastructure services required for social login
- **FR-P05**: Implementation plan MUST document frontend changes including social login buttons, OAuth redirect handling, and token storage in Zustand
- **FR-P06**: Implementation plan MUST specify configuration requirements for Google OAuth credentials, Meta App ID/Secret, and callback URLs
- **FR-P07**: Implementation plan MUST document edge case handling strategies for email conflicts, denied consent, and unverified provider emails

### Key Entities

- **EmailMessage**: Represents an outbound transactional email with recipient address, subject line, HTML body, text body (plain text fallback), sender information, and send status (pending, sent, failed).

- **PasswordResetToken**: Represents a time-limited token generated for password reset requests, associated with a user account, containing token value, expiry timestamp, and usage status (unused, used, expired). *(Note: This is handled by ASP.NET Identity's token provider and stored in AspNetUserTokens; not a new domain entity)*

- **User (extended)**: For social login planning, the User entity will be extended with fields to track external authentication providers (e.g., ExternalProvider enum: None, Google, Meta), ExternalProviderId (provider-specific user ID), and account linking metadata.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of welcome emails are delivered within 60 seconds of account creation
- **SC-002**: 98% of password reset emails are delivered within 60 seconds of request submission
- **SC-003**: Email send failures do not cause any user-facing errors during sign-up or password reset flows (operations complete successfully regardless of email status)
- **SC-004**: Password reset links expire after the configured timeout period and reject access attempts with expired tokens 100% of the time
- **SC-005**: All email templates display correctly across major email clients (Gmail, Outlook, Apple Mail) with Morii Coffee branding visible and links functional
- **SC-006**: Social login implementation plan is comprehensive enough that a developer can begin implementation without requiring additional clarification on architecture, flow, or edge cases
- **SC-007**: Email service logs capture sufficient detail (timestamp, recipient, template, status, error messages) to diagnose delivery issues within 5 minutes of occurrence

## Assumptions

- The frontend URL for password reset links is configured via environment variable (e.g., `NEXT_PUBLIC_APP_URL` or similar)
- SendGrid account is already provisioned with API key and verified sender domain
- Password reset token expiry is already configured in the existing auth system (from Phase 2 implementation) and will be reused
- Email HTML templates will be embedded in the codebase (not fetched from external template management system) for simplicity
- Email sending will be synchronous for MVP (fire-and-forget with error logging); asynchronous retry logic via Hangfire can be added in a future iteration if needed
- For social login planning, the existing User entity structure from Phase 2 (documented in PHASE_2_USER_AUTH_PLAN.md) will serve as the baseline for planning extensions
- OAuth2 providers (Google, Meta) return standard claims including email, email_verified, and provider-specific user ID
- Social login planning deliverable is a structured markdown document listing files to modify/create, not executable code

## Out of Scope

- Email open tracking and click analytics
- Customizable email templates via admin UI (templates are code-based for MVP)
- Multi-language email templates (emails will be in English only for MVP; i18n can be added later)
- Email preference center or unsubscribe functionality (transactional emails only, not marketing)
- SMS or push notification alternatives to email
- Actual implementation of social login (Part 2 is planning only)
- Email bounce handling and retry logic beyond basic error logging
- SPF/DKIM/DMARC configuration guidance (assumed already handled by SendGrid)
