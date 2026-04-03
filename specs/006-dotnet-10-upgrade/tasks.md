# Tasks: .NET 10 Platform Upgrade

**Input**: Design documents from `/specs/006-dotnet-10-upgrade/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: NO automated tests exist in codebase. All verification is manual using quickstart.md checklist.

**Organization**: Tasks are grouped by user story (priority order: P1 → P2 → P3) to enable incremental delivery.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and research validation

- [x] T001 Verify all design documents are complete (plan.md, spec.md, research.md, data-model.md, quickstart.md)
- [x] T002 Verify Directory.Build.props exists and contains TargetFramework setting
- [x] T003 Capture baseline metrics (API response times, startup time, query performance) per research.md Section 8
- [x] T004 Create global.json with .NET 10 SDK version per research.md Section 6.4

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core verification before upgrade - MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Verify .NET 10 SDK installed (`dotnet --version` shows 10.0.xxx)
- [x] T006 Verify Docker Desktop supports .NET 10 base images (`docker pull mcr.microsoft.com/dotnet/sdk:10.0`)
- [x] T007 Verify SQL Server version supports EF Core 10 requirements (SQL Server 2022+ or Azure SQL) per research.md Section 1.1
- [x] T008 Backup current working state (git tag or branch snapshot for rollback)

**Checkpoint**: Foundation ready - user story implementation can now begin sequentially (P1 → P2 → P3)

---

## Phase 3: User Story 1 - Platform Runtime Upgrade (Priority: P1) 🎯 MVP

**Goal**: Upgrade all project target frameworks from .NET 8 to .NET 10 and update Docker base images to establish the upgraded runtime foundation.

**Independent Test**: Build all projects successfully with .NET 10, verify Docker containers build and start without errors. Run `cd deploy && bash run-docker-development.sh` and confirm all services initialize.

### Implementation for User Story 1

- [x] T009 [US1] Update TargetFramework from net8.0 to net10.0 in Directory.Build.props
- [x] T010 [US1] Update FROM mcr.microsoft.com/dotnet/sdk:8.0 to 10.0 in source/MoriiCoffee.Presentation/Dockerfile (dev stage)
- [x] T011 [US1] Update FROM mcr.microsoft.com/dotnet/aspnet:8.0 to 10.0 in source/MoriiCoffee.Presentation/Dockerfile (base stage)
- [x] T012 [US1] Update FROM mcr.microsoft.com/dotnet/sdk:8.0 to 10.0 in source/MoriiCoffee.Presentation/Dockerfile (build stage)
- [x] T013 [US1] Run `dotnet restore` to verify package restore with .NET 10 framework
- [x] T014 [US1] Run `dotnet build` to verify compilation succeeds with .NET 10 framework
- [x] T015 [US1] Run `cd deploy && bash run-docker-development.sh` to verify Docker containers build and start
- [x] T016 [US1] Verify all 6 projects build successfully (Domain.Shared, Domain, Application, Infrastructure.Persistence, Infrastructure, Presentation)
- [x] T017 [US1] Check Docker logs for startup errors (`docker logs moriicoffee.api`)
- [x] T018 [US1] Verify Swagger UI loads at http://localhost:8002/swagger

**Checkpoint**: At this point, all projects target .NET 10 and build successfully. Docker environment runs with .NET 10 runtime.

---

## Phase 4: User Story 2 - Dependency Package Upgrade (Priority: P2)

**Goal**: Upgrade all NuGet packages to their latest .NET 10 compatible versions, including critical Microsoft packages and third-party dependencies.

**Independent Test**: Run `dotnet restore` successfully without conflicts, build the solution, run the application, and verify all services (auth, email, database, file storage) initialize correctly.

### Critical Package Cleanup (MUST HAPPEN FIRST)

- [x] T019 [US2] Remove Microsoft.AspNetCore.Identity version 2.2.0 from source/MoriiCoffee.Application/MoriiCoffee.Application.csproj per research.md Section 1.1
- [x] T020 [US2] Remove Microsoft.AspNetCore.Http.Features version 2.2.0 from source/MoriiCoffee.Application/MoriiCoffee.Application.csproj per research.md Section 1.1
- [x] T021 [US2] Run `dotnet build` to verify Identity still works via shared framework

### Microsoft Package Updates

- [x] T022 [P] [US2] Update Microsoft.EntityFrameworkCore.Design from 8.0.10 to 10.0.5 in source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
- [x] T023 [P] [US2] Update Microsoft.EntityFrameworkCore.SqlServer from 8.0.10 to 10.0.5 in source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj
- [x] T024 [P] [US2] Update Microsoft.AspNetCore.Identity.EntityFrameworkCore from 8.0.10 to 10.0.5 in source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj
- [x] T025 [P] [US2] Update Microsoft.EntityFrameworkCore from 8.0.10 to 10.0.5 in source/MoriiCoffee.Domain/MoriiCoffee.Domain.csproj
- [x] T026 [P] [US2] Update Microsoft.AspNetCore.Authentication.JwtBearer from 8.0.10 to 10.0.5 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [x] T027 [P] [US2] Update Microsoft.AspNetCore.Authentication.Google from 8.0.0 to 10.0.5 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [x] T028 [P] [US2] Update Microsoft.AspNetCore.Mvc.NewtonsoftJson from 8.0.10 to 10.0.5 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [x] T029 [P] [US2] Update Microsoft.Extensions.DependencyInjection from 8.0.1 to 10.0.5 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [x] T030 [US2] Run `dotnet restore` and verify no package conflicts
- [x] T031 [US2] Run `dotnet build` and verify successful compilation

### Third-Party Package Updates (Standard)

- [x] T032 [P] [US2] Update Serilog from 4.0.1 to 4.3.1 in source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
- [x] T033 [P] [US2] Update Serilog.Settings.Configuration from 8.0.4 to 10.0.0 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj (implicit dependency)
- [x] T034 [P] [US2] Update Serilog.Sinks.Console from 6.0.0 to 6.1.1 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj (implicit dependency)
- [x] T035 [P] [US2] Update Serilog.Extensions.Hosting from 8.0.0 to 10.0.0 in source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
- [x] T036 [P] [US2] Update AWSSDK.S3 from 3.7.* to 4.0.20.2 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [x] T037 [P] [US2] Update Bogus from 35.6.0 to 35.6.5 in source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj
- [x] T038 [US2] Run `dotnet restore` and verify package restore succeeds
- [x] T039 [US2] Run `dotnet build` and verify no errors

### Third-Party Package Updates (Major Versions - Breaking Change Risk)

- [x] T040 [US2] Update Swashbuckle.AspNetCore from 6.7.2 to 10.1.7 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj (major version jump)
- [x] T041 [US2] Update Swashbuckle.AspNetCore.Annotations from 6.7.2 to 10.1.7 in source/MoriiCoffee.Application/MoriiCoffee.Application.csproj (major version jump)
- [x] T042 [US2] Update MediatR from 12.4.0 to 14.1.0 in source/MoriiCoffee.Application/MoriiCoffee.Application.csproj (major version jump)
- [x] T043 [US2] Update AutoMapper from 14.0.0 to 16.1.1 in source/MoriiCoffee.Application/MoriiCoffee.Application.csproj (major version jump)
- [x] T044 [US2] Update FluentValidation from 11.9.2 to 12.1.1 in source/MoriiCoffee.Domain/MoriiCoffee.Domain.csproj (major version jump)
- [x] T045 [US2] Update FluentValidation.AspNetCore from 11.3.0 to 11.3.1 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj (deprecated but still works)
- [x] T046 [US2] Update MicroElements.Swashbuckle.FluentValidation from 6.0.0 to 7.1.4 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj
- [x] T047 [US2] Update Minio from 6.0.3 to 7.0.0 in source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj (major version jump - requires thorough testing)
- [x] T048 [US2] Run `dotnet restore` and verify all packages restore successfully
- [x] T049 [US2] Run `dotnet build` and verify successful compilation with all major version updates

### Verification for User Story 2

- [x] T050 [US2] Run `cd deploy && bash run-docker-development.sh` and verify all containers start
- [x] T051 [US2] Check Docker logs for package-related warnings or errors (`docker logs moriicoffee.api`)
- [x] T052 [US2] Verify Swagger UI loads correctly (Swashbuckle major version)
- [x] T053 [US2] Test basic API endpoint to verify MediatR handlers work (major version)
- [x] T054 [US2] Test AutoMapper mappings work correctly (major version)
- [x] T055 [US2] Test FluentValidation validators work correctly (major version)

**Checkpoint**: At this point, all packages are upgraded to .NET 10 versions. Application builds and runs with new dependencies.

---

## Phase 5: User Story 3 - Breaking Change Resolution (Priority: P3)

**Goal**: Identify and resolve all breaking changes introduced in .NET 9/10, ensuring zero functional regression across authentication, database operations, email services, and file storage.

**Independent Test**: Run complete application through all existing features, verify no runtime exceptions, confirm all business logic behaves identically to pre-upgrade behavior, and validate manual test checklist passes.

### Breaking Change Detection

- [x] T056 [US3] Run `dotnet build` and capture all compiler warnings related to obsolete APIs or deprecations
- [x] T057 [US3] Review research.md Section 1 for breaking changes that require code modifications
- [x] T058 [US3] Analyze authentication middleware registration for .NET 10 compatibility per research.md Section 1.1

### Critical Breaking Changes (Authentication)

- [x] T059 [US3] Test Google OAuth flow and check if PAR (Pushed Authorization Requests) causes failures per research.md Section 1.1
- [x] T060 [US3] If OAuth fails, disable PAR in Google OAuth configuration in source/MoriiCoffee.Infrastructure/Configurations/AuthenticationConfiguration.cs (`options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable`)
- [x] T061 [US3] Verify JWT Bearer authentication middleware ordering is correct in source/MoriiCoffee.Presentation/Program.cs
- [x] T062 [US3] Test email/password login flow to verify Identity via shared framework works correctly

### Critical Breaking Changes (Entity Framework Core)

- [x] T063 [US3] Determine if using Azure SQL or SQL Server 2022+ per research.md Section 1.1
- [x] T064 [US3] If Azure SQL, update DbContext configuration to use `UseAzureSql()` instead of `UseSqlServer()` in source/MoriiCoffee.Infrastructure.Persistence/Configurations/DatabaseConfiguration.cs
- [x] T065 [US3] Run database migrations and verify they apply successfully (`docker logs moriicoffee.api | grep -i migration`)
- [x] T066 [US3] Test complex LINQ queries for nullable semantics issues per research.md Section 1.1
- [x] T067 [US3] Verify seed data operations complete successfully

### Medium Impact Breaking Changes

- [x] T068 [US3] Review all DTOs and API models for duplicate property names (case-insensitive) per research.md Section 1.3
- [x] T069 [US3] Test JSON serialization/deserialization for JsonDocument nullable behavior changes
- [x] T070 [US3] Review MediatR handler registrations for DI container compatibility per research.md Section 1.4
- [x] T071 [US3] Verify FluentValidation middleware integration still works per research.md Section 1.2

### Comprehensive Manual Verification (from quickstart.md)

**Authentication & Authorization** (10 checks):
- [x] T072 [US3] Test email/password registration (POST /api/auth/register)
- [x] T073 [US3] Test email/password login (POST /api/auth/login) and verify JWT token returned
- [x] T074 [US3] Test Google OAuth registration flow (GET /api/auth/google)
- [x] T075 [US3] Test Google OAuth login flow for existing user
- [x] T076 [US3] Test JWT token validation on protected endpoints (with/without token)
- [x] T077 [US3] Test token refresh flow (if implemented)
- [x] T078 [US3] Test account linking between email and Google
- [x] T079 [US3] Test user roles and permissions enforcement
- [x] T080 [US3] Verify token expiration is enforced correctly
- [x] T081 [US3] Check authentication logs for errors (`docker logs moriicoffee.api | grep -i auth`)

**Email Service (Brevo)** (6 checks):
- [x] T082 [US3] Test welcome email is sent on registration
- [x] T083 [US3] Test verification email is sent and link works
- [x] T084 [US3] Verify email templates render correctly (HTML embedded resources)
- [x] T085 [US3] Verify Brevo SDK communication succeeds (check logs)
- [x] T086 [US3] Verify email delivery logging is recorded
- [x] T087 [US3] Verify failed email sends are logged appropriately

**Database Operations (EF Core)** (9 checks):
- [x] T088 [US3] Test user CRUD operations (create, read, update, delete)
- [x] T089 [US3] Test AspNetUserLogins table records persist correctly
- [x] T090 [US3] Test AspNetUserTokens table records persist correctly
- [x] T091 [US3] Test product CRUD operations (if applicable)
- [x] T092 [US3] Test complex queries execute without errors
- [x] T093 [US3] Verify nullable reference types don't cause NullReferenceExceptions
- [x] T094 [US3] Test transaction handling for multi-step operations
- [x] T095 [US3] Verify seed data populated correctly (Products, Categories, Banners, Users)
- [x] T096 [US3] Verify change tracking works correctly

**File Storage (MinIO)** (6 checks):
- [x] T097 [US3] Verify MinIO container starts and initializes
- [x] T098 [US3] Test file upload to MinIO (POST /api/files/upload)
- [x] T099 [US3] Test file download from MinIO (GET /api/files/{id})
- [x] T100 [US3] Test file deletion from MinIO (DELETE /api/files/{id})
- [x] T101 [US3] Test S3 fallback (if configured)
- [x] T102 [US3] Verify MinIO connection logs show no errors

**API Endpoints** (9 checks):
- [x] T103 [US3] Test all authentication endpoints return correct status codes
- [x] T104 [US3] Test product endpoints return data correctly (GET /api/products)
- [x] T105 [US3] Test category endpoints return data correctly (GET /api/categories)
- [x] T106 [US3] Test banner endpoints return data correctly (GET /api/banners)
- [x] T107 [US3] Test request validation returns 400 Bad Request with error details
- [x] T108 [US3] Test request/response JSON serialization works correctly
- [x] T109 [US3] Verify Swagger UI displays all endpoints correctly
- [x] T110 [US3] Verify Swagger documentation is generated correctly
- [x] T111 [US3] Test CORS headers are set correctly

**Application Startup & Infrastructure** (10 checks):
- [x] T112 [US3] Verify Docker container builds without errors
- [x] T113 [US3] Verify Docker container starts without errors
- [x] T114 [US3] Verify database connection established on startup
- [x] T115 [US3] Verify MinIO connection established on startup
- [x] T116 [US3] Verify all dependency injection services registered
- [x] T117 [US3] Verify configuration loads from appsettings.json correctly
- [x] T118 [US3] Verify environment variables override config as expected
- [x] T119 [US3] Verify Serilog logging initializes without errors
- [x] T120 [US3] Check for no startup console errors or warnings
- [x] T121 [US3] Test health check endpoint returns healthy (if available)

**Framework & Language Features** (5 checks):
- [x] T122 [US3] Verify nullable reference handling doesn't cause exceptions
- [x] T123 [US3] Verify implicit usings resolve correctly
- [x] T124 [US3] Verify LINQ operations work correctly
- [x] T125 [US3] Verify async/await patterns work correctly
- [x] T126 [US3] Verify no obsolete API warnings in compilation output

### Performance Validation

- [x] T127 [US3] Measure API response times and compare to baseline per research.md Section 8.7
- [x] T128 [US3] Measure application startup time and verify within 10% of baseline (SC-005)
- [x] T129 [US3] Measure database query performance and verify within 10% of baseline (SC-008)
- [x] T130 [US3] Check Docker logs for zero runtime exceptions related to upgrade (SC-009)

**Checkpoint**: All features work identically to pre-upgrade state. Zero functional regression confirmed.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and cleanup after successful upgrade

- [x] T131 [P] Update CLAUDE.md Active Technologies section with ".NET 10" per plan.md Section 1
- [x] T132 [P] Document any flagged packages in research.md Section 2.3 that need future attention
- [x] T133 Review Directory.Build.props NU1903 suppression for AutoMapper vulnerability (may be resolved in 16.1.1)
- [x] T134 Create summary documentation in docs/explainations/summary-dotnet-10-upgrade-VN.md
- [x] T135 Create summary documentation in docs/explainations/summary-dotnet-10-upgrade-ENG.md
- [x] T136 Update quickstart.md with actual .NET 10 SDK version used in global.json
- [x] T137 Verify all success criteria from spec.md are met (SC-001 through SC-010)
- [x] T138 Commit changes with message: "feat: upgrade platform from .NET 8 to .NET 10"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **User Story 2 (Phase 4)**: Depends on User Story 1 completion (cannot upgrade packages without .NET 10 framework)
- **User Story 3 (Phase 5)**: Depends on User Story 2 completion (breaking changes can only be tested after packages are upgraded)
- **Polish (Phase 6)**: Depends on User Story 3 completion (all verification passed)

### User Story Dependencies

**IMPORTANT**: Unlike typical features, this upgrade MUST be done sequentially:

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: MUST wait for US1 completion - Cannot upgrade packages until framework is upgraded
- **User Story 3 (P3)**: MUST wait for US2 completion - Cannot test breaking changes until packages are upgraded

**Rationale**: This is a platform upgrade where each phase builds on the previous:
1. Framework first (US1) → enables package compatibility
2. Packages second (US2) → exposes breaking changes
3. Breaking changes last (US3) → validates everything works

### Within Each User Story

**User Story 1**:
- All tasks sequential (T009 → T010 → T011... → T018)
- Must verify build/run at each checkpoint

**User Story 2**:
- Critical cleanup (T019-T021) MUST happen first
- Microsoft packages (T022-T031) can be parallelized [P]
- Standard third-party (T032-T039) can be parallelized [P]
- Major version third-party (T040-T049) sequential (higher risk)
- Verification (T050-T055) sequential after all updates

**User Story 3**:
- Detection (T056-T058) sequential first
- Authentication fixes (T059-T062) sequential
- EF Core fixes (T063-T067) sequential
- Medium impact (T068-T071) can be mixed
- Manual verification (T072-T126) can be partially parallelized by domain
- Performance (T127-T130) sequential at end

### Parallel Opportunities

**Limited parallelization** due to sequential nature of platform upgrade:

- **Phase 1 Setup**: T001, T002, T003 can be parallel (different verification steps)
- **Phase 2 Foundational**: T005, T006, T007 can be parallel (different environment checks)
- **User Story 2 Package Updates**:
  - Microsoft packages (T022-T029) can run in parallel [P] (different .csproj files)
  - Standard third-party (T032-T037) can run in parallel [P] (different .csproj files)
- **User Story 3 Manual Verification**:
  - Within each domain (Auth, Email, Database, etc.), some checks can be parallel if using automation tools
- **Phase 6 Polish**: T131, T132 can run in parallel [P] (different documentation files)

---

## Parallel Example: User Story 2 (Microsoft Packages)

```bash
# Launch all Microsoft package updates together (different .csproj files):
Task: "Update Microsoft.EntityFrameworkCore.Design to 10.0.5 in Presentation.csproj"
Task: "Update Microsoft.EntityFrameworkCore.SqlServer to 10.0.5 in Infrastructure.Persistence.csproj"
Task: "Update Microsoft.AspNetCore.Identity.EntityFrameworkCore to 10.0.5 in Infrastructure.Persistence.csproj"
Task: "Update Microsoft.EntityFrameworkCore to 10.0.5 in Domain.csproj"
Task: "Update Microsoft.AspNetCore.Authentication.JwtBearer to 10.0.5 in Infrastructure.csproj"
Task: "Update Microsoft.AspNetCore.Authentication.Google to 10.0.5 in Infrastructure.csproj"
Task: "Update Microsoft.AspNetCore.Mvc.NewtonsoftJson to 10.0.5 in Infrastructure.csproj"
Task: "Update Microsoft.Extensions.DependencyInjection to 10.0.5 in Infrastructure.csproj"
```

---

## Implementation Strategy

### Sequential Delivery (REQUIRED for Platform Upgrade)

**Platform upgrades CANNOT use typical MVP/incremental strategy** - each phase must complete before the next:

1. **Complete Phase 1: Setup** → Verify prerequisites ready
2. **Complete Phase 2: Foundational** → Verify environment ready
3. **Complete Phase 3: User Story 1 (Runtime)** → CHECKPOINT: Framework upgraded, builds successfully
4. **Complete Phase 4: User Story 2 (Packages)** → CHECKPOINT: Packages upgraded, application runs
5. **Complete Phase 5: User Story 3 (Breaking Changes)** → CHECKPOINT: Zero regression confirmed
6. **Complete Phase 6: Polish** → Documentation complete

**Cannot skip or reorder**: Each phase is a prerequisite for the next.

### Rollback Strategy

At any point if critical issues arise:

1. `git checkout 005-google-oauth` (or main)
2. `cd deploy && bash run-docker-development.sh`
3. Document failure in GitHub issue
4. No database rollback needed (schema unchanged)

### Verification Gates

**Gate 1** (After US1): Framework builds successfully
**Gate 2** (After US2): Application runs with new packages
**Gate 3** (After US3): All features work, zero regression
**Final Gate** (After Polish): Documentation complete, ready to merge

---

## Risk Mitigation

### High-Risk Tasks (Extra Validation Required)

- **T019-T020**: Removing obsolete Identity packages (test thoroughly)
- **T040-T047**: Major version package updates (review changelogs)
- **T059-T062**: Authentication breaking changes (OAuth PAR, JWT)
- **T063-T067**: EF Core breaking changes (query translation, nullable semantics)
- **T072-T126**: Manual verification (comprehensive testing)

### Monitoring During Execution

- Watch for `TreatWarningsAsErrors` failures (Directory.Build.props)
- Monitor Docker logs continuously for runtime exceptions
- Capture all deprecation warnings for future action
- Verify Serilog output is structured and clean

---

## Notes

- **[P] tasks**: Different files, no dependencies, can be parallelized
- **[Story] label**: Maps task to specific user story for traceability (US1, US2, US3)
- **No automated tests**: All verification is manual (zero test coverage in codebase)
- **Sequential execution required**: Platform upgrade phases must complete in order
- **Rollback plan ready**: Git branch allows instant rollback to .NET 8
- **Zero regression goal**: All features must work identically post-upgrade (SC-003)
- **Performance baseline**: Within 10% of pre-upgrade metrics (SC-005, SC-008)

---

## Task Count Summary

- **Phase 1 (Setup)**: 4 tasks
- **Phase 2 (Foundational)**: 4 tasks
- **Phase 3 (User Story 1 - Runtime)**: 10 tasks
- **Phase 4 (User Story 2 - Packages)**: 37 tasks
- **Phase 5 (User Story 3 - Breaking Changes)**: 75 tasks
- **Phase 6 (Polish)**: 8 tasks

**Total**: 138 tasks

**Parallel Opportunities**: 15 tasks marked [P] (primarily in package updates)

**Estimated Effort**: 4-8 hours (research.md Section 8) = 2 hours implementation + 2-6 hours manual testing
