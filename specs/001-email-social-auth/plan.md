# Implementation Plan: Email Integration and Social Login Planning

**Branch**: `001-email-social-auth` | **Date**: 2026-03-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-email-social-auth/spec.md`

## Summary

Integrate SendGrid transactional emails for welcome messages on sign-up and password reset links for account recovery. Additionally, produce a comprehensive implementation plan for OAuth2 social login with Google and Meta (Facebook) providers. The email service abstraction and infrastructure already exist from Phase 2; this feature focuses on implementing branded HTML templates, updating command handlers to use the service, and ensuring graceful failure handling. Social login planning includes endpoint design, OAuth2 authorization code flow documentation, domain model extensions, and edge case handling strategies.

**Technical Approach**: Leverage existing `IEmailService` abstraction and `SendGridEmailService` implementation from Phase 2. Create branded HTML email templates embedded in Infrastructure resources. Update `SignUpCommandHandler` and `ForgotPasswordCommandHandler` to call email methods with fire-and-forget pattern. Configure SendGrid settings via appsettings.json. For social login, document a complete implementation plan covering backend endpoints, domain changes, frontend integration, OAuth2 flows, and configuration requirements without writing code.

## Technical Context

**Language/Version**: C# / .NET 8, TypeScript (Next.js 16 frontend)
**Primary Dependencies**: SendGrid SDK (SendGrid NuGet package), ASP.NET Core Identity, MediatR (CQRS), Microsoft.Extensions.Options (configuration)
**Storage**: PostgreSQL via Entity Framework Core (existing from Phase 2)
**Testing**: Manual Swagger/Postman testing for MVP (automated tests out of scope)
**Target Platform**: Linux server (backend API), modern browsers (frontend)
**Project Type**: Web service (backend API) + web application (frontend)
**Performance Goals**: 95% of emails delivered within 60 seconds; graceful degradation on email service failure
**Constraints**: Email send failures MUST NOT block user operations (sign-up, forgot-password); email sending is synchronous (fire-and-forget) for MVP
**Scale/Scope**: Transactional emails only (no marketing); English-only templates for MVP; embedded HTML templates (not CMS-managed)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Plan Mode Default
✅ **PASS** - This is a non-trivial feature (3+ steps: template creation, handler updates, configuration, testing) and entered plan mode as required.

### Principle II: Verification Before Done (NON-NEGOTIABLE)
✅ **PASS** - Verification plan includes: manual testing of welcome email on signup, password reset email on forgot-password request, confirmation that email failures do not block operations, and email content validation across Gmail/Outlook clients.

### Principle III: Simplicity First & Minimal Impact
✅ **PASS** - Leveraging existing `IEmailService` abstraction and `SendGridEmailService` from Phase 2. No new abstractions or layers needed. Minimal changes to command handlers (add email method calls). Embedded HTML templates avoid complexity of external template management.

### Principle IV: Subagent Strategy & Delegation
✅ **PASS** - Used Explore subagent to map Clean Architecture structure and locate existing email service implementation. Keeps main context focused on planning rather than codebase navigation.

### Principle V: Self-Improvement Loop
⚠️ **N/A** - No user corrections yet; lessons.md will be updated if corrections occur during implementation.

### Principle VI: Autonomous Execution with Concise Communication
✅ **PASS** - Plan is actionable without hand-holding. Social login planning delivers a comprehensive document enabling autonomous implementation in future session.

### Tech Stack Constraints Compliance
✅ **PASS** - Uses .NET 8 Clean Architecture (backend), adheres to existing patterns (IEmailService abstraction in Application, implementation in Infrastructure, settings in Domain.Shared, DI registration in Infrastructure.DependencyInjection). Frontend social login planning aligns with Next.js 16 + Zustand + next-intl conventions.

**Gate Result**: ✅ **ALL GATES PASSED** - No constitutional violations. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/001-email-social-auth/
├── plan.md              # This file (speckit.plan command output)
├── spec.md              # Feature specification (completed)
├── research.md          # Phase 0: SendGrid best practices, HTML email design, OAuth2 flows
├── data-model.md        # Phase 1: User entity extensions for social login
├── contracts/           # Phase 1: API endpoint contracts for social login
│   ├── social-login-endpoints.md
│   └── oauth2-callback-flow.md
└── quickstart.md        # Phase 1: Developer quickstart for email testing and social login setup
```

### Source Code (repository root)

The Morii Coffee project uses a **Clean Architecture** structure with separate projects per layer. Email integration work focuses on the backend; social login planning covers both backend and frontend.

```text
source/
├── MoriiCoffee.Domain.Shared/          # Shared cross-cutting concerns
│   ├── Enums/
│   │   └── User/
│   │       ├── EGender.cs              # (Existing from Phase 2)
│   │       ├── EUserStatus.cs          # (Existing from Phase 2)
│   │       └── EExternalProvider.cs    # NEW (social login): None, Google, Meta
│   ├── Constants/
│   │   └── AuthenticationConstants.cs  # NEW: OAuth2 provider names, scopes
│   └── Settings/
│       ├── EmailSettings.cs            # (Existing from Phase 2) - UPDATE: Add ResetPasswordBaseUrl
│       └── OAuth2Settings.cs           # NEW: Google & Meta OAuth config
│
├── MoriiCoffee.Domain/                 # Domain layer
│   ├── Aggregates/
│   │   └── UserAggregate/
│       │   └── User.cs                 # UPDATE (social login): Add ExternalProvider, ExternalProviderId fields
│       │   └── UserDomainMethods.cs    # UPDATE: Add LinkExternalProvider() domain method
│   └── Repositories/
│       └── IUsersRepository.cs         # UPDATE: Add GetByExternalProviderAsync()
│
├── MoriiCoffee.Application/            # Application layer (CQRS)
│   ├── Commands/
│   │   ├── Auth/
│   │   │   ├── SignUp/
│   │   │   │   └── SignUpCommandHandler.cs     # UPDATE: Call IEmailService.SendWelcomeEmailAsync()
│   │   │   ├── ForgotPassword/
│   │   │   │   └── ForgotPasswordCommandHandler.cs  # UPDATE: Call IEmailService.SendPasswordResetEmailAsync()
│   │   │   └── SocialLogin/                     # NEW (planned)
│   │   │       ├── SocialLoginCommand.cs
│   │   │       ├── SocialLoginCommandHandler.cs
│   │   │       └── SocialLoginCommandValidator.cs
│   └── SeedWork/
│       ├── Abstractions/
│       │   ├── IEmailService.cs                # (Existing from Phase 2)
│       │   └── IOAuth2Service.cs               # NEW (planned): OAuth2 token exchange
│       └── DTOs/
│           └── Auth/
│               └── SocialLoginDto.cs           # NEW (planned): Provider, code, redirectUri
│
├── MoriiCoffee.Infrastructure/         # Infrastructure layer
│   ├── Services/
│   │   ├── Email/
│   │   │   ├── SendGridEmailService.cs         # (Existing from Phase 2) - UPDATE: Implement new methods
│   │   │   └── EmailTemplates.cs               # (Existing from Phase 2) - UPDATE: Add WelcomeEmail(), PasswordResetEmail()
│   │   └── OAuth2/                             # NEW (planned)
│   │       ├── GoogleOAuth2Service.cs
│   │       └── MetaOAuth2Service.cs
│   ├── Resources/
│   │   └── EmailTemplates/
│   │       ├── welcome.html                    # NEW: Branded welcome email template
│   │       └── password-reset.html             # NEW: Branded password reset email template
│   └── Configurations/
│       └── SettingsConfiguration.cs            # UPDATE: Bind OAuth2Settings
│
├── MoriiCoffee.Infrastructure.Persistence/     # Data access layer
│   ├── Configurations/
│   │   └── UserConfiguration.cs                # UPDATE: Add ExternalProvider, ExternalProviderId columns
│   └── Migrations/
│       └── YYYYMMDD_AddSocialLoginFields.cs    # NEW (planned): Migration for User table
│
└── MoriiCoffee.Presentation/           # Presentation layer (HTTP)
    └── Controllers/
        └── AuthController.cs                   # UPDATE (planned): Add POST /social-login endpoint

frontend/ (Next.js)
└── src/
    ├── components/
    │   └── auth/
    │       ├── SocialLoginButtons.tsx          # NEW (planned): Google/Meta login buttons
    │       └── OAuth2CallbackHandler.tsx       # NEW (planned): Handle OAuth2 redirect
    ├── stores/
    │   └── authStore.ts                        # UPDATE (planned): Add social login actions
    └── app/
        └── auth/
            └── callback/
                └── page.tsx                    # NEW (planned): OAuth2 callback page
```

**Structure Decision**: The project uses Clean Architecture with 6 layers (Domain.Shared → Domain → Application → Infrastructure → Infrastructure.Persistence → Presentation). Email integration work is confined to Application (command handlers), Infrastructure (templates, SendGrid service), and configuration. Social login planning (Part 2) spans all layers plus frontend. Existing patterns from Phase 2 are reused (IEmailService, SettingsConfiguration, DI registration in Infrastructure.DependencyInjection).

## Complexity Tracking

> **No complexity violations** - all gates passed. This table is empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *(none)* | *(none)* | *(none)* |

---

## Phase 0: Research & Best Practices

### Research Tasks

1. **SendGrid Email Best Practices**
   - Investigate SendGrid's recommended patterns for transactional emails
   - Research HTML email template design best practices (responsive, plain-text fallback, accessibility)
   - Determine if SendGrid SDK requires any specific error handling patterns
   - Document SendGrid rate limits and how to handle throttling

2. **HTML Email Template Design**
   - Research email client compatibility (Gmail, Outlook, Apple Mail) for HTML/CSS
   - Identify safe CSS properties for email (many clients strip styles)
   - Determine how to embed Morii Coffee brand colors (oklch color space may not be supported; need hex fallback)
   - Investigate plain-text fallback generation strategies

3. **OAuth2 Authorization Code Flow**
   - Document Google OAuth2 setup process (create OAuth2 client ID, configure redirect URIs)
   - Document Meta (Facebook) OAuth2 setup process (create Facebook App, configure OAuth redirect URIs)
   - Research OAuth2 authorization code flow in detail (authorization URL generation, state parameter for CSRF, code exchange for access token, token validation)
   - Identify required scopes for Google (email, profile) and Meta (email, public_profile)
   - Research edge cases: unverified emails, denied permissions, account linking

4. **Email-Based Account Linking**
   - Research strategies for linking social accounts to existing email-based accounts
   - Determine if automatic linking (by email match) is secure or if user confirmation is needed
   - Investigate how to handle conflicts (user signs up with email/password, then tries Google with same email)

**Output**: `research.md` with consolidated findings, decision rationale, and alternatives considered.

---

## Phase 1: Design & Contracts

### Phase 1a: Data Model

**File**: `data-model.md`

#### Entities

**User (Extended for Social Login)**

The existing `User` aggregate from Phase 2 will be extended with fields to support external authentication providers.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `ExternalProvider` | `EExternalProvider` (enum) | OAuth2 provider used for account creation/login (None, Google, Meta) | Default: None |
| `ExternalProviderId` | `string?` | Provider-specific user ID (e.g., Google sub claim, Meta user ID) | Max 500 chars; nullable |
| `ExternalEmail` | `string?` | Email returned by OAuth2 provider (for audit/debugging) | Max 320 chars; nullable |
| `ExternalEmailVerified` | `bool` | Whether OAuth2 provider verified the email | Default: false |

**Domain Methods**:
- `LinkExternalProvider(EExternalProvider provider, string providerId, string email, bool emailVerified)`: Links a social account to this user
- `UnlinkExternalProvider()`: Removes external provider linkage (revert to local account)

**Relationships**:
- No new relationships; User remains an aggregate root

**State Transitions**:
- Local account (no external provider) → Linked account (ExternalProvider set after social login)
- Linked account → Local account (if user unlinks social provider)

#### EmailMessage (Not a Domain Entity)

`EmailMessage` is conceptual only (not persisted). The email service creates and sends emails ephemerally. For observability, email sends are logged via Serilog with structured properties (recipient, template, status, error).

---

### Phase 1b: Interface Contracts (Social Login - Planned)

**File**: `contracts/social-login-endpoints.md`

#### POST /api/v1/auth/social-login

**Request**:
```json
{
  "provider": "google" | "meta",
  "code": "authorization_code_from_oauth2_provider",
  "redirectUri": "https://moriicoffee.com/auth/callback",
  "state": "csrf_token_from_client"
}
```

**Response (Success - 200 OK)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "opaque_refresh_token",
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "userName": "user123",
    "fullName": "John Doe",
    "externalProvider": "google",
    "roles": ["CUSTOMER"]
  }
}
```

**Response (Error - 400 Bad Request)**:
```json
{
  "error": "INVALID_AUTHORIZATION_CODE",
  "message": "The authorization code is invalid or expired."
}
```

**Response (Error - 409 Conflict)**:
```json
{
  "error": "EMAIL_ALREADY_REGISTERED_LOCAL",
  "message": "This email is already registered with a local account. Please sign in with your password or reset it."
}
```

**Behavior**:
1. Validate `code`, `provider`, `redirectUri`
2. Exchange authorization code for access token with OAuth2 provider (Google/Meta API)
3. Fetch user profile from provider using access token
4. Check if `email_verified` is true; reject if false
5. Check if user exists by email:
   - If exists with `ExternalProvider == None`: link external provider to existing account
   - If exists with same `ExternalProvider`: update profile and issue JWT
   - If exists with different `ExternalProvider`: error (email conflict)
   - If not exists: create new user with `ExternalProvider` and `ExternalProviderId` set
6. Generate JWT access token + refresh token
7. Return auth response

---

#### GET /api/v1/auth/social-login/{provider}/authorization-url

**Request Parameters**:
- `provider`: `google` | `meta` (path parameter)
- `redirectUri`: `https://moriicoffee.com/auth/callback` (query parameter)
- `state`: Client-generated CSRF token (query parameter)

**Response (Success - 200 OK)**:
```json
{
  "authorizationUrl": "https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=...&scope=email+profile&state=..."
}
```

**Behavior**:
1. Construct OAuth2 authorization URL for the specified provider
2. Include required scopes (Google: `email profile openid`; Meta: `email public_profile`)
3. Include `state` parameter for CSRF protection
4. Return authorization URL for frontend to redirect user

---

**File**: `contracts/oauth2-callback-flow.md`

#### OAuth2 Authorization Code Flow (End-to-End)

```
┌─────────┐                                  ┌──────────────┐                          ┌─────────────────┐
│ Browser │                                  │ Morii Coffee │                          │ OAuth2 Provider │
│ (User)  │                                  │   Backend    │                          │ (Google / Meta) │
└────┬────┘                                  └──────┬───────┘                          └────────┬────────┘
     │                                              │                                           │
     │ 1. Click "Sign in with Google"              │                                           │
     │─────────────────────────────────────────────>│                                           │
     │                                              │                                           │
     │ 2. GET /api/v1/auth/social-login/google/     │                                           │
     │    authorization-url?redirectUri=...&state=..│                                           │
     │<─────────────────────────────────────────────│                                           │
     │                                              │                                           │
     │ 3. { "authorizationUrl": "https://accounts.  │                                           │
     │    google.com/o/oauth2/v2/auth?..." }        │                                           │
     │                                              │                                           │
     │ 4. Redirect to authorization URL             │                                           │
     │───────────────────────────────────────────────────────────────────────────────────────────>│
     │                                              │                                           │
     │ 5. User signs in, grants permissions         │                                           │
     │                                              │                                           │
     │ 6. Redirect to callback URL with code        │                                           │
     │    https://moriicoffee.com/auth/callback?    │                                           │
     │    code=AUTH_CODE&state=CSRF_TOKEN           │                                           │
     │<───────────────────────────────────────────────────────────────────────────────────────────│
     │                                              │                                           │
     │ 7. POST /api/v1/auth/social-login            │                                           │
     │    { "provider": "google", "code": "...", ...}│                                          │
     │─────────────────────────────────────────────>│                                           │
     │                                              │                                           │
     │                                              │ 8. Exchange code for access token         │
     │                                              │ POST https://oauth2.googleapis.com/token  │
     │                                              │───────────────────────────────────────────>│
     │                                              │                                           │
     │                                              │ 9. { "access_token": "...", "id_token":"."}│
     │                                              │<───────────────────────────────────────────│
     │                                              │                                           │
     │                                              │ 10. Verify ID token (Google JWT)          │
     │                                              │     Extract email, sub, email_verified     │
     │                                              │                                           │
     │                                              │ 11. Check if user exists by email         │
     │                                              │     - Create or link account               │
     │                                              │     - Generate Morii Coffee JWT            │
     │                                              │                                           │
     │ 12. { "accessToken": "...", "refreshToken":  │                                           │
     │      "...", "user": { ... } }                │                                           │
     │<─────────────────────────────────────────────│                                           │
     │                                              │                                           │
     │ 13. Store tokens in Zustand authStore        │                                           │
     │     Redirect to homepage                     │                                           │
     │                                              │                                           │
```

**Key Points**:
- `state` parameter prevents CSRF attacks; frontend generates random string, backend echoes it back
- Authorization code is single-use and expires quickly (typically 10 minutes)
- Backend exchanges code for access token server-to-server (never exposes access token to browser during OAuth flow)
- For Google, the response includes an `id_token` (JWT) which contains user claims (email, sub, email_verified)
- For Meta, backend must call `https://graph.facebook.com/me?fields=id,email` with access token to get user profile

---

### Phase 1c: Quickstart Guide

**File**: `quickstart.md`

#### Email Testing Quickstart

**Prerequisites**:
- SendGrid account with verified sender email
- SendGrid API key (generate from SendGrid dashboard)

**Configuration**:

1. Update `appsettings.Development.json`:
```json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "SendGrid": {
      "ApiKey": "SG.xxxx..."
    }
  }
}
```

2. Set environment variable (alternative to appsettings):
```bash
export EmailSettings__SendGrid__ApiKey="SG.xxxx..."
```

**Testing Welcome Email**:

1. Start backend: `dotnet run --project source/MoriiCoffee.Presentation`
2. Navigate to Swagger: `http://localhost:5000/swagger`
3. Call `POST /api/v1/auth/signup` with:
```json
{
  "email": "your-test-email@gmail.com",
  "userName": "testuser",
  "password": "Test@1234",
  "confirmPassword": "Test@1234"
}
```
4. Check your inbox for welcome email (within 60 seconds)
5. Verify email contains: username, welcome message, "Shop Now" button linking to storefront

**Testing Password Reset Email**:

1. Call `POST /api/v1/auth/forgot-password` with:
```json
{
  "email": "your-test-email@gmail.com"
}
```
2. Check your inbox for password reset email (within 60 seconds)
3. Click "Reset Password" button; verify it redirects to `http://localhost:3000/reset-password?token=...&email=...`
4. Submit new password on frontend reset page

**Testing Email Failure Handling**:

1. Set invalid API key in appsettings: `"ApiKey": "INVALID_KEY"`
2. Call `POST /api/v1/auth/signup`
3. Verify signup still succeeds (returns 200 OK with JWT tokens)
4. Check backend logs; confirm email send failure is logged

---

#### Social Login Setup Quickstart (Planned)

**Google OAuth2 Setup**:

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project: "Morii Coffee"
3. Enable Google+ API
4. Create OAuth2 credentials:
   - Application type: Web application
   - Authorized redirect URIs: `http://localhost:3000/auth/callback`, `https://moriicoffee.com/auth/callback`
5. Copy Client ID and Client Secret

**Meta (Facebook) OAuth2 Setup**:

1. Go to [Meta for Developers](https://developers.facebook.com/)
2. Create new app: "Morii Coffee"
3. Add Facebook Login product
4. Configure OAuth redirect URIs: `http://localhost:3000/auth/callback`, `https://moriicoffee.com/auth/callback`
5. Copy App ID and App Secret

**Configuration**:

Add to `appsettings.Development.json`:
```json
{
  "OAuth2Settings": {
    "Google": {
      "ClientId": "xxxx.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-xxxxx",
      "RedirectUri": "http://localhost:3000/auth/callback"
    },
    "Meta": {
      "AppId": "1234567890",
      "AppSecret": "xxxxx",
      "RedirectUri": "http://localhost:3000/auth/callback"
    }
  }
}
```

**Testing Social Login** (after implementation):

1. Start frontend: `pnpm dev`
2. Navigate to sign-in page: `http://localhost:3000/signin`
3. Click "Sign in with Google"
4. Authorize app in Google consent screen
5. Verify redirect to callback page
6. Verify JWT token stored in Zustand authStore
7. Verify user profile fetched from backend

---

### Phase 1d: Agent Context Update

Run the agent context update script to inform future sessions about the email integration and social login work:

```bash
.specify/scripts/bash/update-agent-context.sh claude
```

This will update `CLAUDE.md` with:
- SendGrid email service configuration
- OAuth2 social login architecture
- New User entity fields (ExternalProvider, ExternalProviderId)
- Social login endpoints and OAuth2 flow

---

## Phase 2: Social Login Implementation Plan (Part 2 Deliverable)

**Note**: This is a planning-only deliverable. No code is written in this phase. The plan below documents all files to create/modify for a future implementation session.

### Summary

OAuth2 social login allows users to sign in with Google or Meta (Facebook) accounts, eliminating the need to remember another password and reducing registration friction. The implementation requires:

1. **Backend changes**: New endpoints, OAuth2 service abstractions, command handlers, User entity extensions
2. **Frontend changes**: Social login buttons, OAuth2 redirect handling, callback page, Zustand store updates
3. **Configuration**: Google OAuth2 credentials, Meta App ID/Secret, redirect URIs
4. **Database migration**: Add `ExternalProvider`, `ExternalProviderId` columns to User table

### Implementation Checklist

#### Domain Layer Changes

**New Files**:
- `source/MoriiCoffee.Domain.Shared/Enums/User/EExternalProvider.cs`
  - Enum: `None`, `Google`, `Meta`

- `source/MoriiCoffee.Domain.Shared/Constants/AuthenticationConstants.cs`
  - `GOOGLE_AUTHORIZATION_URL = "https://accounts.google.com/o/oauth2/v2/auth"`
  - `GOOGLE_TOKEN_URL = "https://oauth2.googleapis.com/token"`
  - `GOOGLE_USERINFO_URL = "https://www.googleapis.com/oauth2/v3/userinfo"`
  - `GOOGLE_SCOPES = "email profile openid"`
  - `META_AUTHORIZATION_URL = "https://www.facebook.com/v18.0/dialog/oauth"`
  - `META_TOKEN_URL = "https://graph.facebook.com/v18.0/oauth/access_token"`
  - `META_USERINFO_URL = "https://graph.facebook.com/v18.0/me?fields=id,email,name"`
  - `META_SCOPES = "email public_profile"`

- `source/MoriiCoffee.Domain.Shared/Settings/OAuth2Settings.cs`
  - Root class with nested `GoogleOptions` and `MetaOptions`
  - Properties: `ClientId`, `ClientSecret`, `RedirectUri`

**Modified Files**:
- `source/MoriiCoffee.Domain/Aggregates/UserAggregate/User.cs`
  - Add fields: `EExternalProvider ExternalProvider`, `string? ExternalProviderId`, `string? ExternalEmail`, `bool ExternalEmailVerified`
  - Add domain method: `void LinkExternalProvider(EExternalProvider provider, string providerId, string email, bool emailVerified)`
  - Add domain method: `void UnlinkExternalProvider()`

- `source/MoriiCoffee.Domain/Repositories/IUsersRepository.cs`
  - Add method: `Task<User?> GetByExternalProviderAsync(EExternalProvider provider, string providerId)`
  - Add method: `Task<User?> GetByEmailAsync(string email)` (if not already present)

#### Application Layer Changes

**New Files**:
- `source/MoriiCoffee.Application/SeedWork/Abstractions/IOAuth2Service.cs`
  - Interface with methods:
    - `Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri, string state)`
    - `Task<OAuth2TokenResponse> ExchangeCodeForTokenAsync(string provider, string code, string redirectUri)`
    - `Task<OAuth2UserProfile> GetUserProfileAsync(string provider, string accessToken)`

- `source/MoriiCoffee.Application/SeedWork/DTOs/Auth/SocialLoginDto.cs`
  - Properties: `string Provider`, `string Code`, `string RedirectUri`, `string State`

- `source/MoriiCoffee.Application/SeedWork/DTOs/Auth/OAuth2TokenResponse.cs`
  - Properties: `string AccessToken`, `string? IdToken`, `int ExpiresIn`, `string? RefreshToken`

- `source/MoriiCoffee.Application/SeedWork/DTOs/Auth/OAuth2UserProfile.cs`
  - Properties: `string ProviderId`, `string Email`, `bool EmailVerified`, `string? Name`

- `source/MoriiCoffee.Application/Commands/Auth/SocialLogin/SocialLoginCommand.cs`
  - Inherits from `ICommand<AuthResponseDto>`
  - Properties: `string Provider`, `string Code`, `string RedirectUri`, `string State`

- `source/MoriiCoffee.Application/Commands/Auth/SocialLogin/SocialLoginCommandHandler.cs`
  - Dependencies: `IOAuth2Service`, `UserManager<User>`, `ITokenService`, `IUnitOfWork`
  - Logic:
    1. Validate `Provider` (must be "google" or "meta")
    2. Exchange authorization code for access token
    3. Fetch user profile from OAuth2 provider
    4. Verify `EmailVerified` is true; reject if false
    5. Check if user exists by email:
       - If exists with no external provider: call `user.LinkExternalProvider()` and update
       - If exists with same external provider: update profile
       - If exists with different external provider: throw `ConflictException`
       - If not exists: create new user with external provider fields set
    6. Generate JWT access token + refresh token
    7. Return `AuthResponseDto`

- `source/MoriiCoffee.Application/Commands/Auth/SocialLogin/SocialLoginCommandValidator.cs`
  - Validate `Provider` is not empty and is one of ["google", "meta"]
  - Validate `Code` is not empty
  - Validate `RedirectUri` is valid URI
  - Validate `State` is not empty (CSRF token)

- `source/MoriiCoffee.Application/Queries/Auth/GetOAuth2AuthorizationUrl/GetOAuth2AuthorizationUrlQuery.cs`
  - Inherits from `IQuery<string>`
  - Properties: `string Provider`, `string RedirectUri`, `string State`

- `source/MoriiCoffee.Application/Queries/Auth/GetOAuth2AuthorizationUrl/GetOAuth2AuthorizationUrlQueryHandler.cs`
  - Dependencies: `IOAuth2Service`
  - Logic: Call `_oauth2Service.GenerateAuthorizationUrlAsync()` and return URL

**Modified Files**:
- `source/MoriiCoffee.Application/SeedWork/Mappings/UserMapper.cs`
  - Add mapping: `User.ExternalProvider` → `UserDto.ExternalProvider`
  - Add mapping: `SocialLoginDto` → `SocialLoginCommand`

#### Infrastructure Layer Changes

**New Files**:
- `source/MoriiCoffee.Infrastructure/Services/OAuth2/IOAuth2Service.cs` (moved from Application)
  - Implementation interface lives in Infrastructure

- `source/MoriiCoffee.Infrastructure/Services/OAuth2/GoogleOAuth2Service.cs`
  - Implements `IOAuth2Service`
  - Methods:
    - `GenerateAuthorizationUrlAsync()`: Constructs Google OAuth2 authorization URL with scopes, state, redirect_uri
    - `ExchangeCodeForTokenAsync()`: Calls Google token endpoint, returns access token + id_token
    - `GetUserProfileAsync()`: Parses Google ID token (JWT), extracts email, sub, email_verified claims
  - Dependencies: `HttpClient`, `OAuth2Settings.Google`, `ILogger`

- `source/MoriiCoffee.Infrastructure/Services/OAuth2/MetaOAuth2Service.cs`
  - Implements `IOAuth2Service`
  - Methods:
    - `GenerateAuthorizationUrlAsync()`: Constructs Meta OAuth2 authorization URL
    - `ExchangeCodeForTokenAsync()`: Calls Meta token endpoint, returns access token
    - `GetUserProfileAsync()`: Calls `https://graph.facebook.com/me?fields=id,email,name` with access token
  - Dependencies: `HttpClient`, `OAuth2Settings.Meta`, `ILogger`

- `source/MoriiCoffee.Infrastructure/Services/OAuth2/OAuth2ServiceFactory.cs`
  - Factory to resolve provider-specific service (Google or Meta) based on provider string

**Modified Files**:
- `source/MoriiCoffee.Infrastructure/Configurations/SettingsConfiguration.cs`
  - Bind `OAuth2Settings` from appsettings: `services.AddSingleton(config.GetSection("OAuth2Settings").Get<OAuth2Settings>())`

- `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`
  - Register `GoogleOAuth2Service` as scoped
  - Register `MetaOAuth2Service` as scoped
  - Register `IOAuth2Service` factory: resolve based on provider at runtime

#### Infrastructure.Persistence Layer Changes

**New Files**:
- `source/MoriiCoffee.Infrastructure.Persistence/Migrations/YYYYMMDD_AddSocialLoginFields.cs`
  - Add columns to `AspNetUsers` table:
    - `ExternalProvider` (int, nullable, default NULL)
    - `ExternalProviderId` (nvarchar(500), nullable)
    - `ExternalEmail` (nvarchar(320), nullable)
    - `ExternalEmailVerified` (bit, not null, default 0)
  - Add index on `ExternalProvider` + `ExternalProviderId` for lookups

**Modified Files**:
- `source/MoriiCoffee.Infrastructure.Persistence/Configurations/UserConfiguration.cs`
  - Add column mapping: `builder.Property(u => u.ExternalProvider).HasConversion<int>()`
  - Add column mapping: `builder.Property(u => u.ExternalProviderId).HasMaxLength(500)`
  - Add column mapping: `builder.Property(u => u.ExternalEmail).HasMaxLength(320)`
  - Add index: `builder.HasIndex(u => new { u.ExternalProvider, u.ExternalProviderId })`

- `source/MoriiCoffee.Infrastructure.Persistence/Repositories/UsersRepository.cs`
  - Implement `GetByExternalProviderAsync()`: Query by `ExternalProvider` and `ExternalProviderId`

#### Presentation Layer Changes

**New Files**:
- None (AuthController is modified)

**Modified Files**:
- `source/MoriiCoffee.Presentation/Controllers/AuthController.cs`
  - Add endpoint: `POST /api/v1/auth/social-login`
    - Accepts `SocialLoginDto`
    - Maps to `SocialLoginCommand`
    - Sends via `IMediator`
    - Returns `AuthResponseDto`
  - Add endpoint: `GET /api/v1/auth/social-login/{provider}/authorization-url`
    - Accepts query params: `redirectUri`, `state`
    - Sends `GetOAuth2AuthorizationUrlQuery` via `IMediator`
    - Returns `{ "authorizationUrl": "..." }`

#### Frontend Changes (Next.js)

**New Files**:
- `frontend/src/components/auth/SocialLoginButtons.tsx`
  - "use client" directive
  - Renders Google and Meta login buttons
  - On click: calls backend to get authorization URL, then redirects to OAuth2 provider
  - Uses `authStore` actions

- `frontend/src/components/auth/OAuth2CallbackHandler.tsx`
  - "use client" directive
  - Extracts `code` and `state` from URL query params
  - Calls backend `POST /api/v1/auth/social-login` with authorization code
  - Stores returned JWT tokens in Zustand `authStore`
  - Redirects to homepage on success

- `frontend/src/app/[locale]/auth/callback/page.tsx`
  - Server component that renders `OAuth2CallbackHandler`
  - Path: `/auth/callback` (matches OAuth2 redirect URI)

**Modified Files**:
- `frontend/src/stores/authStore.ts`
  - Add action: `socialLogin(provider: string)` - fetches authorization URL and redirects
  - Add action: `handleOAuth2Callback(code: string, state: string)` - exchanges code for tokens
  - Update `login` action to support both email/password and social login flows

- `frontend/src/app/[locale]/signin/page.tsx`
  - Import `SocialLoginButtons` component
  - Render social login buttons below email/password form
  - Add divider: "Or continue with"

- `frontend/src/app/[locale]/signup/page.tsx`
  - Import `SocialLoginButtons` component
  - Render social login buttons below sign-up form

#### Configuration Files

**Modified Files**:
- `appsettings.json` and `appsettings.Development.json`
  - Add section:
```json
{
  "OAuth2Settings": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
      "RedirectUri": "http://localhost:3000/auth/callback"
    },
    "Meta": {
      "AppId": "YOUR_META_APP_ID",
      "AppSecret": "YOUR_META_APP_SECRET",
      "RedirectUri": "http://localhost:3000/auth/callback"
    }
  }
}
```

#### Edge Case Handling

**Email Already Registered with Local Account**:
- **Strategy**: Automatically link external provider to existing account (identified by email)
- **Implementation**: In `SocialLoginCommandHandler`, if user exists with `ExternalProvider == None`, call `user.LinkExternalProvider()` and update
- **User Experience**: User can sign in via either email/password or social login after linking

**Email Already Registered with Different Provider**:
- **Strategy**: Reject with 409 Conflict error
- **Implementation**: In `SocialLoginCommandHandler`, if user exists with different `ExternalProvider`, throw `ConflictException("This email is already linked to another social provider")`
- **User Experience**: Show error message: "This email is already registered with Google. Please use Google to sign in."

**Email Not Verified by OAuth2 Provider**:
- **Strategy**: Reject social login with 400 Bad Request
- **Implementation**: In `SocialLoginCommandHandler`, check `userProfile.EmailVerified`; if false, throw `BadRequestException("Email not verified by provider")`
- **User Experience**: Show error message: "Please verify your email with [Provider] before using social login."

**User Denies OAuth Consent**:
- **Strategy**: OAuth provider redirects with `error=access_denied` query param; frontend shows friendly message
- **Implementation**: In `OAuth2CallbackHandler`, check for `error` query param; if present, redirect to sign-in page with error toast
- **User Experience**: Show error message: "You cancelled the sign-in process. Please try again."

**OAuth2 Provider Service Downtime**:
- **Strategy**: Backend catches `HttpRequestException` from OAuth2 service, logs error, returns 503 Service Unavailable
- **Implementation**: In `SocialLoginCommandHandler`, wrap `_oauth2Service` calls in try-catch
- **User Experience**: Show error message: "Social login is temporarily unavailable. Please try again later or use email/password."

**State Parameter Mismatch (CSRF Attack)**:
- **Strategy**: Backend validates `state` parameter matches the one stored in session/cookie
- **Implementation**: Frontend generates random `state` before redirect, stores in session storage; backend echoes it back; frontend validates match
- **User Experience**: If mismatch, show error: "Security validation failed. Please try again."

---

## Implementation Order

### Part 1: Email Integration (Immediate Implementation)

**Step 1: Email Templates**
1. Create `welcome.html` template in `source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/`
   - Branded with Morii Coffee colors (use hex fallback for oklch)
   - Includes: username placeholder `{{NAME}}`, storefront link `{{STOREFRONT_URL}}`, "Shop Now" CTA button
   - Responsive design for mobile and desktop
   - Plain-text fallback

2. Create `password-reset.html` template in same directory
   - Includes: reset link `{{RESET_URL}}`, "Reset Password" CTA button, expiry notice
   - Branded with Morii Coffee colors
   - Responsive design
   - Plain-text fallback

**Step 2: Update EmailTemplates Helper**
1. Modify `source/MoriiCoffee.Infrastructure/Services/Email/EmailTemplates.cs`
   - Add method: `string WelcomeEmail(string userName, string storefrontUrl)`
   - Add method: `string PasswordResetEmail(string resetUrl)`
   - Load HTML templates from embedded resources
   - Replace placeholders with actual values
   - HTML-encode user inputs to prevent XSS

**Step 3: Update SendGridEmailService**
1. Modify `source/MoriiCoffee.Infrastructure/Services/Email/SendGridEmailService.cs`
   - Implement `SendWelcomeEmailAsync(string to, string name)`
     - Call `EmailTemplates.WelcomeEmail()` to get HTML body
     - Create SendGrid message with HTML body + plain-text fallback
     - Send via SendGrid SDK
     - Log success/failure with structured properties (recipient, template, status, error)
     - Catch exceptions and log; do not throw (graceful degradation)
   - Implement `SendPasswordResetEmailAsync(string to, string token)`
     - Construct reset URL: `{EmailSettings.ResetPasswordBaseUrl}?token={token}&email={to}`
     - Call `EmailTemplates.PasswordResetEmail()` to get HTML body
     - Create SendGrid message with HTML body + plain-text fallback
     - Send via SendGrid SDK
     - Log success/failure
     - Catch exceptions and log; do not throw

**Step 4: Update EmailSettings**
1. Modify `source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs`
   - Add property: `string ResetPasswordBaseUrl` (e.g., "http://localhost:3000/reset-password")

**Step 5: Update Command Handlers**
1. Modify `source/MoriiCoffee.Application/Commands/Auth/SignUp/SignUpCommandHandler.cs`
   - After user creation and JWT generation, call:
     ```csharp
     _ = _emailService.SendWelcomeEmailAsync(user.Email!, user.UserName!);
     ```
   - Fire-and-forget pattern (discard result)
   - Do not await; do not catch exceptions (service handles gracefully)

2. Modify `source/MoriiCoffee.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs`
   - After password reset token generation, call:
     ```csharp
     _ = _emailService.SendPasswordResetEmailAsync(user.Email!, token);
     ```
   - Fire-and-forget pattern (discard result)

**Step 6: Update Configuration**
1. Update `appsettings.Development.json`:
   ```json
   {
     "EmailSettings": {
       "Provider": "SendGrid",
       "FromEmail": "no-reply@moriicoffee.com",
       "FromName": "Morii Coffee",
       "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
       "SendGrid": {
         "ApiKey": "YOUR_SENDGRID_API_KEY"
       }
     }
   }
   ```

**Step 7: Manual Testing**
1. Start backend: `dotnet run --project source/MoriiCoffee.Presentation`
2. Test welcome email: Call `POST /api/v1/auth/signup` via Swagger
3. Test password reset email: Call `POST /api/v1/auth/forgot-password` via Swagger
4. Verify emails arrive in inbox (within 60 seconds)
5. Test email failure: Set invalid API key, verify signup still succeeds

---

### Part 2: Social Login Planning (Documentation Only)

**Deliverable**: This section (Phase 2) serves as the comprehensive implementation plan for social login. No code is written. The plan documents:

- All files to create (23 new files across Domain, Application, Infrastructure, Persistence, Presentation, Frontend)
- All files to modify (11 modified files)
- OAuth2 authorization code flow (end-to-end diagram)
- API endpoint contracts (request/response schemas)
- Edge case handling strategies
- Configuration requirements
- Database migration details
- Frontend component structure
- Testing quickstart guide

**Next Steps** (for future implementation session):
1. Follow the implementation checklist above in order
2. Start with Domain layer (enums, settings, User entity extensions)
3. Move to Application layer (commands, DTOs, validators)
4. Implement Infrastructure layer (OAuth2 services)
5. Add Persistence layer changes (migration, repository methods)
6. Update Presentation layer (AuthController endpoints)
7. Implement Frontend changes (SocialLoginButtons, callback page, Zustand actions)
8. Test OAuth2 flow end-to-end with Google and Meta

---

## Verification Plan

### Email Integration Verification

**Test 1: Welcome Email on Sign-Up**
1. Call `POST /api/v1/auth/signup` with valid email and password
2. Verify account created successfully (returns 200 OK with JWT tokens)
3. Verify welcome email received within 60 seconds
4. Open email, verify:
   - Username displayed correctly
   - Welcome message in Morii Coffee brand tone
   - "Shop Now" button present and links to storefront
   - Email displays correctly in Gmail, Outlook, Apple Mail
5. Verify no user-facing errors during signup (even if email fails)

**Test 2: Password Reset Email**
1. Call `POST /api/v1/auth/forgot-password` with registered email
2. Verify request returns 200 OK (always returns success)
3. Verify password reset email received within 60 seconds
4. Open email, verify:
   - "Reset Password" button present
   - Reset URL format: `https://{frontend_url}/reset-password?token={token}&email={email}`
   - Expiry notice displayed
   - Email displays correctly in Gmail, Outlook, Apple Mail
5. Click reset link, verify frontend reset page opens with token and email pre-filled

**Test 3: Email Failure Does Not Block Operations**
1. Set invalid SendGrid API key in configuration
2. Call `POST /api/v1/auth/signup`
3. Verify signup succeeds (returns 200 OK with JWT tokens)
4. Verify user can sign in immediately
5. Check backend logs, confirm email send failure logged with error details

**Test 4: Password Reset Token Expiry**
1. Generate password reset token
2. Wait for token to expire (based on configured expiry time)
3. Attempt to use expired token
4. Verify frontend shows error: "This reset link has expired. Please request a new one."

**Test 5: Concurrent Password Reset Requests**
1. Request password reset for user (generates token A)
2. Request password reset again for same user (generates token B)
3. Attempt to use token A
4. Verify token A is invalid (only token B works)

### Social Login Verification (Post-Implementation)

**Test 6: Google OAuth2 Sign-In**
1. Click "Sign in with Google" button on sign-in page
2. Verify redirect to Google authorization page
3. Authorize app in Google consent screen
4. Verify redirect to callback page with authorization code
5. Verify JWT tokens stored in Zustand authStore
6. Verify user profile fetched and displayed
7. Verify user can access protected routes

**Test 7: Meta OAuth2 Sign-In**
1. Click "Sign in with Meta" button on sign-in page
2. Verify redirect to Meta authorization page
3. Authorize app in Meta consent screen
4. Verify redirect to callback page with authorization code
5. Verify JWT tokens stored in Zustand authStore
6. Verify user profile fetched and displayed

**Test 8: Email Already Registered (Account Linking)**
1. Create local account with email: `user@example.com`
2. Sign in with Google using same email
3. Verify external provider linked to existing account
4. Verify user can sign in via email/password OR Google

**Test 9: Email Conflict (Different Provider)**
1. Create account with Google (email: `user@example.com`)
2. Attempt to sign in with Meta using same email
3. Verify 409 Conflict error returned
4. Verify error message: "This email is already linked to Google. Please use Google to sign in."

**Test 10: Email Not Verified by Provider**
1. Use test Google account with unverified email
2. Attempt social login
3. Verify 400 Bad Request error returned
4. Verify error message: "Please verify your email with Google before using social login."

**Test 11: User Denies OAuth Consent**
1. Click "Sign in with Google"
2. Cancel authorization on Google consent screen
3. Verify redirect to sign-in page with error message: "You cancelled the sign-in process."

---

## Dependencies & NuGet Packages

### Email Integration

**Already Installed (Phase 2)**:
- `SendGrid` (NuGet) - SendGrid SDK for .NET
- `Serilog` - Structured logging

**No Additional Packages Needed**

### Social Login (Planned)

**To Be Installed**:
- `System.IdentityModel.Tokens.Jwt` (NuGet) - For parsing Google ID tokens (JWT validation)
- `Microsoft.Extensions.Http` (NuGet) - HttpClient factory for OAuth2 HTTP calls
- No additional frontend packages (use native `fetch` API)

---

## Risk Assessment

### Email Integration Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| SendGrid API rate limit exceeded | Low | Medium | Log errors; queue retries via Hangfire (future enhancement); does not block user operations |
| Email fails to deliver (spam filter) | Medium | Low | Provide clear instructions to check spam folder; SendGrid domain verification reduces spam risk |
| HTML email renders incorrectly | Low | Medium | Test across Gmail, Outlook, Apple Mail; use email-safe CSS properties; include plain-text fallback |
| Credential leakage (API key in repo) | Medium | High | Use environment variables; never commit `appsettings.Development.json` to repo; use secrets management |

### Social Login Risks (Planned)

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| OAuth2 provider service downtime | Low | Medium | Catch `HttpRequestException`, return 503 error; email/password login remains available |
| User confused by multiple sign-in methods | Medium | Low | Clear UI labels ("Continue with Google"); remember last-used method in cookie |
| Email conflicts (same email, different providers) | Low | Medium | Reject with 409 Conflict; show clear error message; suggest contacting support |
| CSRF attack (state parameter bypass) | Low | High | Validate state parameter; use cryptographically random state values; store state in session |
| OAuth2 credential leakage | Medium | High | Use environment variables; never commit credentials to repo; rotate credentials regularly |

---

## Success Metrics

**Email Integration**:
- ✅ 95% of welcome emails delivered within 60 seconds (measured via SendGrid dashboard)
- ✅ 98% of password reset emails delivered within 60 seconds
- ✅ Zero user-facing errors during sign-up/forgot-password flows when email service fails
- ✅ Email templates display correctly in Gmail, Outlook, Apple Mail (manual verification)

**Social Login Planning**:
- ✅ Implementation plan is comprehensive (covers all layers, files, and edge cases)
- ✅ OAuth2 authorization code flow documented end-to-end with diagram
- ✅ All API endpoint contracts specified with request/response schemas
- ✅ Edge case handling strategies documented for 6 scenarios
- ✅ Configuration requirements documented with example appsettings.json

---

## Completion Checklist

### Part 1: Email Integration (Immediate)

- [ ] Create `welcome.html` template
- [ ] Create `password-reset.html` template
- [ ] Update `EmailTemplates.cs` helper with new methods
- [ ] Update `SendGridEmailService.cs` with implementation
- [ ] Update `EmailSettings.cs` with `ResetPasswordBaseUrl` property
- [ ] Update `SignUpCommandHandler.cs` to call welcome email
- [ ] Update `ForgotPasswordCommandHandler.cs` to call password reset email
- [ ] Update `appsettings.Development.json` with email configuration
- [ ] Test welcome email on sign-up
- [ ] Test password reset email on forgot-password
- [ ] Test email failure does not block operations
- [ ] Verify email templates render correctly in major clients

### Part 2: Social Login Planning (Documentation Only)

- [x] Document all new files to create (Domain, Application, Infrastructure, Persistence, Presentation, Frontend)
- [x] Document all files to modify
- [x] Document OAuth2 authorization code flow (end-to-end diagram)
- [x] Document API endpoint contracts with request/response schemas
- [x] Document edge case handling strategies (6 scenarios)
- [x] Document configuration requirements with example appsettings.json
- [x] Document database migration details
- [x] Document frontend component structure
- [x] Document testing quickstart guide
- [x] Include implementation order checklist

---

## Notes

- **Email service infrastructure already exists** from Phase 2 (IEmailService, SendGridEmailService, AwsSesEmailService, EmailSettings, DI registration). This feature focuses on creating branded templates and wiring up command handlers.
- **Fire-and-forget pattern** used for email sending (discard result with `_`). Prevents blocking user operations on email failures.
- **Graceful degradation**: All email send exceptions are caught and logged; user operations (sign-up, forgot-password) succeed regardless of email status.
- **Social login is planning only** (Part 2). No code is written in this phase. The plan above serves as the implementation blueprint for a future session.
- **OAuth2 security**: State parameter prevents CSRF; email_verified claim validation prevents unverified accounts; server-side token exchange prevents access token leakage.
- **Account linking strategy**: Automatic linking by email match (for local → social flow); conflict rejection for social → different social flow; user can link/unlink social providers in profile settings (future enhancement).
