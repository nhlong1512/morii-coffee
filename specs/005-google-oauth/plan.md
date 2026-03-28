# Implementation Plan: Google OAuth External Authentication

**Branch**: `005-google-oauth` | **Date**: 2026-03-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-google-oauth/spec.md`

## Summary

Implement Google OAuth 2.0 authentication for MoriiCoffee, allowing users to sign in using their Google accounts without creating passwords. The system will automatically create customer accounts for new Google users, link Google accounts to existing users by email match, assign CUSTOMER roles, and manage JWT access/refresh tokens. The implementation follows OAuth 2.0 Authorization Code Flow with ASP.NET Core Identity integration.

## Technical Context

**Language/Version**: C# / .NET 8.0
**Primary Dependencies**: ASP.NET Core Identity, Microsoft.AspNetCore.Authentication.Google, MediatR, FluentValidation, EF Core
**Storage**: SQL Server (via Entity Framework Core) - AspNetUsers, AspNetUserLogins, AspNetUserTokens tables
**Testing**: Manual testing via Swagger UI and browser OAuth flow (no automated test suite currently exists)
**Target Platform**: Linux/Windows server (Docker)
**Project Type**: Web API (Clean Architecture with MediatR CQRS pattern)
**Performance Goals**: <30 seconds for complete OAuth flow (including Google authentication time), <200ms p95 for token generation
**Constraints**: HTTPS required for OAuth callback in production, cookie-based token delivery with 5-minute expiration, state parameter validation for CSRF protection
**Scale/Scope**: Affects 2 new command handlers, 2 new DTOs, 1 controller update, configuration changes, new NuGet package dependency

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Plan Mode Default
- **Status**: PASS
- **Evidence**: Using `/speckit.plan` command with detailed technical context and research phase

### ✅ II. Verification Before Done
- **Status**: PASS
- **Plan**: Will test complete OAuth flow manually: initiate sign-in → Google authentication → callback processing → token receipt → authenticated API access. Verify new user creation, existing user linking, role assignment, and token refresh.

### ✅ III. Simplicity First & Minimal Impact
- **Status**: PASS
- **Evidence**: Changes limited to:
  - New commands for ExternalLogin and ExternalLoginCallback (no modification of existing auth)
  - Configuration addition (Google OAuth credentials)
  - No database migration (uses existing ASP.NET Identity tables)
  - No refactoring of existing sign-in/sign-up flows
  - Reuses existing TokenService, EmailService, UserManager

### ✅ IV. Subagent Strategy & Delegation
- **Status**: PASS (if needed)
- **Plan**: Use Explore subagent if codebase exploration reveals complex dependencies

### ⚠️ V. Self-Improvement Loop
- **Status**: N/A (no corrections yet)
- **Plan**: Will update `tasks/lessons.md` if corrected during implementation

### ✅ VI. Autonomous Execution with Concise Communication
- **Status**: PASS
- **Plan**: Will implement OAuth flow end-to-end, verify with manual testing, report results concisely

## Project Structure

### Documentation (this feature)

```text
specs/005-google-oauth/
├── plan.md              # This file
├── data-model.md        # Phase 1 output (ASP.NET Identity tables analysis)
├── quickstart.md        # Phase 1 output (OAuth testing guide)
├── contracts/           # Phase 1 output (API contracts for external-login and callback)
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Domain/
│   └── Aggregates/
│       └── UserAggregate/
│           └── User.cs                              # NO CHANGES (already extends IdentityUser)
│
├── MoriiCoffee.Application/
│   ├── Commands/
│   │   └── Auth/
│   │       ├── ExternalLogin/                      # NEW FOLDER
│   │       │   ├── ExternalLoginCommand.cs         # NEW: Command to initiate OAuth
│   │       │   └── ExternalLoginCommandHandler.cs  # NEW: Prepares OAuth challenge
│   │       └── ExternalLoginCallback/              # NEW FOLDER
│   │           ├── ExternalLoginCallbackCommand.cs # NEW: Command to process callback
│   │           └── ExternalLoginCallbackCommandHandler.cs # NEW: Creates/links user, issues tokens
│   └── SeedWork/
│       └── DTOs/
│           └── Auth/
│               └── ExternalLoginResponseDto.cs     # NEW: OAuth challenge response DTO
│
├── MoriiCoffee.Infrastructure/
│   ├── Configurations/
│   │   └── AuthenticationConfiguration.cs          # NEW: Google OAuth configuration
│   └── DependencyInjection.cs                     # UPDATE: Register Google auth
│
├── MoriiCoffee.Presentation/
│   ├── Controllers/
│   │   └── AuthController.cs                      # UPDATE: Add external-login and external-auth-callback endpoints
│   ├── appsettings.json                           # UPDATE: Add Authentication:Google section
│   └── appsettings.Development.json               # UPDATE: Add Google OAuth dev config
│
└── docs/
    └── features/
        └── google-auth/
            └── google-auth-explaination.md        # EXISTING: Implementation guide (already created)
```

**Structure Decision**: This is a Clean Architecture web API. Changes are concentrated in Application (new commands/handlers), Infrastructure (OAuth configuration), and Presentation (new controller endpoints). Domain layer requires no changes as User entity already extends IdentityUser with support for external logins.

## Complexity Tracking

No constitutional violations. This feature adds new functionality without modifying existing authentication flows, following the open-closed principle.

---

## Phase 0: Research & Unknowns Resolution

### Research Questions

All technical decisions have been resolved based on existing MoriiCoffee patterns and ASP.NET Core Identity best practices. No research phase needed.

**Key Decisions Made**:

1. **OAuth Provider Package**: `Microsoft.AspNetCore.Authentication.Google` (official Microsoft package, well-maintained)
2. **Token Storage**: ASP.NET Core Identity's `AspNetUserTokens` table (already in use for refresh tokens)
3. **State Management**: ASP.NET Core Identity's built-in state parameter generation and validation (CSRF protection)
4. **Configuration Storage**: appsettings.json with User Secrets for development, environment variables for production
5. **Cookie Settings**: HttpOnly, Secure, 5-minute expiration matching existing auth patterns
6. **Error Handling**: Use existing exception types (BadRequestException, UnauthorizedException) from MoriiCoffee.Application.SeedWork.Exceptions

---

## Phase 1: Design Artifacts

### 1. Data Model Analysis (`data-model.md`)

Document ASP.NET Core Identity tables used for external authentication:
- **AspNetUsers**: No schema changes (User already extends IdentityUser<Guid>)
- **AspNetUserLogins**: Links users to external providers (LoginProvider="Google", ProviderKey=GoogleUserID)
- **AspNetUserTokens**: Stores refresh tokens (LoginProvider="Google", Name="REFRESH", Value=token)
- **AspNetRoles**: Existing CUSTOMER role assignment
- **AspNetUserRoles**: Links users to roles

### 2. API Contracts (`contracts/`)

Document request/response contracts for new endpoints:

**external-login-request.md**:
- Query params: `provider` (string, "Google"), `returnUrl` (string, default "/")
- Response: HTTP 302 redirect to Google OAuth page

**external-auth-callback-request.md**:
- Query params: `code` (authorization code from Google), `state` (CSRF token), `returnUrl`
- Response: HTTP 302 redirect to returnUrl with `AuthTokenHolder` cookie

**error-responses.md**:
- 400 Bad Request: Missing email from Google, user denial
- 401 Unauthorized: Invalid state parameter, expired tokens

### 3. Testing Quickstart (`quickstart.md`)

Step-by-step guide for manual OAuth testing:
1. Configure Google Cloud Console (Client ID, Client Secret, redirect URIs)
2. Add configuration to appsettings.Development.json
3. Start API (`bash deploy/run-docker-development.sh`)
4. Navigate to `http://localhost:8002/api/v1/auth/external-login?provider=Google&returnUrl=http://localhost:3000`
5. Complete Google authentication
6. Verify callback redirect with tokens in cookie
7. Test token extraction and API authentication
8. Test refresh token flow
9. Test new user creation (check database for AspNetUsers entry)
10. Test existing user linking (sign in with matching email)

### 4. Agent Context Update

Run `.specify/scripts/bash/update-agent-context.sh claude` to update `CLAUDE.md` with:
- Google OAuth 2.0 authentication added
- New dependencies: Microsoft.AspNetCore.Authentication.Google
- Configuration requirements: Google Client ID/Secret
- Testing checklist for OAuth flow

---

## Phase 2: Implementation Tasks

**Note**: Tasks are generated by `/speckit.tasks` command (not part of this plan document).

Expected task breakdown:

**Phase 1: NuGet Package and Configuration**
1. Install Microsoft.AspNetCore.Authentication.Google package
2. Add Google OAuth configuration section to appsettings.json
3. Create AuthenticationConfiguration.cs for Google OAuth setup
4. Register Google authentication in DependencyInjection.cs
5. Configure User Secrets for development (ClientId, ClientSecret)

**Phase 2: Command and Handler Implementation**
6. Create ExternalLoginCommand and ExternalLoginResponseDto
7. Implement ExternalLoginCommandHandler (prepare OAuth challenge)
8. Create ExternalLoginCallbackCommand
9. Implement ExternalLoginCallbackCommandHandler (process callback, create/link user, issue tokens)

**Phase 3: Controller Endpoints**
10. Add external-login POST endpoint to AuthController
11. Add external-auth-callback GET endpoint to AuthController
12. Add XML documentation for both endpoints
13. Configure Swagger annotations for OAuth endpoints

**Phase 4: Google Cloud Console Setup**
14. Create Google Cloud project and OAuth 2.0 credentials
15. Configure authorized redirect URIs (dev and prod)
16. Document Client ID and Client Secret location

**Phase 5: Testing and Verification**
17. Build project (`dotnet build --no-incremental`)
18. Start development environment
19. Test: New user sign-in with Google (verify account creation)
20. Test: Existing user sign-in with Google (verify linking)
21. Test: Role assignment (verify CUSTOMER role)
22. Test: Welcome email delivery
23. Test: Access token authentication
24. Test: Refresh token flow
25. Test: Error scenarios (user denial, missing email, invalid state)
26. Verify AspNetUserLogins table has Google entries
27. Verify AspNetUserTokens table has refresh tokens

**Phase 6: Documentation**
28. Create VN summary document (`docs/explainations/summary-google-oauth-VN.md`)
29. Create ENG summary document (`docs/explainations/summary-google-oauth-ENG.md`)
30. Update API documentation if applicable
31. Git commit with descriptive message

---

## Key Design Decisions

### Decision 1: Use ASP.NET Core Identity's External Authentication

**Choice**: Leverage SignInManager.ConfigureExternalAuthenticationProperties and GetExternalLoginInfoAsync
**Rationale**: Built-in OAuth handling provides state generation/validation, token exchange, and claims extraction. Reduces implementation complexity and security risks.
**Alternative Rejected**: Manual OAuth implementation would require custom state management, CSRF protection, and token handling—unnecessary duplication of framework features.

### Decision 2: Cookie-Based Token Delivery

**Choice**: Return tokens in HttpOnly, Secure cookie with 5-minute expiration
**Rationale**: Consistent with existing auth pattern, allows frontend to extract tokens safely, short expiration limits exposure window.
**Alternative Rejected**: Returning tokens directly in redirect URL would expose them in browser history and server logs.

### Decision 3: Email-Based Account Linking

**Choice**: Link Google accounts to existing MoriiCoffee accounts when email addresses match
**Rationale**: Users expect their existing account to work with Google sign-in if emails match. Prevents account fragmentation.
**Alternative Rejected**: Always creating new accounts would lead to duplicate accounts for users who already registered with email/password.

### Decision 4: Automatic CUSTOMER Role Assignment

**Choice**: Assign CUSTOMER role to all new Google users automatically
**Rationale**: Consistent with existing SignUp flow, enables immediate access to customer features (product browsing, ordering).
**Alternative Rejected**: Manual role assignment would require admin intervention before users can access the system.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Google OAuth service outage | Low | High | Display clear error message, allow fallback to email/password auth |
| Redirect URI mismatch in production | Medium | High | Document exact URIs in deployment guide, test in staging first |
| CSRF attack via state parameter manipulation | Low | High | ASP.NET Identity handles state validation automatically |
| User denies Google permission | Medium | Low | Handle gracefully with clear message, suggest email/password alternative |
| Google account has no verified email | Low | High | Reject sign-in with error message requiring email scope |
| Token cookie not extracted by frontend | Medium | Medium | Document cookie extraction in frontend integration guide |
| Duplicate account creation | Low | Medium | Email uniqueness check in handler prevents duplicates |

---

## Dependencies

- **External**: Google Cloud Console project with OAuth 2.0 credentials configured
- **Configuration**: Google Client ID and Client Secret stored securely (User Secrets for dev, env vars for prod)
- **Infrastructure**: HTTPS access in production for secure OAuth callback
- **Services**: Email service availability for welcome emails (existing EmailService)
- **Frontend**: Frontend application capability to initiate OAuth flow and extract tokens from cookies

---

## Rollback Plan

If issues are discovered post-deployment:
1. Remove external-login and external-auth-callback endpoints from AuthController
2. Remove Google authentication registration from DependencyInjection.cs
3. Revert appsettings.json changes
4. Redeploy previous version
5. No database rollback needed (AspNetUserLogins entries are benign if feature is disabled)

---

## Success Criteria Verification

Per spec.md, the implementation satisfies these criteria:

- **SC-001**: OAuth flow completes <30 seconds → Verified via manual timing test
- **SC-002**: 100% success or clear error → Verified via error scenario testing
- **SC-003**: Auto CUSTOMER role → Verified via database query after new user sign-in
- **SC-004**: Access tokens work → Verified via authenticated API calls
- **SC-005**: Refresh tokens work → Verified via refresh endpoint test
- **SC-006**: No duplicates → Verified via multiple sign-ins with same Google email
- **SC-007**: CSRF prevention → Verified via state parameter validation
- **SC-008**: Welcome emails <1 min → Verified via email delivery check

---

## Implementation Strategy

**MVP Approach** (User Story 1 only):
1. Install package and configuration
2. Implement commands and handlers
3. Add controller endpoints
4. Test new user OAuth sign-in
5. Deploy if successful

**Full Feature** (All user stories):
1. Complete MVP
2. Test existing user linking (User Story 2)
3. Test token refresh flow (User Story 3)
4. Complete all edge case scenarios
5. Create documentation
6. Deploy

**Recommended**: Start with MVP, verify OAuth flow works end-to-end, then add remaining functionality. This allows early detection of configuration issues (Client ID, redirect URIs) before investing in full implementation.
