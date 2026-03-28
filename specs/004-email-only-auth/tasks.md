# Tasks: Restrict Authentication Identity to Email Only

**Input**: Design documents from `/specs/004-email-only-auth/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests are NOT requested for this feature. All verification is done through manual testing per quickstart.md.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

This is a Clean Architecture .NET 8 project with the following structure:
- `source/MoriiCoffee.Application/` - Commands, Queries, Validators
- `source/MoriiCoffee.Presentation/` - Controllers
- `docs/` - API documentation

---

## Phase 1: Setup (Prerequisites Verification)

**Purpose**: Verify current implementation and confirm no foundational changes needed

- [X] T001 [P] Read current SignInCommandValidator.cs at source/MoriiCoffee.Application/Commands/Auth/SignIn/SignInCommandValidator.cs to understand existing validation
- [X] T002 [P] Read current SignInCommandHandler.cs at source/MoriiCoffee.Application/Commands/Auth/SignIn/SignInCommandHandler.cs to understand identity lookup logic
- [X] T003 [P] Read SignUpCommandValidator.cs at source/MoriiCoffee.Application/Commands/Auth/SignUp/SignUpCommandValidator.cs to confirm email validation pattern

**Checkpoint**: Current implementation understood - ready for user story implementation

---

## Phase 2: User Story 1 - Sign In with Email Only (Priority: P1) 🎯 MVP

**Goal**: Restrict sign-in to email-only identity. Phone numbers must be rejected with clear validation error.

**Independent Test**: Sign in with email succeeds, sign in with phone number fails with 400 Bad Request error message

### Implementation for User Story 1

- [X] T004 [US1] Update SignInCommandValidator.cs at source/MoriiCoffee.Application/Commands/Auth/SignIn/SignInCommandValidator.cs to add email format validation using FluentValidation .EmailAddress() rule
- [X] T005 [US1] Update SignInCommandHandler.cs at source/MoriiCoffee.Application/Commands/Auth/SignIn/SignInCommandHandler.cs to remove phone number lookup from LINQ query (change from `u.Email == request.Identity || u.PhoneNumber == request.Identity` to `u.Email == request.Identity`)
- [X] T006 [US1] Update XML documentation in SignInCommand.cs at source/MoriiCoffee.Application/Commands/Auth/SignIn/SignInCommand.cs to document that Identity field must be email only
- [X] T007 [US1] Update XML documentation in AuthController.cs at source/MoriiCoffee.Presentation/Controllers/AuthController.cs for SignIn endpoint to note breaking change (phone numbers no longer accepted)
- [X] T008 [US1] Build project using `dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental` to verify no compilation errors
- [ ] T009 [US1] Start development environment using `bash deploy/run-docker-development.sh` and verify API starts on http://localhost:8002
- [ ] T010 [US1] Manual test: Sign in with valid email + correct password via Swagger UI → Should succeed with 200 OK and tokens returned (Test #1 from quickstart.md)
- [ ] T011 [US1] Manual test: Sign in with phone number + correct password via Swagger UI → Should fail with 400 Bad Request and error message "Invalid email format" (Test #2 from quickstart.md)
- [ ] T012 [US1] Manual test: Sign in with invalid email format via Swagger UI → Should fail with 400 Bad Request and error message (Test #3 from quickstart.md)
- [ ] T013 [US1] Manual test: Sign in with correct email but wrong password → Should fail with 401 Unauthorized and generic error message (Test #4 from quickstart.md)
- [ ] T014 [US1] Manual test: Sign in with non-existent email → Should fail with 401 Unauthorized with same generic error as wrong password (Test #5 from quickstart.md)

**Checkpoint**: User Story 1 complete - Email-only sign-in enforced and tested

---

## Phase 3: User Story 2 - Sign Up with Email as Primary Identity (Priority: P2)

**Goal**: Verify that sign-up already requires email as primary identity and that newly created users cannot sign in with phone number

**Independent Test**: Create new account with email, then attempt sign-in with phone number (should fail)

### Verification for User Story 2

- [ ] T015 [US2] Read SignUpCommandHandler.cs at source/MoriiCoffee.Application/Commands/Auth/SignUp/SignUpCommandHandler.cs to verify it already creates email-based accounts (no changes needed)
- [ ] T016 [US2] Manual test: Register new user with email and phone number via Swagger UI → Should succeed with 200 OK (Test #10 from quickstart.md)
- [ ] T017 [US2] Manual test: Attempt to sign in with newly registered user's phone number → Should fail with 400 Bad Request (validates US1 changes apply to new users)
- [ ] T018 [US2] Manual test: Sign in with newly registered user's email → Should succeed with 200 OK (validates email-based auth works for new users)
- [ ] T019 [US2] Manual test: Attempt to register duplicate email → Should fail with appropriate error message

**Checkpoint**: User Story 2 complete - Sign-up verified to use email as primary identity

---

## Phase 4: User Story 3 - Maintain Phone Number as Profile Field (Priority: P3)

**Goal**: Verify that phone numbers remain visible in user profiles but cannot be used for authentication

**Independent Test**: Retrieve user profile, see phone number present, attempt to sign in with it (should fail)

### Verification for User Story 3

- [ ] T020 [US3] Manual test: Sign in with valid email to get access token
- [ ] T021 [US3] Manual test: Retrieve user profile via GET /api/v1/users/me with Bearer token → Should return 200 OK with phoneNumber field present (Test #8 from quickstart.md)
- [ ] T022 [US3] Manual test: Attempt to sign in with phone number from profile → Should fail with 400 Bad Request (validates phone is profile-only)

**Checkpoint**: User Story 3 complete - Phone numbers remain in profiles but cannot be used for authentication

---

## Phase 5: Related Authentication Flows Verification

**Goal**: Verify that other authentication endpoints (forgot password, refresh token) continue to work correctly and don't rely on phone number identity

**Independent Test**: Execute forgot password and refresh token flows successfully

### Verification Tasks

- [ ] T023 [P] Read ForgotPasswordCommandHandler.cs at source/MoriiCoffee.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs to verify it uses email-only lookup (no changes needed)
- [ ] T024 [P] Read RefreshTokenCommandHandler.cs at source/MoriiCoffee.Application/Commands/Auth/RefreshToken/RefreshTokenCommandHandler.cs to verify it uses JWT sub claim, not identity lookup (no changes needed)
- [ ] T025 Manual test: Forgot password with email via Swagger UI → Should succeed with 200 OK (Test #6 from quickstart.md)
- [ ] T026 Manual test: Refresh token flow using expired access token and valid refresh token → Should succeed with 200 OK and new token pair (Test #7 from quickstart.md)
- [ ] T027 Manual test: Case-insensitive email sign-in → Should succeed with 200 OK (Test #9 from quickstart.md)

**Checkpoint**: All auth flows verified to work correctly without phone number identity

---

## Phase 6: Documentation & Polish

**Purpose**: Update documentation and create summary files per CLAUDE.md workflow

- [ ] T028 [P] Update API documentation at docs/api/auth-api-structure.md to reflect that identity field accepts email only (if this file exists)
- [ ] T029 Create Vietnamese summary document at docs/explainations/summary-email-only-auth-VN.md documenting all changes, files modified, business rules, and verification steps
- [ ] T030 Create English summary document at docs/explainations/summary-email-only-auth-ENG.md documenting all changes, files modified, business rules, and verification steps
- [ ] T031 Final build verification: Run `dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental` to confirm 0 errors and 0 warnings
- [ ] T032 Git commit: Create atomic commit with descriptive message explaining email-only authentication change

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **User Story 1 (Phase 2)**: Depends on Setup completion - This is the MVP
- **User Story 2 (Phase 3)**: Depends on User Story 1 completion (needs email validation to be in place)
- **User Story 3 (Phase 4)**: Depends on User Story 1 completion (needs sign-in rejection to test)
- **Related Flows (Phase 5)**: Can run in parallel with User Story 3 after User Story 1 complete
- **Documentation (Phase 6)**: Depends on all implementation and testing complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Setup (Phase 1) - No dependencies on other stories - **THIS IS THE MVP**
- **User Story 2 (P2)**: Depends on User Story 1 (needs email-only validation in place to test new user flow)
- **User Story 3 (P3)**: Depends on User Story 1 (needs phone rejection to verify profile-only behavior)

### Within Each User Story

**User Story 1**:
1. T001-T003 (Setup reads) can run in parallel
2. T004-T007 (Implementation) must run sequentially (T004→T005→T006→T007)
3. T008 (Build) must complete before T009 (Start environment)
4. T010-T014 (Tests) can run in any order after T009

**User Story 2**:
1. T015 (Read handler) runs first
2. T016-T019 (Manual tests) run sequentially

**User Story 3**:
1. T020-T022 run sequentially (need token from T020 for T021)

**Related Flows Verification**:
1. T023-T024 (Reads) can run in parallel
2. T025-T027 (Manual tests) can run in any order

**Documentation**:
1. T028 runs independently
2. T029-T030 can run in parallel
3. T031-T032 run sequentially at the end

### Parallel Opportunities

- **Phase 1**: T001, T002, T003 can all run in parallel (different files)
- **Phase 5**: T023, T024 can run in parallel (read-only, different files)
- **Phase 6**: T028, T029, T030 can run in parallel (different documentation files)

---

## Parallel Example: User Story 1 Setup

```bash
# Launch all setup reads together:
Task: "Read SignInCommandValidator.cs"
Task: "Read SignInCommandHandler.cs"
Task: "Read SignUpCommandValidator.cs"

# These can be read in parallel since they're just information gathering
# and don't modify any files
```

---

## Parallel Example: Related Flows Verification

```bash
# Launch verification reads together:
Task: "Read ForgotPasswordCommandHandler.cs"
Task: "Read RefreshTokenCommandHandler.cs"

# These are independent reads that don't depend on each other
```

---

## Parallel Example: Documentation

```bash
# Launch documentation tasks together:
Task: "Update API documentation at docs/api/auth-api-structure.md"
Task: "Create Vietnamese summary at docs/explainations/summary-email-only-auth-VN.md"
Task: "Create English summary at docs/explainations/summary-email-only-auth-ENG.md"

# These write to different files and can be done in parallel
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: User Story 1 (T004-T014)
3. **STOP and VALIDATE**: Test User Story 1 independently using quickstart.md tests
4. If successful: MVP is done! Email-only authentication is enforced.

### Incremental Delivery

1. Complete Setup (Phase 1) → Foundation ready
2. Add User Story 1 (Phase 2) → Test independently → **Deploy/Demo (MVP!)**
3. Add User Story 2 (Phase 3) → Test independently → Confirm new users work
4. Add User Story 3 (Phase 4) → Test independently → Confirm profiles intact
5. Verify Related Flows (Phase 5) → Test independently → Confirm no regressions
6. Add Documentation (Phase 6) → Complete feature

### Single Developer Strategy

Recommended execution order:

1. **Day 1**: Setup + User Story 1 implementation (T001-T007)
2. **Day 1**: Build and test User Story 1 (T008-T014)
3. **Day 2**: Verify User Story 2 (T015-T019)
4. **Day 2**: Verify User Story 3 (T020-T022)
5. **Day 3**: Verify Related Flows (T023-T027)
6. **Day 3**: Documentation and commit (T028-T032)

---

## Task Summary

**Total Tasks**: 32

### By Phase:
- Phase 1 (Setup): 3 tasks
- Phase 2 (User Story 1 - P1 - MVP): 11 tasks
- Phase 3 (User Story 2 - P2): 5 tasks
- Phase 4 (User Story 3 - P3): 3 tasks
- Phase 5 (Related Flows): 5 tasks
- Phase 6 (Documentation): 5 tasks

### By Type:
- Implementation changes: 4 tasks (T004-T007)
- Verification reads: 5 tasks (T001-T003, T015, T023-T024)
- Build/environment: 2 tasks (T008-T009)
- Manual testing: 16 tasks (T010-T014, T016-T019, T020-T022, T025-T027)
- Documentation: 4 tasks (T028-T030, T032)
- Final verification: 1 task (T031)

### Parallel Opportunities:
- 8 tasks can run in parallel: T001, T002, T003 (Setup), T023, T024 (Verification), T028, T029, T030 (Documentation)

### Critical Path (Minimum for MVP):
- T001-T003 (Setup): ~15 minutes
- T004-T009 (Implementation + Build): ~45 minutes
- T010-T014 (Core testing): ~30 minutes
- **Total MVP Time**: ~90 minutes

---

## Notes

- **No migration needed**: Phone number field remains in database unchanged
- **Breaking change**: Clients using phone-based sign-in will receive 400 Bad Request
- **Rollback is simple**: Revert T004 and T005 changes only (no database rollback)
- **Build must pass**: Run `dotnet build --no-incremental` after changes (T008, T031)
- **Manual testing required**: No automated test suite exists (per plan.md)
- **Summary docs required**: Must create VN + ENG summaries before marking complete (per CLAUDE.md)
- **[P] tasks**: Different files, no dependencies, can run in parallel
- **[Story] labels**: Map tasks to user stories for traceability
- **Each user story should be independently completable and testable**
- **Commit after completing each phase** or logical group of tasks
- **Stop at any checkpoint** to validate story independently
