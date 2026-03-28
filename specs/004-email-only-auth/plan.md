# Implementation Plan: Restrict Authentication Identity to Email Only

**Branch**: `004-email-only-auth` | **Date**: 2026-03-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-email-only-auth/spec.md`

## Summary

The authentication system currently accepts both email and phone number as valid identity values for sign-in. This plan removes phone number as an authentication identity, restricting sign-in to email only. Phone number remains a profile field but cannot be used for authentication. The change affects SignInCommand validation, user lookup logic, and related auth flows (forgot password, refresh token).

## Technical Context

**Language/Version**: C# / .NET 8.0
**Primary Dependencies**: ASP.NET Core Identity, MediatR, FluentValidation, EF Core
**Storage**: SQL Server (via Entity Framework Core)
**Testing**: N/A (no test suite currently exists)
**Target Platform**: Linux/Windows server (Docker)
**Project Type**: Web API (Clean Architecture)
**Performance Goals**: <200ms p95 for authentication endpoints
**Constraints**: Must maintain backward compatibility for existing user data; breaking API change for clients
**Scale/Scope**: Affects 5 command handlers, 2 validators, API documentation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Plan Mode Default
- **Status**: PASS
- **Evidence**: Using `/speckit.plan` command with detailed technical context gathering

### ✅ II. Verification Before Done
- **Status**: PASS
- **Plan**: Will test sign-in with email (should succeed), sign-in with phone (should fail 400), verify all auth flows function correctly

### ✅ III. Simplicity First & Minimal Impact
- **Status**: PASS
- **Evidence**: Changes limited to:
  - SignInCommandValidator (add email format validation)
  - SignInCommandHandler (remove phone number lookup)
  - No new abstractions or patterns introduced
  - Phone number field remains unchanged in User entity (no migration needed)

### ⚠️ IV. Subagent Strategy & Delegation
- **Status**: PASS
- **Evidence**: Used Explore subagent to research current auth implementation

### ⚠️ V. Self-Improvement Loop
- **Status**: N/A (no corrections yet)
- **Plan**: Will update `tasks/lessons.md` if corrected during implementation

### ✅ VI. Autonomous Execution with Concise Communication
- **Status**: PASS
- **Plan**: Will fix issues directly, verify with build + manual testing

## Project Structure

### Documentation (this feature)

```text
specs/004-email-only-auth/
├── plan.md              # This file
├── research.md          # Phase 0 output (validation patterns, breaking change strategy)
├── data-model.md        # Phase 1 output (User entity analysis)
├── quickstart.md        # Phase 1 output (testing guide)
├── contracts/           # Phase 1 output (API request/response contracts)
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Domain/
│   └── Aggregates/
│       └── UserAggregate/
│           └── User.cs                         # NO CHANGES (phone remains as profile field)
│
├── MoriiCoffee.Application/
│   └── Commands/
│       └── Auth/
│           ├── SignIn/
│           │   ├── SignInCommand.cs            # UPDATE: Document identity = email only
│           │   ├── SignInCommandValidator.cs   # UPDATE: Add email format validation
│           │   └── SignInCommandHandler.cs     # UPDATE: Remove phone lookup logic
│           ├── SignUp/
│           │   └── SignUpCommandHandler.cs     # VERIFY: Already email-based (no changes)
│           ├── ForgotPassword/
│           │   └── ForgotPasswordCommandHandler.cs  # VERIFY: Already email-only (no changes)
│           └── RefreshToken/
│               └── RefreshTokenCommandHandler.cs    # VERIFY: Uses userId from JWT (no changes)
│
├── MoriiCoffee.Presentation/
│   └── Controllers/
│       └── AuthController.cs                   # UPDATE: XML doc comments for breaking change
│
└── docs/
    └── api/
        └── auth-api-structure.md              # UPDATE: Document identity = email only
```

**Structure Decision**: This is a Clean Architecture web API with clear layer separation. Changes are concentrated in the Application layer (command validation and handlers) with documentation updates in Presentation. No Domain or Infrastructure changes required since phone number remains a valid profile field.

## Complexity Tracking

No constitutional violations. This change reduces complexity by removing dual-identity lookup logic.

---

## Phase 0: Research & Unknowns Resolution

### Research Questions

1. **Email Validation Pattern**: What email validation pattern should be used in SignInCommandValidator?
   - Current system uses FluentValidation's `.EmailAddress()` rule in SignUpCommandValidator
   - Same pattern should be applied to SignInCommandValidator for consistency

2. **Error Message Strategy**: What error message should be shown when a user attempts to sign in with a phone number?
   - Options:
     - Generic: "Invalid email format"
     - Specific: "Email address required for sign-in"
     - Educational: "Sign-in with email only. Phone numbers are no longer supported."
   - Decision needed on information disclosure vs user experience

3. **Backward Compatibility**: Should there be any grace period or migration notice?
   - Assumption: API documentation update and client notification handled separately (out of scope per spec)
   - Implementation: Hard cutover (immediate rejection of phone-based sign-in)

4. **Testing Strategy**: How to verify the change without an existing test suite?
   - Manual testing via Swagger UI / Postman
   - Test scenarios:
     - Sign in with valid email + password (should succeed)
     - Sign in with phone number + password (should fail 400)
     - Sign in with invalid email format (should fail 400)
     - Forgot password with email (should succeed)
     - Refresh token flow (should succeed)

### Research Output Location

All research findings will be consolidated in `specs/004-email-only-auth/research.md`

---

## Phase 1: Design Artifacts

### 1. Data Model Analysis (`data-model.md`)

Document the User entity structure and confirm:
- Email field: `string`, inherited from `IdentityUser<Guid>`, unique, required
- PhoneNumber field: `string`, inherited from `IdentityUser<Guid>`, optional, NOT used for auth after this change
- No schema changes required (phone number remains in database)
- No migration needed

### 2. API Contracts (`contracts/`)

Document request/response contracts for affected endpoints:

**SignInRequest** (`contracts/sign-in-request.md`):
```json
{
  "identity": "user@example.com",  // CHANGED: Must be valid email format
  "password": "SecurePass123!"
}
```

**Error Responses** (`contracts/error-responses.md`):
- 400 Bad Request when identity is not a valid email
- 401 Unauthorized when credentials are invalid
- Error message format and codes

### 3. Testing Quickstart (`quickstart.md`)

Step-by-step guide for manual testing:
1. Start the API (`bash deploy/run-docker-development.sh`)
2. Open Swagger UI
3. Test sign-in with email (existing user)
4. Test sign-in with phone number (should fail 400)
5. Test forgot password flow
6. Test refresh token flow
7. Verify UserProfile API still returns phone number

### 4. Agent Context Update

Run `.specify/scripts/bash/update-agent-context.sh claude` to update `CLAUDE.md` with:
- Breaking change note: sign-in identity now email-only
- Testing verification checklist
- Common error patterns to watch for

---

## Phase 2: Implementation Tasks

**Note**: Tasks are generated by `/speckit.tasks` command (not part of this plan document).

Expected task breakdown:
1. Research & validation (Phase 0 output)
2. Update SignInCommandValidator to enforce email format
3. Update SignInCommandHandler to remove phone number lookup
4. Update XML documentation in SignInCommand
5. Update AuthController XML comments to note breaking change
6. Update API documentation (`docs/api/auth-api-structure.md`)
7. Verify ForgotPasswordCommandHandler is email-only (no changes needed)
8. Verify RefreshTokenCommandHandler doesn't use phone (no changes needed)
9. Verify SignUpCommandHandler creates email-based accounts (no changes needed)
10. Build verification (`dotnet build --no-incremental`)
11. Manual testing (all scenarios in quickstart.md)
12. Document results in summary files (VN + ENG per CLAUDE.md workflow)

---

## Key Design Decisions

### Decision 1: Validation Layer (Validator vs Handler)

**Choice**: Add email format validation in `SignInCommandValidator`
**Rationale**: Fail fast at the validation layer rather than in the handler. Consistent with existing SignUp flow which validates email format in the validator.
**Alternative Rejected**: Handler-level validation would duplicate logic and delay error response.

### Decision 2: Error Message Specificity

**Choice**: "Invalid email format" for malformed input, "Invalid credentials" for failed authentication
**Rationale**: Prevents enumeration attacks (don't reveal whether email exists). Consistent with security best practices.
**Alternative Rejected**: Specific message "Phone numbers not supported" would reveal implementation details and aid attackers.

### Decision 3: User Lookup Strategy

**Choice**: Remove phone number from LINQ query entirely
**Current**: `u.Email == request.Identity || u.PhoneNumber == request.Identity`
**New**: `u.Email == request.Identity`
**Rationale**: Simplifies query, removes ambiguity, enforces email-only at data access layer.

### Decision 4: Phone Number Field Retention

**Choice**: Keep PhoneNumber in User entity and database
**Rationale**: Per spec, phone number is a valid profile field. No migration needed. Simpler implementation.
**Alternative Rejected**: Dropping the field would require migration, data loss, and is out of scope.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Existing users attempt phone-based sign-in | High | High | Clear error message + API documentation update (out of scope) |
| Clients cache phone-based auth | Medium | High | Version API or notify clients (out of scope) |
| Phone number still indexed for lookups | Low | Low | EF Core will optimize query; no functional impact |
| Forgot password flow breaks | Low | High | Verification testing (already email-only, no changes needed) |
| Refresh token flow breaks | Low | High | Verification testing (uses JWT sub claim, no identity lookup) |

---

## Dependencies

- None (isolated change within Application layer)

---

## Rollback Plan

If issues are discovered post-deployment:
1. Revert SignInCommandHandler changes (restore dual-lookup)
2. Revert SignInCommandValidator changes (remove email validation)
3. Redeploy previous version
4. No data migration rollback needed (no schema changes)

---

## Success Criteria Verification

Per spec.md, the implementation satisfies these criteria:

- **SC-001**: Sign-in with valid email processes correctly → Verified via manual testing
- **SC-002**: Sign-in with phone number rejected with 400 → Verified via manual testing
- **SC-003**: All auth endpoints function without phone lookup → Verified via ForgotPassword, RefreshToken testing
- **SC-004**: User profiles display phone numbers → Verified via GET /api/v1/users/me
- **SC-005**: API docs reflect email-only → Verified via docs/api/auth-api-structure.md update
