# Feature Specification: .NET 10 Platform Upgrade

**Feature Branch**: `006-dotnet-10-upgrade`
**Created**: 2026-04-02
**Status**: Draft
**Input**: User description: "Upgrade the entire project from .NET 8 to .NET 10, including all .csproj files, NuGet packages, runtime references, and resolving breaking changes"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Platform Runtime Upgrade (Priority: P1)

The development team needs to upgrade the entire Morii Coffee platform from .NET 8 to .NET 10 to benefit from the latest runtime performance improvements, security patches, language features, and long-term support. This upgrade must maintain all existing functionality without regressions while establishing a foundation for future development on the latest stable .NET platform.

**Why this priority**: This is the foundational change that enables all other aspects of the upgrade. Without successfully upgrading the runtime platform itself, package upgrades and breaking change resolutions cannot be completed. This represents the core technical migration.

**Independent Test**: Can be fully tested by building all projects successfully with the updated .NET 10 SDK and runtime, verifying that all compilation succeeds without errors, and confirming Docker containers build and start correctly with .NET 10 base images. Delivers immediate value by establishing the upgraded platform foundation.

**Acceptance Scenarios**:

1. **Given** all projects currently target .NET 8, **When** all .csproj files are updated to target net10.0, **Then** all projects compile successfully without framework compatibility errors
2. **Given** the solution uses .NET 8 SDK, **When** global.json is updated to specify .NET 10 SDK version, **Then** the build process uses the correct SDK version consistently
3. **Given** Dockerfiles reference .NET 8 base images, **When** all Dockerfile FROM statements are updated to mcr.microsoft.com/dotnet/aspnet:10.0 and mcr.microsoft.com/dotnet/sdk:10.0, **Then** Docker containers build successfully and run with .NET 10 runtime
4. **Given** the project is built with .NET 10, **When** running `cd deploy && bash run-docker-development.sh`, **Then** all services start successfully without runtime errors

---

### User Story 2 - Dependency Package Upgrade (Priority: P2)

All NuGet package dependencies across the solution need to be upgraded to their latest .NET 10 compatible versions, with particular attention to critical Microsoft packages (ASP.NET Core, Entity Framework Core, Extensions, Identity) and third-party packages, ensuring compatibility and stability while taking advantage of performance improvements and new features.

**Why this priority**: After the runtime upgrade (P1), package upgrades are the next critical step. Without upgrading dependencies, the application cannot leverage .NET 10 improvements and may encounter compatibility issues. This must happen before breaking changes can be properly addressed since new package versions may introduce or resolve breaking changes.

**Independent Test**: Can be fully tested by restoring all NuGet packages successfully, verifying no package version conflicts exist, confirming all package references resolve to .NET 10 compatible versions, and ensuring the application builds without package-related errors. Delivers value by providing access to latest features, bug fixes, and performance improvements in dependencies.

**Acceptance Scenarios**:

1. **Given** the solution uses .NET 8 versions of Microsoft.AspNetCore.* packages, **When** all ASP.NET Core packages are upgraded to latest .NET 10 versions, **Then** package restore succeeds and no version conflicts occur
2. **Given** the solution uses .NET 8 versions of Microsoft.EntityFrameworkCore.* packages, **When** all EF Core packages are upgraded to latest .NET 10 versions, **Then** database operations function correctly with the new versions
3. **Given** the solution uses .NET 8 versions of Microsoft.Extensions.* packages, **When** all Extensions packages are upgraded to latest .NET 10 versions, **Then** dependency injection and configuration systems work correctly
4. **Given** the solution uses .NET 8 version of ASP.NET Core Identity, **When** Microsoft.AspNetCore.Identity packages are upgraded to .NET 10 versions, **Then** authentication and authorization features function correctly
5. **Given** third-party packages (SendGrid, Brevo, etc.) may not have .NET 10 specific releases, **When** evaluating each package, **Then** either latest stable version compatible with .NET 10 is used or explicitly flagged with compatibility notes
6. **Given** all packages are upgraded, **When** running `dotnet restore` on the solution, **Then** all packages restore successfully without warnings or errors

---

### User Story 3 - Breaking Change Resolution (Priority: P3)

All breaking changes introduced in .NET 9 and .NET 10 that affect the Morii Coffee codebase must be identified and resolved, including deprecated APIs, changed default behaviors, removed methods, and any behavioral differences that could impact application functionality, ensuring zero functional regression after the upgrade.

**Why this priority**: After runtime and packages are upgraded (P1, P2), the final step is addressing breaking changes. This ensures the application code is compatible with the new platform and dependencies. This is the last priority because breaking changes can only be fully identified and tested after the runtime and packages are updated.

**Independent Test**: Can be fully tested by running the complete application through all existing features, verifying no runtime exceptions occur related to API changes, confirming all business logic behaves identically to pre-upgrade behavior, and validating that automated tests pass. Delivers value by ensuring production readiness and zero regression.

**Acceptance Scenarios**:

1. **Given** .NET 10 may have deprecated certain APIs used in the codebase, **When** analyzing compilation warnings and errors, **Then** all deprecated API usages are identified and replaced with recommended alternatives
2. **Given** .NET 10 may have changed default behaviors (e.g., nullable reference type handling, JSON serialization), **When** comparing runtime behavior against .NET 8 baseline, **Then** any behavioral differences are identified and corrected to maintain consistency
3. **Given** certain methods or classes may be removed in .NET 10, **When** scanning for breaking change documentation, **Then** all removed APIs are replaced with their .NET 10 equivalents
4. **Given** the application is fully upgraded, **When** running all existing features end-to-end (authentication, email services, product management, etc.), **Then** all functionality works identically to the pre-upgrade state with no regressions
5. **Given** all code changes are complete, **When** running automated tests (if any exist), **Then** all tests pass successfully
6. **Given** the upgraded application is running, **When** monitoring application logs during feature testing, **Then** no unexpected warnings, errors, or exceptions appear related to the platform upgrade

---

### Edge Cases

- What happens when a critical third-party package (e.g., Brevo SDK, SendGrid SDK) does not yet have a stable .NET 10 release? The system should flag this explicitly, use the closest compatible version (preferably .NET 8 compatible version that supports .NET 10), and document a note for future upgrade when stable version is available.
- What happens when breaking changes affect core authentication or authorization logic? Changes must be thoroughly tested with all authentication flows (email/password, Google OAuth) to ensure no security regressions occur.
- What happens when EF Core breaking changes affect database migrations or queries? All database operations must be validated, and migrations must be tested to ensure data integrity is maintained.
- What happens when Docker container build fails due to SDK version conflicts? The build process should clearly identify the conflicting version, and global.json should be consulted to ensure SDK version consistency.
- What happens when runtime performance characteristics change after upgrade? Performance-critical operations (database queries, API response times) should be monitored to ensure no unexpected degradation occurs.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST identify all locations in the codebase where .NET version is referenced, including all .csproj files, global.json, Dockerfiles (including docker-compose files), and any CI/CD configuration files
- **FR-002**: System MUST update TargetFramework in every .csproj file from net8.0 to net10.0, covering Domain, Application, Infrastructure, and API projects without exception
- **FR-003**: System MUST upgrade all Microsoft.AspNetCore.* package references to their latest .NET 10 compatible versions
- **FR-004**: System MUST upgrade all Microsoft.EntityFrameworkCore.* package references to their latest .NET 10 compatible versions
- **FR-005**: System MUST upgrade all Microsoft.Extensions.* package references to their latest .NET 10 compatible versions
- **FR-006**: System MUST upgrade all Microsoft.AspNetCore.Identity.* package references to their latest .NET 10 compatible versions
- **FR-007**: System MUST upgrade all third-party packages (SendGrid SDK, Brevo SDK, FluentValidation, MediatR, Serilog, etc.) to their latest .NET 10 compatible versions
- **FR-008**: System MUST update global.json to specify the .NET 10 SDK version if global.json exists
- **FR-009**: System MUST update all Dockerfile FROM statements to use mcr.microsoft.com/dotnet/aspnet:10.0 and mcr.microsoft.com/dotnet/sdk:10.0 base images
- **FR-010**: System MUST update all docker-compose.yml files to reference .NET 10 compatible container images
- **FR-011**: System MUST identify and document all breaking changes introduced in .NET 9 and .NET 10 that affect the existing codebase
- **FR-012**: System MUST resolve all breaking changes by replacing deprecated APIs, updating code to handle changed default behaviors, and replacing removed methods with their .NET 10 equivalents
- **FR-013**: System MUST flag any third-party package that does not have a stable .NET 10 release, document the chosen version, and provide a note explaining the compatibility status
- **FR-014**: System MUST verify that the upgraded application builds successfully without errors across all project layers (Domain, Application, Infrastructure, API)
- **FR-015**: System MUST verify that all existing features function correctly after upgrade, including authentication (email/password and Google OAuth), email services (SendGrid/Brevo), and all business logic
- **FR-016**: System MUST verify that Docker containers build and run successfully using the run-docker-development.sh script
- **FR-017**: System MUST ensure zero functional regression - all features that worked before the upgrade must continue to work identically after the upgrade
- **FR-018**: System MUST verify that database operations (EF Core queries, migrations) function correctly with the upgraded EF Core version

### Key Entities

This is a platform upgrade feature that affects infrastructure rather than business domain entities. The upgrade impacts:

- **Project Files**: All .csproj files defining TargetFramework and NuGet package references
- **SDK Configuration**: global.json file specifying SDK version requirements
- **Container Definitions**: Dockerfile and docker-compose.yml files defining runtime environments
- **Package Dependencies**: NuGet package references across all projects
- **Codebase**: All C# source code that may be affected by API changes, deprecations, or behavioral differences

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All projects in the solution compile successfully with .NET 10 without any build errors or warnings related to framework compatibility
- **SC-002**: All Docker containers build successfully and start without errors when running the deployment script
- **SC-003**: All existing application features (100% feature parity) function identically to pre-upgrade behavior with zero regressions during end-to-end testing
- **SC-004**: Package restore completes successfully for all projects with zero version conflicts or compatibility warnings
- **SC-005**: Application startup time remains within 10% of pre-upgrade baseline (no significant performance degradation)
- **SC-006**: All authentication flows (email/password and Google OAuth) complete successfully with no security or functional regressions
- **SC-007**: All email sending operations (SendGrid/Brevo) function correctly with upgraded SDK versions
- **SC-008**: All database operations perform correctly with upgraded EF Core version, with query execution times within 10% of pre-upgrade baseline
- **SC-009**: Zero runtime exceptions or errors appear in application logs during comprehensive feature testing that are related to the platform upgrade
- **SC-010**: Any third-party packages without .NET 10 stable releases are documented with clear compatibility notes and future upgrade paths identified
