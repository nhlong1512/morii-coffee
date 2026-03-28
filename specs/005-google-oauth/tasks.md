---
description: "Implementation tasks for Google OAuth external authentication"
---

# Tasks: Google OAuth External Authentication

**Input**: Design documents from `/specs/005-google-oauth/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, quickstart.md

**Tests**: No automated tests requested in specification. Manual testing via quickstart.md after implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

MoriiCoffee uses Clean Architecture with these directories:
- **Domain**: `source/MoriiCoffee.Domain/Aggregates/UserAggregate/`
- **Application**: `source/MoriiCoffee.Application/Commands/Auth/`
- **Infrastructure**: `source/MoriiCoffee.Infrastructure/Configurations/`
- **Presentation**: `source/MoriiCoffee.Presentation/Controllers/`

---

## Phase 1: Setup (NuGet Package and Configuration)

**Purpose**: Install dependencies and configure Google OAuth credentials

- [X] T001 Install Microsoft.AspNetCore.Authentication.Google NuGet package to source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [X] T002 [P] Add Google OAuth configuration section to source/MoriiCoffee.Presentation/appsettings.json (Authentication:Google:ClientId and ClientSecret placeholders)
- [X] T003 [P] Add Google OAuth development configuration to source/MoriiCoffee.Presentation/appsettings.Development.json
- [X] T004 [P] Document User Secrets setup for development in specs/005-google-oauth/quickstart.md (already created)

---

## Phase 2: Foundational (Authentication Infrastructure)

**Purpose**: Register Google OAuth provider in ASP.NET Core Identity middleware

**⚠️ CRITICAL**: This phase MUST be complete before ANY user story can be implemented

- [X] T005 Create AuthenticationConfiguration.cs in source/MoriiCoffee.Infrastructure/Configurations/ to configure Google OAuth with AddAuthentication().AddGoogle()
- [X] T006 Register Google authentication in source/MoriiCoffee.Infrastructure/DependencyInjection.cs by calling AuthenticationConfiguration setup method
- [X] T007 Verify Google OAuth configuration is loaded correctly by building project with dotnet build --no-incremental

**Checkpoint**: Foundation ready - Google OAuth provider registered, user story implementation can now begin

---

## Phase 3: User Story 1 - Quick Sign-In with Google Account (Priority: P1) 🎯 MVP

**Goal**: Enable users to initiate OAuth flow, authenticate with Google, and receive access/refresh tokens

**Independent Test**: Navigate to `/api/v1/auth/external-login?provider=Google`, complete Google authentication, verify redirect with token cookie

### Implementation for User Story 1

- [X] T008 [P] [US1] Create ExternalLoginCommand.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLogin/ with Provider and ReturnUrl properties
- [X] T009 [P] [US1] Create ExternalLoginResponseDto.cs in source/MoriiCoffee.Application/SeedWork/DTOs/Auth/ with RedirectUrl property
- [X] T010 [P] [US1] Create ExternalLoginCommandValidator.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLogin/ to validate Provider is "Google"
- [X] T011 [US1] Implement ExternalLoginCommandHandler.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLogin/ using SignInManager.ConfigureExternalAuthenticationProperties
- [X] T012 [P] [US1] Create ExternalLoginCallbackCommand.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLoginCallback/ with Code, State, ReturnUrl properties
- [X] T013 [P] [US1] Create ExternalLoginCallbackCommandValidator.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLoginCallback/ to validate Code and State are not empty
- [X] T014 [US1] Implement ExternalLoginCallbackCommandHandler.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLoginCallback/ with SignInManager.GetExternalLoginInfoAsync and token generation
- [X] T015 [US1] Add external-login POST endpoint to source/MoriiCoffee.Presentation/Controllers/AuthController.cs that calls ExternalLoginCommand
- [X] T016 [US1] Add external-auth-callback GET endpoint to source/MoriiCoffee.Presentation/Controllers/AuthController.cs that calls ExternalLoginCallbackCommand
- [X] T017 [P] [US1] Add XML documentation comments to ExternalLoginCommand describing OAuth flow initiation
- [X] T018 [P] [US1] Add XML documentation comments to ExternalLoginCallbackCommand describing callback processing
- [X] T019 [P] [US1] Add XML documentation to external-login endpoint in AuthController.cs with request/response examples
- [X] T020 [P] [US1] Add XML documentation to external-auth-callback endpoint in AuthController.cs with error scenarios
- [X] T021 [US1] Build project with dotnet build --no-incremental to verify no compilation errors

**Checkpoint**: At this point, User Story 1 should be fully functional - users can initiate OAuth, authenticate with Google, and receive tokens

---

## Phase 4: User Story 2 - Automatic Account Creation and Role Assignment (Priority: P2)

**Goal**: Automatically create customer accounts for new Google users and assign CUSTOMER role

**Independent Test**: Sign in with a new Google account (never used with MoriiCoffee), verify account created in AspNetUsers table with CUSTOMER role assigned

### Implementation for User Story 2

- [X] T022 [US2] Add user account creation logic to ExternalLoginCallbackCommandHandler.cs to check if email exists in AspNetUsers
- [X] T023 [US2] Add UserManager.CreateAsync call in ExternalLoginCallbackCommandHandler.cs for new Google users with email, username, full name, EmailConfirmed=true
- [X] T024 [US2] Add UserManager.AddLoginAsync call in ExternalLoginCallbackCommandHandler.cs to link Google account to user in AspNetUserLogins table
- [X] T025 [US2] Add role assignment logic in ExternalLoginCallbackCommandHandler.cs using UserManager.AddToRoleAsync with "CUSTOMER" role
- [X] T026 [US2] Add existing user linking logic in ExternalLoginCallbackCommandHandler.cs to find user by email and link Google account if not already linked
- [X] T027 [US2] Add welcome email sending in ExternalLoginCallbackCommandHandler.cs using existing EmailService for new users only
- [X] T028 [US2] Add error handling for inactive/deleted accounts in ExternalLoginCallbackCommandHandler.cs returning 403 Forbidden
- [X] T029 [US2] Add error handling for missing email from Google in ExternalLoginCallbackCommandHandler.cs returning 400 Bad Request
- [X] T030 [US2] Build project with dotnet build --no-incremental to verify no compilation errors

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - new users get accounts with CUSTOMER role, existing users link Google

---

## Phase 5: User Story 3 - Seamless Token Management and Session Handling (Priority: P3)

**Goal**: Store refresh tokens securely and support token refresh flow without re-authentication

**Independent Test**: Sign in with Google, extract refresh token from cookie, use refresh token to obtain new access token via /refresh-token endpoint

### Implementation for User Story 3

- [X] T031 [US3] Add refresh token storage in ExternalLoginCallbackCommandHandler.cs using UserManager.SetAuthenticationTokenAsync with LoginProvider="Google", Name="REFRESH"
- [X] T032 [US3] Add refresh token replacement logic in ExternalLoginCallbackCommandHandler.cs to remove old token before storing new one
- [X] T033 [US3] Add AuthTokenHolder cookie creation in ExternalLoginCallbackCommandHandler.cs with HttpOnly, Secure, SameSite=Strict flags and 5-minute expiration
- [X] T034 [US3] Verify existing RefreshTokenCommandHandler in source/MoriiCoffee.Application/Commands/Auth/RefreshToken/ works with Google-issued refresh tokens
- [X] T035 [US3] Build project with dotnet build --no-incremental to verify no compilation errors

**Checkpoint**: All user stories should now be independently functional - tokens stored securely, refresh flow works

---

## Phase 6: Google Cloud Console Setup (External Dependency)

**Purpose**: Configure Google OAuth 2.0 credentials in Google Cloud Console

- [ ] T036 Create Google Cloud project at https://console.cloud.google.com/
- [ ] T037 Enable Google+ API in Google Cloud Console APIs & Services Library
- [ ] T038 Create OAuth 2.0 credentials in Google Cloud Console with Web application type
- [ ] T039 Add authorized redirect URIs to Google OAuth credentials: http://localhost:8002/api/v1/auth/external-auth-callback and http://localhost:8002/signin-google
- [ ] T040 Copy Google Client ID and Client Secret and store in User Secrets using dotnet user-secrets set commands
- [ ] T041 Document Client ID and Client Secret configuration in specs/005-google-oauth/quickstart.md (already documented)

---

## Phase 7: Manual Testing and Verification

**Purpose**: Execute manual test scenarios from quickstart.md to verify OAuth flow

- [ ] T042 Start development environment using bash deploy/run-docker-development.sh
- [ ] T043 Test Scenario 1: New user sign-in with Google (navigate to external-login, complete Google auth, verify token cookie)
- [ ] T044 Verify new user created in AspNetUsers table with EmailConfirmed=true and Status=Active
- [ ] T045 Verify Google link created in AspNetUserLogins table with LoginProvider="Google"
- [ ] T046 Verify CUSTOMER role assigned in AspNetUserRoles table
- [ ] T047 Verify refresh token stored in AspNetUserTokens table with LoginProvider="Google" and Name="REFRESH"
- [ ] T048 Verify welcome email sent to new user's email address
- [ ] T049 Test access token authentication by making GET /api/v1/users/me request with Authorization header
- [ ] T050 Test Scenario 2: Existing user linking (sign up with email/password first, then sign in with Google using same email)
- [ ] T051 Verify no duplicate user created (count AspNetUsers rows with same email = 1)
- [ ] T052 Verify Google linked to existing account in AspNetUserLogins table
- [ ] T053 Verify no welcome email sent for existing user
- [ ] T054 Test Scenario 3: Returning user sign-in (sign in with Google again using same account)
- [ ] T055 Verify refresh token replaced (not duplicated) in AspNetUserTokens table
- [ ] T056 Test Scenario 4: Token refresh flow (extract refresh token from cookie, call POST /refresh-token, verify new access token works)
- [ ] T057 Test Scenario 5A: User denies permission (click Cancel on Google consent screen, verify error message)
- [ ] T058 Test Scenario 5B: Invalid provider (call external-login with provider=Facebook, verify 400 error)
- [ ] T059 Test Scenario 5C: Invalid state parameter (tamper with state in callback URL, verify 401 error)
- [ ] T060 Test Scenario 5D: Expired OAuth flow (wait 20 minutes between external-login and callback, verify 401 error)
- [ ] T061 Test Scenario 5E: Inactive account (mark user as inactive in database, sign in with Google, verify 403 error)
- [ ] T062 Test Scenario 6: Multiple sign-in sessions (sign in on two different browsers, verify both tokens work)

---

## Phase 8: Documentation (Mandatory per CLAUDE.md)

**Purpose**: Create summary documentation in Vietnamese and English

**Note**: Skipped VN/ENG summaries per user request. Created Next.js integration guide instead.

- [ ] T063 [P] Create docs/explainations/summary-google-oauth-VN.md documenting all changes, files modified, database tables used, API endpoints added (SKIPPED - user requested)
- [ ] T064 [P] Create docs/explainations/summary-google-oauth-ENG.md documenting all changes, files modified, database tables used, API endpoints added (SKIPPED - user requested)
- [ ] T065 Verify both summary files include: What was implemented, Files created/modified, Database changes, API changes, Business rules, How to test (SKIPPED - user requested)
- [X] T066 Update docs/features/google-auth/google-auth-explaination.md status from "Not Yet Implemented" to "Implemented" with implementation date
- [X] T067 (BONUS) Create docs/features/google-auth/google-auth-integration-guide.md for Next.js frontend integration (comprehensive guide with code examples)

---

## Phase 9: Git Commit and Deployment Preparation

**Purpose**: Commit changes with descriptive message and prepare for deployment

- [ ] T067 Run git status to verify all OAuth-related files are staged
- [ ] T068 Run git diff to review all changes before commit
- [ ] T069 Create git commit with message: "feat: implement Google OAuth external authentication with automatic account creation and role assignment"
- [ ] T070 Add Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com> to commit message
- [ ] T071 Verify commit includes all new files (ExternalLogin commands, ExternalLoginCallback commands, AuthenticationConfiguration)
- [ ] T072 Document production deployment requirements in specs/005-google-oauth/deployment.md (HTTPS, environment variables, redirect URIs)
- [ ] T073 Do NOT push to remote repository (wait for user approval)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User Story 1 (P1): OAuth flow initiation and callback processing
  - User Story 2 (P2): Account creation and role assignment (integrates with US1)
  - User Story 3 (P3): Token storage and refresh (integrates with US1)
- **Google Cloud Setup (Phase 6)**: Can be done in parallel with Phase 3-5 implementation
- **Manual Testing (Phase 7)**: Depends on Phase 1-6 completion
- **Documentation (Phase 8)**: Depends on Phase 7 completion (all features tested)
- **Git Commit (Phase 9)**: Depends on Phase 8 completion (documentation ready)

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories - CORE OAUTH FLOW
- **User Story 2 (P2)**: Depends on User Story 1 completion - Extends ExternalLoginCallbackCommandHandler with account creation logic
- **User Story 3 (P3)**: Depends on User Story 1 completion - Extends ExternalLoginCallbackCommandHandler with token storage logic

**Important**: User Stories 2 and 3 are NOT independent - they extend the same handler created in US1. Must implement sequentially: US1 → US2 → US3.

### Within Each User Story

- **User Story 1**: Commands before handlers, handlers before controller endpoints, XML docs in parallel with implementation
- **User Story 2**: Account creation logic → Role assignment → Email sending → Error handling
- **User Story 3**: Refresh token storage → Cookie creation → Refresh flow verification

### Parallel Opportunities

- **Phase 1**: T002, T003, T004 can run in parallel (different files)
- **Phase 3 (US1)**: T008/T009/T010 can run in parallel, T012/T013 can run in parallel, T017/T018/T019/T020 can run in parallel
- **Phase 6**: Can run in parallel with Phase 3-5 (external Google Cloud Console setup)
- **Phase 8**: T063 and T064 can run in parallel (different files)

---

## Parallel Example: User Story 1

```bash
# Launch all command/DTO/validator creation together:
Task: "Create ExternalLoginCommand.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLogin/"
Task: "Create ExternalLoginResponseDto.cs in source/MoriiCoffee.Application/SeedWork/DTOs/Auth/"
Task: "Create ExternalLoginCommandValidator.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLogin/"

# Launch all callback-related command creation together:
Task: "Create ExternalLoginCallbackCommand.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLoginCallback/"
Task: "Create ExternalLoginCallbackCommandValidator.cs in source/MoriiCoffee.Application/Commands/Auth/ExternalLoginCallback/"

# Launch all XML documentation tasks together:
Task: "Add XML documentation comments to ExternalLoginCommand"
Task: "Add XML documentation comments to ExternalLoginCallbackCommand"
Task: "Add XML documentation to external-login endpoint in AuthController.cs"
Task: "Add XML documentation to external-auth-callback endpoint in AuthController.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004) → Google OAuth package installed
2. Complete Phase 2: Foundational (T005-T007) → Google OAuth provider registered
3. Complete Phase 3: User Story 1 (T008-T021) → OAuth flow works end-to-end
4. Complete Phase 6: Google Cloud Console Setup (T036-T041) → OAuth credentials configured
5. **STOP and VALIDATE**: Test User Story 1 via quickstart.md Scenario 1 (T042-T049)
6. Deploy/demo if OAuth flow works correctly

**Rationale**: User Story 1 is the core OAuth flow. If this doesn't work, no other features matter. Validate early before investing in account creation and token storage features.

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready (T001-T007)
2. Add User Story 1 → Test independently (T008-T021, T036-T049) → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently (T022-T030, T050-T053) → Deploy/Demo (Account creation works)
4. Add User Story 3 → Test independently (T031-T035, T054-T056) → Deploy/Demo (Token refresh works)
5. Complete all error scenarios (T057-T062) → Deploy/Demo (Robust error handling)
6. Complete documentation (T063-T066) → Deploy/Demo (Production-ready)
7. Each phase adds value without breaking previous features

### Full Feature (All User Stories)

1. Complete all phases sequentially (T001-T073)
2. Test all scenarios from quickstart.md (T042-T062)
3. Create documentation in VN and ENG (T063-T066)
4. Git commit with descriptive message (T067-T073)
5. Ready for production deployment (after updating Google OAuth credentials with production redirect URIs)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- No automated tests in this feature - using manual testing via quickstart.md instead
- User Stories 2 and 3 extend User Story 1's handler (not independent implementations)
- Commit only after all manual testing passes and documentation is complete
- Google Cloud Console setup can be done in parallel with implementation
- Follow clean-architecture-skill conventions: XML docs, DataAnnotations, IEntityTypeConfiguration for relationships
- Build project after each user story phase to catch compilation errors early
- Do NOT push to remote until user approves (per CLAUDE.md)

---

## Task Summary

- **Total Tasks**: 73
- **Phase 1 (Setup)**: 4 tasks
- **Phase 2 (Foundational)**: 3 tasks (BLOCKS all user stories)
- **Phase 3 (User Story 1)**: 14 tasks - Core OAuth flow
- **Phase 4 (User Story 2)**: 9 tasks - Account creation and role assignment
- **Phase 5 (User Story 3)**: 5 tasks - Token storage and refresh
- **Phase 6 (Google Cloud Setup)**: 6 tasks (can run in parallel with implementation)
- **Phase 7 (Manual Testing)**: 21 tasks - Comprehensive test scenarios
- **Phase 8 (Documentation)**: 4 tasks - VN and ENG summaries
- **Phase 9 (Git Commit)**: 7 tasks - Commit and deployment prep

**Parallel Opportunities Identified**: 11 tasks marked [P] across phases

**Independent Test Criteria**:
- **US1**: Navigate to external-login, complete Google auth, verify token cookie received
- **US2**: Sign in with new Google account, verify account created with CUSTOMER role in database
- **US3**: Extract refresh token, call /refresh-token endpoint, verify new access token works

**Suggested MVP Scope**: User Story 1 only (T001-T021, T036-T049) - Core OAuth flow without account creation or token storage features. Delivers immediate value by enabling Google sign-in for existing users.
