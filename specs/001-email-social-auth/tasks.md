# Tasks: Email Integration and Social Login Planning

**Input**: Design documents from `/specs/001-email-social-auth/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `source/MoriiCoffee.*` projects (Clean Architecture)
- **Frontend**: Frontend work is planning only (US3)
- Paths shown below use absolute references from repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Verify SendGrid NuGet package is installed in MoriiCoffee.Infrastructure project
- [ ] T002 [P] Create EmailTemplates directory in source/MoriiCoffee.Infrastructure/Resources/
- [ ] T003 [P] Verify IEmailService abstraction exists in source/MoriiCoffee.Application/SeedWork/Abstractions/IEmailService.cs
- [ ] T004 [P] Verify SendGridEmailService implementation exists in source/MoriiCoffee.Infrastructure/Services/Email/SendGridEmailService.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Update EmailSettings class in source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs to add ResetPasswordBaseUrl property
- [ ] T006 [P] Create EmailTemplates helper class in source/MoriiCoffee.Infrastructure/Services/Email/EmailTemplates.cs with methods to load and parse HTML templates
- [ ] T007 [P] Configure appsettings.Development.json with SendGrid API key, sender email, sender name, and reset password base URL

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Welcome Email on Sign Up (Priority: P1) 🎯 MVP

**Goal**: Send branded welcome email immediately after user account creation via signup endpoint

**Independent Test**: Call POST /api/v1/auth/signup with test email; verify welcome email arrives within 60 seconds containing username, welcome message, and storefront link

### Implementation for User Story 1

- [ ] T008 [P] [US1] Create welcome.html email template in source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/welcome.html with Morii Coffee branding
- [ ] T009 [P] [US1] Add WelcomeEmail() method to EmailTemplates helper in source/MoriiCoffee.Infrastructure/Services/Email/EmailTemplates.cs
- [ ] T010 [US1] Implement SendWelcomeEmailAsync() method in SendGridEmailService in source/MoriiCoffee.Infrastructure/Services/Email/SendGridEmailService.cs
- [ ] T011 [US1] Update IEmailService interface in source/MoriiCoffee.Application/SeedWork/Abstractions/IEmailService.cs to add SendWelcomeEmailAsync() method signature
- [ ] T012 [US1] Update SignUpCommandHandler in source/MoriiCoffee.Application/Commands/Auth/SignUp/SignUpCommandHandler.cs to call SendWelcomeEmailAsync() with fire-and-forget pattern
- [ ] T013 [US1] Test welcome email manually: call POST /api/v1/auth/signup via Swagger and verify email delivery

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Password Reset Email (Priority: P2)

**Goal**: Send password reset email containing time-limited reset link when user requests password recovery

**Independent Test**: Call POST /api/v1/auth/forgot-password with registered email; verify reset email arrives within 60 seconds with valid reset link

### Implementation for User Story 2

- [ ] T014 [P] [US2] Create password-reset.html email template in source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/password-reset.html with Morii Coffee branding
- [ ] T015 [P] [US2] Add PasswordResetEmail() method to EmailTemplates helper in source/MoriiCoffee.Infrastructure/Services/Email/EmailTemplates.cs
- [ ] T016 [US2] Implement SendPasswordResetEmailAsync() method in SendGridEmailService in source/MoriiCoffee.Infrastructure/Services/Email/SendGridEmailService.cs
- [ ] T017 [US2] Update IEmailService interface in source/MoriiCoffee.Application/SeedWork/Abstractions/IEmailService.cs to add SendPasswordResetEmailAsync() method signature
- [ ] T018 [US2] Update ForgotPasswordCommandHandler in source/MoriiCoffee.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs to call SendPasswordResetEmailAsync() with fire-and-forget pattern
- [ ] T019 [US2] Test password reset email manually: call POST /api/v1/auth/forgot-password via Swagger and verify email delivery with valid reset link

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Social Login Planning (Priority: P3)

**Goal**: Produce comprehensive implementation plan for OAuth2 social login with Google and Meta providers

**Independent Test**: Review plan documentation (data-model.md, contracts/, plan.md Phase 2) and verify all required components are documented with file paths and implementation details

### Documentation for User Story 3 (Planning Only - No Code)

- [ ] T020 [US3] Review data-model.md and verify User entity extensions are fully specified (ExternalProvider, ExternalProviderId, LinkExternalProvider() method)
- [ ] T021 [US3] Review contracts/api-endpoints.md and verify all OAuth2 endpoint contracts are documented (POST /api/v1/auth/social-login, GET /api/v1/auth/social-login/{provider}/authorization-url)
- [ ] T022 [US3] Review plan.md Phase 2 section and verify OAuth2 implementation checklist is complete (all files to create/modify listed across Domain, Application, Infrastructure, Persistence, Presentation, Frontend layers)
- [ ] T023 [US3] Review research.md OAuth2 section and verify Google/Meta setup procedures are documented (client ID/secret, redirect URIs, scopes)
- [ ] T024 [US3] Review quickstart.md and verify social login testing guide is complete (Google OAuth2 setup steps, Meta OAuth2 setup steps, test scenarios)
- [ ] T025 [US3] Verify edge case handling strategies are documented (email conflicts, denied consent, unverified provider emails) in plan.md or contracts/

**Checkpoint**: All user stories should now be independently functional (US1, US2 implemented; US3 documented for future implementation)

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T026 [P] Test welcome email template rendering in Gmail, Outlook, and Apple Mail clients
- [ ] T027 [P] Test password reset email template rendering in Gmail, Outlook, and Apple Mail clients
- [ ] T028 [P] Test email failure graceful degradation: set invalid SendGrid API key, verify signup still succeeds
- [ ] T029 [P] Test password reset token expiry: wait for token to expire, verify expired link shows error message
- [ ] T030 Test concurrent password reset requests: request reset twice, verify only latest token works
- [ ] T031 [P] Verify email send attempts are logged with structured properties (timestamp, recipient, template, status, error)
- [ ] T032 [P] Update documentation in specs/001-email-social-auth/quickstart.md with final testing instructions
- [ ] T033 Code review: verify all email template HTML uses table-based layout with inline CSS (hex colors only, no OKLCH)
- [ ] T034 Code review: verify fire-and-forget pattern is used correctly (no await, no exception throwing in SendGridEmailService)
- [ ] T035 Code review: verify EmailSettings.ResetPasswordBaseUrl is used to construct password reset links

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3, 4, 5)**:
  - US1 (Phase 3): Depends on Foundational (Phase 2) - No dependencies on other stories
  - US2 (Phase 4): Depends on Foundational (Phase 2) - Can run in parallel with US1
  - US3 (Phase 5): Depends on Foundational (Phase 2) - Documentation only, no code dependencies
- **Polish (Phase 6)**: Depends on all user stories (Phase 3, 4, 5) being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - No dependencies on other stories (can run in parallel with US1)
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Documentation only, no code dependencies

### Within Each User Story

**User Story 1 (Welcome Email)**:
- HTML template (T008) and helper method (T009) can run in parallel
- SendGridEmailService implementation (T010, T011) depends on helper method (T009)
- SignUpCommandHandler update (T012) depends on SendGridEmailService implementation (T010)
- Manual testing (T013) depends on SignUpCommandHandler update (T012)

**User Story 2 (Password Reset Email)**:
- HTML template (T014) and helper method (T015) can run in parallel
- SendGridEmailService implementation (T016, T017) depends on helper method (T015)
- ForgotPasswordCommandHandler update (T018) depends on SendGridEmailService implementation (T016)
- Manual testing (T019) depends on ForgotPasswordCommandHandler update (T018)

**User Story 3 (Social Login Planning)**:
- All documentation tasks (T020-T025) can run in parallel (review existing documents)

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T002, T003, T004)
- All Foundational tasks marked [P] can run in parallel (T006, T007)
- **User Story 1 and User Story 2 can run in parallel** after Foundational phase completes
- Within US1: HTML template (T008) and helper method (T009) can run in parallel
- Within US2: HTML template (T014) and helper method (T015) can run in parallel
- Within US3: All documentation review tasks (T020-T025) can run in parallel
- Polish phase: Testing tasks marked [P] can run in parallel (T026, T027, T028, T029, T031, T032, T033)

---

## Parallel Example: User Story 1

```bash
# Launch HTML template and helper method together:
Task: "Create welcome.html email template"
Task: "Add WelcomeEmail() method to EmailTemplates helper"

# After both complete, implement SendGridEmailService:
Task: "Implement SendWelcomeEmailAsync() method"

# After SendGridEmailService complete, update command handler:
Task: "Update SignUpCommandHandler to call SendWelcomeEmailAsync()"
```

---

## Parallel Example: User Story 1 AND User Story 2 Together

```bash
# Once Foundational phase (T005-T007) completes, launch both stories in parallel:

# Developer A works on User Story 1:
Task: "Create welcome.html email template"
Task: "Add WelcomeEmail() method to EmailTemplates helper"
Task: "Implement SendWelcomeEmailAsync() method"
Task: "Update SignUpCommandHandler"

# Developer B works on User Story 2 (in parallel):
Task: "Create password-reset.html email template"
Task: "Add PasswordResetEmail() method to EmailTemplates helper"
Task: "Implement SendPasswordResetEmailAsync() method"
Task: "Update ForgotPasswordCommandHandler"

# Both stories complete and integrate independently
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T007) - CRITICAL, blocks all stories
3. Complete Phase 3: User Story 1 (T008-T013)
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

**Deliverable**: Welcome email on signup (MVP!)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (Welcome email MVP)
3. Add User Story 2 → Test independently → Deploy/Demo (Password reset email)
4. Complete User Story 3 → Documentation ready for future implementation
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (T001-T007)
2. Once Foundational is done:
   - **Developer A: User Story 1** (T008-T013)
   - **Developer B: User Story 2** (T014-T019)
   - **Developer C: User Story 3** (T020-T025) - Documentation review
3. Stories complete and integrate independently
4. Team reconvenes for Polish phase (T026-T035)

---

## Task Summary

| Phase | Task Range | Count | Parallelizable | Story |
|-------|------------|-------|----------------|-------|
| Setup | T001-T004 | 4 | 3 | N/A |
| Foundational | T005-T007 | 3 | 2 | N/A |
| User Story 1 (P1) | T008-T013 | 6 | 2 | Welcome Email |
| User Story 2 (P2) | T014-T019 | 6 | 2 | Password Reset |
| User Story 3 (P3) | T020-T025 | 6 | 6 (all) | Social Login Planning |
| Polish | T026-T035 | 10 | 7 | Cross-cutting |
| **TOTAL** | T001-T035 | **35** | **22** | 3 stories |

**Breakdown by User Story**:
- **US1 (P1)**: 6 tasks (2 parallelizable) - Welcome email implementation
- **US2 (P2)**: 6 tasks (2 parallelizable) - Password reset email implementation
- **US3 (P3)**: 6 tasks (6 parallelizable) - Social login planning documentation
- **Shared**: 17 tasks (15 parallelizable) - Setup, Foundational, Polish

**Parallel Opportunities**:
- 22 out of 35 tasks (63%) can run in parallel with other tasks
- User Story 1 and User Story 2 are fully independent after Foundational phase
- User Story 3 documentation tasks can all run in parallel

**MVP Scope** (Minimum Viable Product):
- User Story 1 only: 13 tasks total (Setup + Foundational + US1)
- Estimated effort: 1-2 days for single developer
- Delivers core value: Welcome email on signup

**Full Feature Scope** (All 3 User Stories):
- All tasks: 35 tasks total
- Estimated effort: 3-4 days for single developer, 2-3 days with parallel team
- Delivers: Welcome email + Password reset email + Social login planning documentation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- **No tests requested** - spec does not require automated tests; manual testing via Swagger is sufficient
- Commit after each logical task group (e.g., commit after US1 complete, commit after US2 complete)
- Stop at any checkpoint to validate story independently
- User Story 3 is documentation only (no code); success measured by plan completeness
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence

---

## Verification Checklist

After completing all tasks, verify:

- [ ] Welcome email sends on signup (US1)
- [ ] Password reset email sends on forgot-password request (US2)
- [ ] Email failures do not block user operations (graceful degradation)
- [ ] Email templates display correctly in Gmail, Outlook, Apple Mail
- [ ] Password reset links expire correctly (expiry validation works)
- [ ] Concurrent password reset requests handled correctly (latest token wins)
- [ ] Social login plan documentation is comprehensive (data-model.md, contracts/, plan.md reviewed)
- [ ] All email send attempts logged with structured properties
- [ ] SendGrid configuration stored in appsettings.json (not hardcoded)
- [ ] Fire-and-forget pattern used correctly (no blocking on email send)
