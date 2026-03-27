# Tasks: Email Service for Transactional Emails

**Input**: Design documents from `/specs/003-email-service-spec/`
**Prerequisites**: plan.md (✅), spec.md (✅), research.md (✅), data-model.md (✅), contracts/ (✅), quickstart.md (✅)

**Tests**: Tests are NOT requested in this feature specification. Focus is on implementation and manual verification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions

Following Clean Architecture structure in `source/` directory:
- **Domain.Shared**: `source/MoriiCoffee.Domain.Shared/`
- **Application**: `source/MoriiCoffee.Application/`
- **Infrastructure**: `source/MoriiCoffee.Infrastructure/`
- **Presentation**: `source/MoriiCoffee.Presentation/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, NuGet packages, and configuration setup

- [x] T001 Install brevo_csharp NuGet package (v6.0.0) in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [x] T002 Create Resources/EmailTemplates directory in source/MoriiCoffee.Infrastructure/
- [x] T003 [P] Create EmailSettings configuration class in source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs
- [x] T004 [P] Create EmailTemplates helper class in source/MoriiCoffee.Infrastructure/Services/Email/EmailTemplates.cs
- [x] T005 Bind EmailSettings in source/MoriiCoffee.Infrastructure/Configurations/SettingsConfiguration.cs (ConfigureSettings method)
- [x] T006 Configure Brevo API key via user secrets (dotnet user-secrets set "EmailSettings:Brevo:ApiKey" "your-key")

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core email service infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T007 Create BrevoEmailService implementation in source/MoriiCoffee.Infrastructure/Services/Email/BrevoEmailService.cs
- [x] T008 Update DI registration in source/MoriiCoffee.Infrastructure/DependencyInjection.cs (replace EmailService with BrevoEmailService)
- [x] T009 Delete old stub EmailService.cs from source/MoriiCoffee.Infrastructure/Services/EmailService.cs
- [x] T010 Mark email templates as embedded resources in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj (add EmbeddedResource ItemGroup)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - New User Welcome Email (Priority: P1) 🎯 MVP

**Goal**: Send welcome email to newly registered users with personalized content and storefront link

**Independent Test**: Create a test account via /api/Auth/signup and verify welcome email arrives within 1 minute with correct name and working storefront link

### Implementation for User Story 1

- [x] T011 [US1] Create welcome.html email template in source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/welcome.html (includes {{UserName}} and {{StorefrontUrl}} placeholders)
- [x] T012 [US1] Implement SendWelcomeEmailAsync in source/MoriiCoffee.Infrastructure/Services/Email/BrevoEmailService.cs (load template, replace placeholders, send via Brevo)
- [x] T013 [US1] Verify SignUpCommandHandler integration in source/MoriiCoffee.Application/Commands/Auth/SignUp/SignUpCommandHandler.cs (ensure fire-and-forget call exists)
- [x] T014 [US1] Build and run application - test signup flow via Swagger UI at http://localhost:8002/swagger
- [ ] T015 [US1] Verify welcome email received with correct personalization and storefront link works (MANUAL TEST - requires real Brevo API key)
- [ ] T016 [US1] Check application logs for successful MessageId from Brevo API (MANUAL TEST - requires real Brevo API key)
- [ ] T017 [US1] Test error handling - temporarily set invalid API key and verify signup still succeeds with error logged (MANUAL TEST)

**Checkpoint**: At this point, User Story 1 should be fully functional - users receive welcome emails after signup

---

## Phase 4: User Story 2 - Password Reset Email (Priority: P1)

**Goal**: Send password reset email with secure, time-limited reset link when users request password recovery

**Independent Test**: Request password reset via /api/Auth/forgot-password, verify email arrives with working reset link, confirm link navigates to frontend reset page

### Implementation for User Story 2

- [x] T018 [US2] Create password-reset.html email template in source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/password-reset.html (includes {{UserName}} and {{ResetUrl}} placeholders)
- [x] T019 [US2] Implement SendPasswordResetEmailAsync in source/MoriiCoffee.Infrastructure/Services/Email/BrevoEmailService.cs (lookup user, load template, replace placeholders, send via Brevo)
- [x] T020 [US2] Verify ForgotPasswordCommandHandler integration in source/MoriiCoffee.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs (ensure fire-and-forget call exists)
- [ ] T021 [US2] Test forgot password flow via Swagger UI - verify email received within 1 minute (MANUAL TEST - requires real Brevo API key)
- [ ] T022 [US2] Click reset password link in email and verify navigation to frontend reset page with token parameters (MANUAL TEST - requires real Brevo API key)
- [ ] T023 [US2] Check application logs for successful MessageId from Brevo API (MANUAL TEST - requires real Brevo API key)
- [ ] T024 [US2] Test error handling - verify forgot password succeeds even if email fails (MANUAL TEST)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - welcome emails on signup, reset emails on forgot password

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, deployment preparation, and final validation

- [x] T025 [P] Document Brevo setup instructions in deployment guide or README (sender verification, API key configuration) - Documented in summary files
- [ ] T026 [P] Verify all edge cases from spec.md (Brevo API unavailable, invalid sender, rate limits, missing templates, invalid recipient) (MANUAL TEST - requires real Brevo API key and production scenarios)
- [ ] T027 Test complete signup and password reset flows end-to-end with real email addresses (MANUAL TEST - requires real Brevo API key)
- [ ] T028 Verify production configuration checklist from quickstart.md (environment variables, verified sender, URLs) (MANUAL TEST - requires production environment)
- [x] T029 [P] Update summary documentation in docs/explainations/ per CLAUDE.md requirements (summary-email-service-VN.md and summary-email-service-ENG.md)
- [ ] T030 Run final validation - verify both user stories work independently and together (MANUAL TEST - requires real Brevo API key)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-4)**: All depend on Foundational phase completion
  - User Story 1 (Welcome Email): Can proceed after Foundational
  - User Story 2 (Password Reset): Can proceed after Foundational (independent of US1)
- **Polish (Phase 5)**: Depends on both user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - No dependencies on US1 (uses same BrevoEmailService foundation)

### Within Each Phase

**Phase 1 (Setup)**:
- T001 must complete first (NuGet package needed for compilation)
- T003, T004 can run in parallel [P] (different files)
- T005 depends on T003 (needs EmailSettings class)
- T002, T006 are independent setup tasks

**Phase 2 (Foundational)**:
- T007 depends on T001, T003, T004 (needs package, settings, helper)
- T008 depends on T007 (needs BrevoEmailService to exist)
- T009 can happen anytime after T008
- T010 is independent (project file update)

**Phase 3 (US1)**:
- T011 is independent (template creation)
- T012 depends on T011 (needs template to load)
- T013 is verification only
- T014-T017 are sequential verification steps

**Phase 4 (US2)**:
- T018 is independent (template creation)
- T019 depends on T018 (needs template to load)
- T020 is verification only
- T021-T024 are sequential verification steps

**Phase 5 (Polish)**:
- T025, T026, T029 can run in parallel [P]
- T027, T028, T030 are sequential validation steps

### Parallel Opportunities

- **Phase 1**: T003 and T004 can be written in parallel (different files)
- **Phase 2**: Limited parallelization (most tasks are sequential)
- **Phase 3 & 4**: User Story 1 and 2 can be implemented in parallel AFTER Phase 2 completes (different template files, different methods in same service)
- **Phase 5**: T025, T026, T029 documentation tasks can be done in parallel

---

## Parallel Example: After Foundational Phase

Once Phase 2 (Foundational) is complete, User Stories 1 and 2 can proceed in parallel:

```bash
# Team Member A: User Story 1 - Welcome Email
Task T011: Create welcome.html template
Task T012: Implement SendWelcomeEmailAsync
Task T013-T017: Verification

# Team Member B: User Story 2 - Password Reset Email
Task T018: Create password-reset.html template
Task T019: Implement SendPasswordResetEmailAsync
Task T020-T024: Verification

# Both can work simultaneously because:
# - Different template files (welcome.html vs password-reset.html)
# - Different methods in BrevoEmailService (SendWelcomeEmailAsync vs SendPasswordResetEmailAsync)
# - Different command handlers (SignUpCommandHandler vs ForgotPasswordCommandHandler)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: Foundational (T007-T010) - CRITICAL
3. Complete Phase 3: User Story 1 (T011-T017)
4. **STOP and VALIDATE**: Test welcome email flow independently
5. Deploy/demo if ready - users can receive welcome emails on signup

### Incremental Delivery

1. Complete Setup + Foundational (Phase 1-2) → Foundation ready
2. Add User Story 1 (Phase 3) → Test independently → Deploy/Demo (MVP! - Welcome emails working)
3. Add User Story 2 (Phase 4) → Test independently → Deploy/Demo (Full feature - Welcome + Password reset emails)
4. Add Polish (Phase 5) → Production ready with documentation

### Parallel Team Strategy

With two developers:

1. Both complete Phase 1 (Setup) together - ~30 minutes
2. Both complete Phase 2 (Foundational) together - ~1 hour
3. Once Foundational is done:
   - **Developer A**: User Story 1 (Welcome Email) - ~45 minutes
   - **Developer B**: User Story 2 (Password Reset) - ~45 minutes
4. Both complete Phase 5 (Polish) together - ~30 minutes

**Total Time**: ~2-3 hours with parallel execution vs 3-4 hours sequential

---

## Verification Checklist

After completing all tasks, verify:

**User Story 1 (Welcome Email)**:
- [ ] Signup creates account successfully
- [ ] Welcome email received within 1 minute
- [ ] Email contains user's name
- [ ] Storefront link works and navigates to frontend
- [ ] Log shows MessageId from Brevo
- [ ] Signup succeeds even if email fails (tested with invalid API key)

**User Story 2 (Password Reset)**:
- [ ] Forgot password request succeeds
- [ ] Reset email received within 1 minute
- [ ] Email contains user's name (or fallback "there")
- [ ] Reset link works and includes token + email parameters
- [ ] Reset link navigates to frontend reset page
- [ ] Log shows MessageId from Brevo
- [ ] Forgot password succeeds even if email fails

**Cross-Story Integration**:
- [ ] Both email types work independently
- [ ] Logs clearly distinguish welcome vs reset emails
- [ ] Fire-and-forget pattern works for both flows
- [ ] Error handling consistent across both stories

**Production Readiness**:
- [ ] Brevo sender email verified in dashboard
- [ ] API key configured via environment variables (not committed)
- [ ] Storefront and reset URLs point to correct environments
- [ ] Templates are embedded resources (not file system dependent)
- [ ] Documentation updated with setup instructions

---

## Success Criteria Mapping

| Success Criterion | Verification Tasks | Status |
|-------------------|-------------------|--------|
| **SC-001**: 99% welcome emails within 1 minute | T015, T027 | ⏳ Verify in implementation |
| **SC-002**: 99% reset emails within 1 minute | T021, T027 | ⏳ Verify in implementation |
| **SC-003**: 95% delivery success rate | T016, T023 (log MessageId), production monitoring | ⏳ Verify in production |
| **SC-004**: No blocking of user operations | T017, T024 (error handling tests) | ⏳ Verify in implementation |
| **SC-005**: Reduced support tickets | T016, T023 (MessageId tracking) | ⏳ Verify in production |

---

## Notes

- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[Story] label**: Maps task to specific user story (US1 or US2) for traceability
- **Each user story**: Independently completable and testable (can deploy US1 without US2)
- **Fire-and-forget pattern**: Already implemented in command handlers - just verify integration
- **Templates**: Use HTML samples from quickstart.md or customize as needed
- **Logging**: Serilog infrastructure already exists - service just needs to use ILogger
- **Error handling**: Catch all exceptions in SendAsync, log errors, never rethrow
- **Commit strategy**: Commit after each phase completion or after each user story
- **Stop at checkpoints**: Validate user stories independently before moving to next phase

---

## Task Count Summary

- **Phase 1 (Setup)**: 6 tasks
- **Phase 2 (Foundational)**: 4 tasks
- **Phase 3 (US1 - Welcome Email)**: 7 tasks
- **Phase 4 (US2 - Password Reset)**: 7 tasks
- **Phase 5 (Polish)**: 6 tasks

**Total**: 30 tasks

**Parallel Opportunities**: 5 tasks can run in parallel (marked with [P])
- T003, T004 in Phase 1
- T025, T026, T029 in Phase 5

**Independent User Stories**: 2 stories can be implemented in parallel after Foundational phase (Phase 3 and Phase 4)

**Suggested MVP Scope**: Phase 1 + Phase 2 + Phase 3 (Setup + Foundational + Welcome Email) = ~2 hours implementation

**Full Feature**: All 30 tasks = ~3-4 hours with sequential execution, ~2-3 hours with parallel team
