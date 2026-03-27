# Tasks: Remove AWS SES Email Provider Support

**Input**: Design documents from `/specs/002-remove-aws-ses/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Not applicable - this is a refactoring task. Verification uses existing email functionality tests from feature 001-email-social-auth.

**Organization**: Tasks are grouped by user story to enable independent verification of each outcome (simplified configuration, cleaner codebase, no service disruption).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

This is a .NET Clean Architecture project:
- **Source code**: `source/MoriiCoffee.{Layer}/`
- **Configuration**: `source/MoriiCoffee.Presentation/appsettings*.json`
- **Specs**: `specs/002-remove-aws-ses/`

---

## Phase 1: Setup (Pre-Implementation Verification)

**Purpose**: Verify current state and prepare for AWS SES removal

- [X] T001 Verify current branch is 002-remove-aws-ses and working directory is clean
- [ ] T002 Verify SendGrid email functionality works (baseline test before changes) - SKIPPED (requires running app)
- [X] T003 Document current EmailSettings.cs structure for rollback reference

**Checkpoint**: Baseline established - ready to begin refactoring

---

## Phase 2: Foundational (Not Applicable)

**Purpose**: N/A - This refactoring has no blocking prerequisites

**Note**: This feature has no foundational phase. All tasks can proceed directly after Setup.

---

## Phase 3: User Story 1 - Simplified Email Configuration (Priority: P1) 🎯

**Goal**: Developers configure only SendGrid credentials without AWS SES options cluttering configuration files

**Independent Test**: Deploy application with SendGrid-only configuration, start successfully, send welcome and password reset emails, verify delivery without AWS-related errors/warnings in logs

### Implementation for User Story 1

- [X] T004 [US1] Remove Provider property from EmailSettings class in source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs (line 14)
- [X] T005 [US1] Remove AwsSes property from EmailSettings class in source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs (line 38)
- [X] T006 [US1] Remove AwsSesOptions class entirely from source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs (lines 49-59)
- [X] T007 [US1] Update XML documentation comment for EmailSettings class to remove Provider selection reference (line 25)
- [X] T008 [US1] Update XML documentation comment for SendGrid property to remove "Required when Provider is SendGrid" text (line 53)
- [X] T009 [US1] Simplify IEmailService registration in source/MoriiCoffee.Infrastructure/DependencyInjection.cs by replacing factory pattern (lines 44-52) with direct registration: services.AddScoped<IEmailService, SendGridEmailService>()

**Checkpoint**: EmailSettings simplified to SendGrid-only - verify application builds without errors

---

## Phase 4: User Story 2 - Cleaner Codebase (Priority: P2)

**Goal**: AWS SES code completely removed from codebase for better maintainability

**Independent Test**: Perform global code search for "AwsSes", "AWS.*SES", "SesOptions" and confirm zero results in source/ directory

### Verification for User Story 2

- [X] T010 [P] [US2] Run global code search for "AwsSes" in source/ directory and verify zero results
- [X] T011 [P] [US2] Run global code search for "AWS.*SES" in source/ directory and verify zero results
- [X] T012 [P] [US2] Run global code search for "SesOptions" in source/ directory and verify zero results
- [X] T013 [US2] Verify MoriiCoffee.Infrastructure.csproj contains no AWS SES NuGet packages
- [X] T014 [US2] Review DependencyInjection.cs and confirm only SendGridEmailService registration exists (no provider switch logic)
- [X] T015 [US2] Review EmailSettings.cs and confirm only SendGrid-related properties remain

**Checkpoint**: Codebase is clean - zero AWS SES references remain

---

## Phase 5: User Story 3 - No Email Service Disruption (Priority: P1) 🎯

**Goal**: Email delivery continues working identically to pre-refactoring state (zero regression)

**Independent Test**: Execute signup and forgot-password flows from feature 001 test scenarios, verify welcome and password reset emails delivered with identical content/timing/branding

### Build & Runtime Verification for User Story 3

- [X] T016 [US3] Run dotnet build on MoriiCoffee solution and verify clean compilation (zero errors, zero warnings)
- [ ] T017 [US3] Start application with appsettings.Development.json (SendGrid configuration) and verify clean startup (no configuration errors)
- [ ] T018 [US3] Verify application logs show "Email service: SendGridEmailService registered" or similar confirmation
- [ ] T019 [US3] Send POST request to /api/v1/auth/signup with test user credentials
- [ ] T020 [US3] Verify welcome email delivered to test user inbox with identical branding/content to pre-refactoring
- [ ] T021 [US3] Check application logs for welcome email send confirmation (no errors or warnings)
- [ ] T022 [US3] Send POST request to /api/v1/auth/forgot-password with test user email
- [ ] T023 [US3] Verify password reset email delivered to test user inbox with identical content to pre-refactoring
- [ ] T024 [US3] Check application logs for password reset email send confirmation (no errors or warnings)
- [ ] T025 [US3] Compare current logs with baseline logs from T002 and confirm no new error types introduced

**Checkpoint**: Email functionality verified - 100% functional parity confirmed

---

## Phase 6: Polish & Documentation

**Purpose**: Update documentation to reflect simplified SendGrid-only architecture

- [ ] T026 [P] Update specs/001-email-social-auth/quickstart.md to remove AwsSes provider option from configuration documentation
- [ ] T027 [P] Update specs/summaries/summary-email-service-integration-VN.md to clarify AWS SES was planned but never implemented
- [ ] T028 [P] Update specs/summaries/summary-email-service-integration-ENG.md to clarify AWS SES was planned but never implemented
- [ ] T029 Create summary documentation files: docs/explainations/summary-aws-ses-removal-VN.md
- [ ] T030 Create summary documentation files: docs/explainations/summary-aws-ses-removal-ENG.md
- [ ] T031 Review and validate quickstart.md instructions for this feature match actual implementation
- [ ] T032 Run final smoke test per quickstart.md verification section

**Checkpoint**: Documentation complete and accurate

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: N/A (skipped for this refactoring)
- **User Story 1 (Phase 3)**: Depends on Setup completion only
- **User Story 2 (Phase 4)**: Depends on User Story 1 completion (needs code changes from US1 to verify)
- **User Story 3 (Phase 5)**: Depends on User Story 1 completion (needs code changes from US1 to test)
- **Polish (Phase 6)**: Depends on all user stories being verified

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies - can start after Setup
- **User Story 2 (P2)**: Depends on US1 (verifies US1 changes were complete)
- **User Story 3 (P1)**: Depends on US1 (tests US1 changes work correctly)

**Critical Path**: Setup → US1 (code changes) → US2 (verification) + US3 (testing) → Polish

### Within Each User Story

**User Story 1** (sequential - modifying same file):
- T004-T008: EmailSettings.cs modifications (sequential edits to same file)
- T009: DependencyInjection.cs modification (can run after T004-T008 complete)

**User Story 2** (parallel verification):
- T010-T012: Code searches (all can run in parallel - different search patterns)
- T013-T015: Code reviews (all can run in parallel - different files)

**User Story 3** (sequential testing):
- T016-T018: Build and startup verification (sequential - must confirm app runs before testing)
- T019-T021: Welcome email test (sequential - send, verify delivery, check logs)
- T022-T024: Password reset email test (can run after welcome test OR in parallel with different test user)
- T025: Log comparison (runs last - needs all tests complete)

### Parallel Opportunities

**Limited parallelization due to refactoring nature:**

- Setup tasks (T001-T003): Can run in parallel if using different terminals/tools
- US2 verification tasks (T010-T015): Can all run in parallel after US1 complete
- Polish documentation tasks (T026-T028): Can all run in parallel
- Summary documentation (T029-T030): Can run in parallel (Vietnamese vs English)

**NOT parallelizable:**
- US1 code changes (T004-T009): Must be sequential (modifying same files)
- US3 tests (T016-T025): Mostly sequential (app must run before testing, tests must complete before log comparison)

---

## Parallel Example: User Story 2 Verification

```bash
# After US1 code changes complete, launch all US2 verification tasks together:

# Terminal 1:
Task T010: "Run rg 'AwsSes' source/ and verify zero results"

# Terminal 2:
Task T011: "Run rg 'AWS.*SES' source/ and verify zero results"

# Terminal 3:
Task T012: "Run rg 'SesOptions' source/ and verify zero results"

# Terminal 4:
Task T013: "Review MoriiCoffee.Infrastructure.csproj for AWS SES packages"

# Terminal 5:
Task T014: "Review DependencyInjection.cs for provider switch logic"

# Terminal 6:
Task T015: "Review EmailSettings.cs for AWS SES properties"
```

---

## Implementation Strategy

### Sequential Refactoring (Recommended)

This is a simple refactoring with tight file dependencies. Best executed sequentially:

1. **Phase 1: Setup** (T001-T003) - Establish baseline
2. **Phase 3: User Story 1** (T004-T009) - Make all code changes
3. **Phase 4: User Story 2** (T010-T015) - Verify changes are complete (parallel verification)
4. **Phase 5: User Story 3** (T016-T025) - Verify changes work correctly
5. **Phase 6: Polish** (T026-T032) - Update documentation

**Total Time Estimate**: 1-2 hours for experienced developer

### Checkpoint-Driven Validation

**Critical Checkpoints**:

1. **After Setup**: Baseline email functionality works (T002 passes)
2. **After US1**: Application builds without errors (T016 prerequisite)
3. **After US2**: Zero AWS SES code remains in codebase (T010-T015 all pass)
4. **After US3**: Email delivery works identically to baseline (T019-T024 all pass)
5. **After Polish**: Documentation accurate and complete

**Rollback Points**:
- If T016 fails: Revert US1 changes using T003 documentation
- If T019-T024 fail: Compare with T002 baseline, identify regression

### MVP Definition

For this refactoring, "MVP" means:

- **Minimum**: US1 + US3 complete (code changes + functional verification)
- **US2** is optional polish (code search verification)
- **Phase 6** is optional (documentation updates)

**Reasoning**: US1 delivers the code changes. US3 proves no regression. US2 just double-checks completeness. Documentation can be updated later.

---

## Verification Checklist (Definition of Done)

Before marking feature complete, verify:

### Code Changes (US1)
- [ ] EmailSettings.Provider property removed
- [ ] EmailSettings.AwsSes property removed
- [ ] AwsSesOptions class removed entirely
- [ ] XML documentation updated to remove AWS SES references
- [ ] DependencyInjection uses direct registration (not factory pattern)

### Code Cleanliness (US2)
- [ ] Zero results for "AwsSes" in source code search
- [ ] Zero results for "AWS.*SES" in source code search (excluding AWS S3 file storage)
- [ ] Zero results for "SesOptions" in source code search
- [ ] No AWS SES NuGet packages in Infrastructure.csproj

### Functional Verification (US3)
- [ ] Application builds successfully (dotnet build)
- [ ] Application starts successfully with SendGrid configuration
- [ ] Welcome email sends and delivers identically to pre-refactoring
- [ ] Password reset email sends and delivers identically to pre-refactoring
- [ ] Logs show no new errors or warnings

### Documentation
- [ ] Feature 001 quickstart.md updated to remove AWS SES references
- [ ] Summary docs updated to clarify AWS SES was never implemented
- [ ] Vietnamese summary documentation created
- [ ] English summary documentation created

### Git Hygiene
- [ ] Atomic commits per logical change (e.g., "Remove Provider property from EmailSettings")
- [ ] Descriptive commit messages explaining "why" not just "what"
- [ ] All changes on 002-remove-aws-ses branch

---

## Notes

- This is a **refactoring task** (removing unused code), not a feature addition
- **Zero risk**: AWS SES was never implemented, so removal cannot break production
- **No database migrations** required (email config is file-based, not persisted)
- **No API changes** (IEmailService interface unchanged)
- **Backward compatible**: Existing SendGrid configurations continue working
- **Optional cleanup**: Configuration files with Provider/AwsSes fields will ignore those fields (harmless)
- Focus on **verification**: US3 is the most critical phase (prove no regression)
- Use **quickstart.md** as the verification guide for T019-T024 (email delivery tests)

---

## Success Criteria

**This feature is complete when**:

1. ✅ EmailSettings contains only SendGrid properties (no Provider, no AwsSes, no AwsSesOptions)
2. ✅ DependencyInjection registers SendGridEmailService directly (no provider switch)
3. ✅ Global code search for AWS SES terms returns zero results in source/
4. ✅ Application builds and runs successfully
5. ✅ Welcome and password reset emails deliver identically to pre-refactoring state
6. ✅ Logs show no new errors or warnings
7. ✅ Summary documentation files created in docs/explainations/ (Vietnamese + English)

**Acceptance**: All checkpoints pass, all verification tasks (T010-T025) confirm expected outcomes.
