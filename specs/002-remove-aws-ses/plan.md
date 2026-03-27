# Implementation Plan: Remove AWS SES Email Provider Support

**Branch**: `002-remove-aws-ses` | **Date**: 2026-03-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-remove-aws-ses/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Remove all AWS SES email provider infrastructure from the Morii Coffee codebase, keeping only SendGrid as the email delivery service. This simplifies configuration, reduces code complexity, and eliminates unused provider-switching logic while maintaining 100% functional parity for welcome and password reset emails.

## Technical Context

**Language/Version**: C# / .NET 8.0
**Architecture**: Clean Architecture with Domain-Driven Design
**Primary Dependencies**: SendGrid SDK 9.29.3, ASP.NET Core Identity, MediatR, Serilog
**Storage**: SQL Server (via Entity Framework Core), email configuration in appsettings.json
**Testing**: N/A (refactoring task - existing email functionality tests from feature 001 apply)
**Target Platform**: Docker containers (Linux), development on macOS/Windows
**Project Type**: Web service (ASP.NET Core API backend for Next.js frontend)
**Performance Goals**: Maintain existing email delivery performance (95% delivered within 60 seconds)
**Constraints**: Zero regression in email functionality, no new errors introduced
**Scale/Scope**: Single infrastructure project (MoriiCoffee.Infrastructure), 3 files modified, 1 configuration class simplified

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Plan Mode Default ✅

**Status**: PASS

This feature properly entered plan mode before implementation. The planning workflow is being followed: specification created first, now generating implementation plan before any code changes.

### II. Verification Before Done ✅

**Status**: PASS

The specification includes clear verification requirements:
- Global code search for AWS SES references must return zero results
- Email functionality tests from feature 001 must pass with identical behavior
- Application must start successfully with SendGrid-only configuration
- Logs must show no new errors or warnings

### III. Simplicity First & Minimal Impact ✅

**Status**: PASS

This feature exemplifies minimal impact:
- Removes unused code only (AWS SES was never implemented)
- Changes limited to 3 files: EmailSettings.cs, DependencyInjection.cs, and potentially appsettings.json
- No new functionality added
- No refactoring of unrelated code
- SendGrid implementation remains completely unchanged

### IV. Subagent Strategy & Delegation ✅

**Status**: PASS (not applicable)

This is a simple refactoring task with no need for subagent delegation. The scope is clear: remove AWS SES references from a well-defined set of files.

### V. Self-Improvement Loop ✅

**Status**: PASS (preparatory)

Upon completion, if the user provides corrections, `tasks/lessons.md` will be updated according to the self-improvement loop principle.

### VI. Autonomous Execution with Concise Communication ✅

**Status**: PASS

This plan enables autonomous execution: clear identification of files to modify, specific code to remove, and verification steps. Implementation can proceed without hand-holding.

### Summary

**All constitutional checks passed.** No violations require justification. This refactoring aligns perfectly with the "Simplicity First & Minimal Impact" principle by removing dead code.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

This is a .NET Clean Architecture web service with the following structure:

```text
source/
├── MoriiCoffee.Domain/                    # Core domain entities
├── MoriiCoffee.Domain.Shared/
│   └── Settings/
│       └── EmailSettings.cs               # ⚠️ TO MODIFY: Remove AwsSesOptions class
├── MoriiCoffee.Application/               # Use cases and interfaces
│   └── SeedWork/
│       └── Abstractions/
│           └── IEmailService.cs           # Interface (unchanged)
├── MoriiCoffee.Infrastructure/            # External service implementations
│   ├── DependencyInjection.cs             # ⚠️ TO MODIFY: Remove provider switch logic
│   ├── Services/
│   │   ├── Email/
│   │   │   └── SendGridEmailService.cs    # ✅ KEEP: Already correct implementation
│   │   └── EmailService.cs                # ✅ KEEP: Development stub (unchanged)
│   └── MoriiCoffee.Infrastructure.csproj  # ✅ VERIFY: No AWS SES packages
├── MoriiCoffee.Infrastructure.Persistence/
└── MoriiCoffee.Presentation/
    ├── appsettings.json                   # 📝 OPTIONAL: Add EmailSettings (currently missing)
    └── appsettings.Development.json       # ✅ VERIFY: Already SendGrid-only

specs/
└── 002-remove-aws-ses/
    ├── plan.md                            # This file
    ├── research.md                        # Phase 0 output
    ├── data-model.md                      # Phase 1 output
    └── quickstart.md                      # Phase 1 output
```

**Structure Decision**: This is a Clean Architecture .NET web service. The email functionality is isolated in the Infrastructure layer. Changes are limited to:
1. `EmailSettings.cs` - Remove `AwsSesOptions` class and `AwsSes` property
2. `DependencyInjection.cs` - Remove provider switch statement, register SendGridEmailService directly
3. Verification of existing configuration files (no AWS SES references)

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations to track.** All constitutional principles are satisfied. This refactoring exemplifies the "Simplicity First & Minimal Impact" principle by removing unused code.

---

## Phase 0: Research Summary

**Status**: ✅ Complete

**Deliverable**: [research.md](./research.md)

**Key Findings**:

1. **AWS SES Implementation**: NEVER EXISTED
   - Only configuration class (`AwsSesOptions`) was created
   - No service implementation file (`AwsSesEmailService.cs`) exists
   - DependencyInjection switch defaults to SendGrid for unknown providers

2. **Current Provider Logic**: NON-FUNCTIONAL for AWS SES
   - Switch statement checks `Provider` value
   - Only "SendGrid" case exists
   - Default case also returns SendGrid
   - Setting Provider to "AwsSes" would still use SendGrid

3. **Code Locations**:
   - `EmailSettings.cs` - lines 11, 14, 37-38, 49-59 (AWS SES references)
   - `DependencyInjection.cs` - lines 44-52 (provider switch statement)
   - No AWS SES NuGet packages installed

4. **Risk Assessment**: ZERO RISK
   - AWS SES never used in production (configuration doesn't even exist in appsettings files)
   - No migration path needed (nothing to migrate from)
   - SendGrid implementation unchanged

**Decision**: Safe to remove all AWS SES infrastructure without impacting functionality.

---

## Phase 1: Design & Contracts Summary

**Status**: ✅ Complete

**Deliverables**:
- [data-model.md](./data-model.md) - Simplified EmailSettings model
- [quickstart.md](./quickstart.md) - Developer configuration guide

**Design Decisions**:

1. **Configuration Model**:
   - Remove `Provider` property (no provider selection with single implementation)
   - Remove `AwsSes` property and `AwsSesOptions` class
   - Keep all other EmailSettings properties unchanged
   - Keep `SendGridOptions` class unchanged

2. **Dependency Injection**:
   - Replace factory pattern with direct registration
   - Change from: `services.AddScoped<IEmailService>(sp => { switch... })`
   - Change to: `services.AddScoped<IEmailService, SendGridEmailService>()`
   - Follows standard ASP.NET Core pattern for single implementation

3. **Backward Compatibility**:
   - Existing SendGrid-only configurations work without changes
   - Configurations with Provider/AwsSes fields will ignore those fields (harmless)
   - No breaking changes to public API or email behavior

4. **No Contracts Required**:
   - This is an internal refactoring (infrastructure layer only)
   - No external API changes
   - IEmailService interface unchanged
   - Email sending behavior unchanged

**Agent Context Updated**: ✅ CLAUDE.md updated with .NET 8 + SendGrid stack

---

## Phase 2: Task Generation

**Status**: ⏭️ NOT STARTED (use `/speckit.tasks` to generate)

Phase 2 will generate `tasks.md` with step-by-step implementation instructions for:
1. Modifying `EmailSettings.cs` to remove AWS SES classes
2. Simplifying `DependencyInjection.cs` to use direct registration
3. Verifying no AWS SES references remain in codebase
4. Testing email delivery to ensure no regressions

---

## Re-Evaluation: Constitution Check (Post-Design)

*Required: Re-check constitutional principles after design decisions are made*

### I. Plan Mode Default ✅

**Status**: PASS (unchanged)

Design phase followed proper workflow: research completed before design artifacts created.

### II. Verification Before Done ✅

**Status**: PASS (enhanced)

Design phase added detailed verification strategy in research.md:
- Static code analysis (grep for AWS SES references must return zero)
- Build verification (dotnet build must succeed)
- Runtime verification (email delivery tests must pass)

### III. Simplicity First & Minimal Impact ✅

**Status**: PASS (validated)

Design confirms minimal impact:
- Only 2 source files modified (EmailSettings.cs, DependencyInjection.cs)
- Zero changes to email service implementations
- Zero changes to email templates
- Zero changes to public API surface
- Configuration changes are subtractive only (removing unused fields)

### IV. Subagent Strategy & Delegation ✅

**Status**: PASS (not applicable)

No changes from initial assessment.

### V. Self-Improvement Loop ✅

**Status**: PASS (preparatory)

No changes from initial assessment.

### VI. Autonomous Execution with Concise Communication ✅

**Status**: PASS (enhanced)

Design artifacts provide complete autonomous execution blueprint:
- research.md identifies exact files and line numbers to modify
- data-model.md shows before/after code for each change
- quickstart.md enables verification without additional guidance

### Summary (Post-Design)

**All constitutional checks passed.** Design decisions reinforce the "Simplicity First" principle by choosing direct registration over factory pattern complexity.
