# Implementation Plan: Email Service for Transactional Emails

**Branch**: `003-email-service-spec` | **Date**: 2026-03-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-email-service-spec/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Replace the stub email service with a production-ready implementation using Brevo (formerly Sendinblue) API to send transactional emails (welcome emails on signup, password reset emails). The service will use HTML templates with dynamic placeholder replacement, fire-and-forget sending to avoid blocking user operations, and comprehensive logging of all email activities. Templates will be stored as embedded resources within the Infrastructure project.

## Technical Context

**Language/Version**: C# / .NET 8.0
**Primary Dependencies**: brevo_csharp (official Brevo SDK), ASP.NET Core Identity, MediatR, Serilog
**Storage**: SQL Server (via Entity Framework Core), embedded HTML email templates, email configuration in appsettings.json
**Testing**: Not yet implemented (to be added as xUnit test project in future)
**Target Platform**: Linux/Windows server (containerized via Docker)
**Project Type**: Web service (ASP.NET Core Web API with Clean Architecture)
**Performance Goals**: Email delivery within 1 minute for 99% of sends, non-blocking email operations
**Constraints**: Fire-and-forget email sending (no blocking of signup/password reset flows), synchronous HTTP calls to Brevo API (async background jobs planned for future Hangfire integration)
**Scale/Scope**: Handles transactional emails for coffee shop web application (welcome, password reset), expected low-to-moderate volume (<1000 emails/day initially)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Constitutional Compliance

| Principle | Status | Notes |
|-----------|--------|-------|
| **Plan Mode Default** | ✅ PASS | Using `/speckit.plan` for architectural planning before implementation |
| **Verification Before Done** | ✅ PASS | Plan includes test scenarios for email delivery verification |
| **Simplicity First & Minimal Impact** | ✅ PASS | Only replacing stub EmailService.cs, minimal changes to existing code |
| **Subagent Strategy** | ✅ PASS | Used Explore agent for codebase discovery |
| **Self-Improvement Loop** | ✅ N/A | No prior corrections on this feature |
| **Autonomous Execution** | ✅ PASS | Plan is actionable without hand-holding |

### Tech Stack Compliance

| Constraint | Status | Notes |
|-----------|--------|-------|
| **Backend (.NET 8 Clean Architecture)** | ✅ PASS | Follows existing pattern: IEmailService interface in Application layer, implementation in Infrastructure layer |
| **Dependency Injection** | ✅ PASS | Will register service in existing DI configuration |
| **Configuration Management** | ✅ PASS | EmailSettings configuration already defined in appsettings.json |

**Overall Gate Status**: ✅ PASS - No constitutional violations. Feature follows established patterns.

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

```text
source/
├── MoriiCoffee.Domain/
│   ├── Aggregates/               # Domain entities and aggregate roots
│   ├── Repositories/             # Repository interfaces
│   └── SeedWork/                 # Base classes for domain layer
│
├── MoriiCoffee.Domain.Shared/
│   ├── Constants/                # Application-wide constants
│   ├── Enums/                    # Shared enumerations
│   ├── HttpResponses/            # Standard HTTP response wrappers
│   ├── Settings/                 # Configuration classes
│   │   ├── JwtOptions.cs
│   │   ├── MinioSettings.cs
│   │   ├── AwsS3Settings.cs
│   │   └── EmailSettings.cs      # ⭐ NEW - Map Brevo config
│   └── SeedWork/                 # Shared base classes
│
├── MoriiCoffee.Application/
│   ├── Commands/                 # CQRS command handlers
│   │   ├── Auth/
│   │   │   ├── SignUp/           # ⚡ Uses IEmailService
│   │   │   └── ForgotPassword/   # ⚡ Uses IEmailService
│   │   └── [other commands]
│   ├── Queries/                  # CQRS query handlers
│   └── SeedWork/
│       ├── Abstractions/         # Service interfaces
│       │   └── IEmailService.cs  # ⚡ Already defined
│       ├── DTOs/
│       ├── Exceptions/
│       ├── Behaviors/            # MediatR pipeline behaviors
│       ├── Mappings/
│       └── Helpers/
│
├── MoriiCoffee.Infrastructure/
│   ├── Configurations/           # DI and configuration setups
│   │   ├── DependencyInjection.cs   # ⚡ Register BrevoEmailService
│   │   └── SettingsConfiguration.cs # ⚡ Bind EmailSettings
│   ├── Services/
│   │   ├── EmailService.cs       # ❌ REMOVE - stub implementation
│   │   ├── Email/
│   │   │   ├── BrevoEmailService.cs  # ⭐ NEW - Production implementation
│   │   │   └── EmailTemplates.cs     # ⭐ NEW - Template loader helper
│   │   ├── TokenService.cs
│   │   ├── MinioFileService.cs
│   │   └── AwsS3FileService.cs
│   └── Resources/
│       └── EmailTemplates/       # ⭐ NEW directory
│           ├── welcome.html      # ⭐ NEW - Welcome email template
│           └── password-reset.html # ⭐ NEW - Password reset template
│
├── MoriiCoffee.Infrastructure.Persistence/
│   ├── Configurations/           # EF Core configurations
│   ├── Data/                     # ApplicationDbContext and seeding
│   ├── Migrations/               # EF Core migrations
│   ├── Repositories/             # Repository implementations
│   └── SeedWork/                 # Base repository and UnitOfWork
│
└── MoriiCoffee.Presentation/
    ├── Controllers/              # API endpoints (unchanged)
    ├── Middlewares/              # HTTP pipeline middleware
    ├── Extensions/               # Startup extensions
    └── appsettings.json          # ⚡ Already has EmailSettings section
```

**Structure Decision**: Clean Architecture with layered separation. Email service follows existing pattern:
- **Interface**: `IEmailService` in Application/SeedWork/Abstractions (already exists)
- **Implementation**: `BrevoEmailService` in Infrastructure/Services/Email (new)
- **Configuration**: `EmailSettings` in Domain.Shared/Settings (new), bound in Infrastructure configuration
- **Templates**: HTML files as embedded resources in Infrastructure/Resources/EmailTemplates (new)
- **Registration**: DI registration in Infrastructure/DependencyInjection.cs (update existing)

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No Violations**: All constitutional principles followed. No complexity justification required.

---

## Phase 0: Research (Complete)

**Status**: ✅ Complete

**Output**: `research.md`

**Key Decisions**:
1. **Email Provider**: Brevo (formerly Sendinblue) via brevo_csharp SDK
2. **Template Management**: Embedded HTML resources with placeholder replacement
3. **Configuration**: EmailSettings class following existing pattern
4. **Error Handling**: Fire-and-forget with comprehensive logging
5. **DI Lifetime**: Scoped registration
6. **SDK Pattern**: TransactionalEmailsApi from brevo_csharp
7. **Placeholders**: Simple string replacement ({{UserName}}, {{StorefrontUrl}}, {{ResetUrl}})
8. **Security**: Leverage ASP.NET Identity for token generation, manual sender verification in Brevo
9. **Verification**: Manual testing via development environment
10. **Package**: brevo_csharp v6.0.0

**All unknowns resolved**: Ready for Phase 1 design

---

## Phase 1: Design & Contracts (Complete)

**Status**: ✅ Complete

**Outputs**:
- `data-model.md` - Configuration models, service interfaces, template metadata
- `contracts/IEmailService.md` - Internal service contract documentation
- `quickstart.md` - Step-by-step implementation guide
- `CLAUDE.md` - Updated agent context with new technologies

**Key Design Artifacts**:

### Data Model
- **EmailSettings**: Configuration class with FromEmail, FromName, URLs, Brevo API key
- **IEmailService**: Existing interface (no changes required)
- **Template Metadata**: welcome.html and password-reset.html with defined placeholders
- **No Database Entities**: Fire-and-forget email sending, no persistence

### Contracts
- **IEmailService Contract**: Internal service abstraction used by command handlers
  - `SendWelcomeEmailAsync(string toEmail, string toName)`
  - `SendPasswordResetEmailAsync(string toEmail, string resetUrl)`
  - Fire-and-forget behavior (no exceptions except template loading)
  - Comprehensive logging (success with MessageId, failure with exception)

### Quickstart
- Complete implementation guide with 6 major steps
- Configuration setup, NuGet package installation, template creation
- Service implementation, DI registration, verification testing
- Production deployment checklist and troubleshooting guide

### Agent Context Update
- Added brevo_csharp to active technologies
- Added embedded HTML email templates to database technologies
- Preserved manual additions between markers

---

## Phase 2: Re-evaluate Constitution Check

**Status**: ✅ PASS - No violations after design

### Post-Design Constitutional Review

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| **Plan Mode Default** | ✅ PASS | Complete planning artifacts created before implementation |
| **Verification Before Done** | ✅ PASS | Quickstart includes comprehensive testing checklist |
| **Simplicity First** | ✅ PASS | Minimal changes: 3 new files, 2 templates, 2 config updates, 1 deletion |
| **Minimal Impact** | ✅ PASS | Only touches email service code, no changes to handlers or controllers |
| **Subagent Strategy** | ✅ PASS | Used Explore agent for codebase discovery |
| **Self-Improvement Loop** | ✅ N/A | No prior corrections |
| **Autonomous Execution** | ✅ PASS | Quickstart enables autonomous implementation |

### Tech Stack Compliance (Post-Design)

| Constraint | Status | Post-Design Notes |
|-----------|--------|-------------------|
| **Clean Architecture** | ✅ PASS | Follows layer separation: Domain.Shared (settings), Application (interface), Infrastructure (implementation) |
| **Dependency Injection** | ✅ PASS | Scoped registration in existing DI configuration |
| **Configuration** | ✅ PASS | Strongly-typed EmailSettings bound via existing SettingsConfiguration pattern |
| **Logging** | ✅ PASS | Uses existing Serilog infrastructure |
| **NuGet Packages** | ✅ PASS | Single new dependency: brevo_csharp v6.0.0 |

**Overall Gate Status**: ✅ PASS - Design maintains constitutional compliance

---

## Implementation Checklist

**Prerequisites**:
- [ ] Brevo account created
- [ ] Sender email verified in Brevo dashboard
- [ ] API key generated from Brevo dashboard
- [ ] .NET 8 SDK installed

**Phase 1: Configuration**:
- [ ] Create EmailSettings.cs in Domain.Shared/Settings
- [ ] Bind EmailSettings in SettingsConfiguration.cs
- [ ] Configure API key via user secrets or environment variable

**Phase 2: Package & Templates**:
- [ ] Install brevo_csharp NuGet package (v6.0.0)
- [ ] Create Resources/EmailTemplates directory
- [ ] Create welcome.html template
- [ ] Create password-reset.html template
- [ ] Mark templates as embedded resources in .csproj

**Phase 3: Implementation**:
- [ ] Create EmailTemplates.cs helper class
- [ ] Create BrevoEmailService.cs implementation
- [ ] Delete old EmailService.cs stub
- [ ] Update DI registration to use BrevoEmailService

**Phase 4: Verification**:
- [ ] Build project (should compile without errors)
- [ ] Run application (should start successfully)
- [ ] Test welcome email via signup flow
- [ ] Test password reset email via forgot password flow
- [ ] Verify emails received and links work
- [ ] Check logs for MessageId on success
- [ ] Test error handling with invalid API key

**Phase 5: Documentation**:
- [ ] Document Brevo setup in deployment guide
- [ ] Update README with email service configuration
- [ ] Add monitoring/alerting for email failures (optional)

---

## Success Criteria Verification

Mapping feature spec success criteria to implementation verification:

| Success Criterion | Verification Method | Status |
|-------------------|---------------------|--------|
| **SC-001**: 99% emails delivered within 1 minute | Manual testing + production monitoring | ⏳ Verify in implementation |
| **SC-002**: 99% password reset emails within 1 minute | Manual testing + production monitoring | ⏳ Verify in implementation |
| **SC-003**: 95% delivery success rate | Brevo dashboard analytics + logs | ⏳ Verify in production |
| **SC-004**: No blocking of user operations | Fire-and-forget pattern + manual testing | ✅ Verified in design |
| **SC-005**: Reduced support tickets via tracking | Log MessageId for delivery tracking | ✅ Verified in design |

---

## Risks & Mitigations

| Risk | Impact | Mitigation | Status |
|------|--------|-----------|--------|
| Brevo API downtime | Emails not delivered | Fire-and-forget prevents signup/reset failures, logs capture errors | ✅ Mitigated |
| Invalid API key | Emails not delivered | Configuration validation at startup, error logging | ✅ Mitigated |
| Template loading failure | Email sending crashes | FileNotFoundException thrown at runtime, indicates deployment issue | ✅ Acceptable (fail fast) |
| Rate limit exceeded | Emails blocked | Monitor Brevo usage, upgrade plan if needed | ⏳ Monitor in production |
| Sender not verified | Emails rejected by Brevo | Manual verification required before production deployment | ⏳ Document in deployment guide |

---

## Next Steps

After completing planning:

1. **Implementation**: Follow quickstart.md step-by-step guide
2. **Testing**: Execute verification checklist in quickstart
3. **Deployment**: Configure production environment per deployment checklist
4. **Monitoring**: Set up alerts for email failures (Serilog sinks)
5. **Task Generation**: Run `/speckit.tasks` to generate implementation tasks

**Note**: This planning phase ends here. Implementation begins with `/speckit.tasks` or manual execution of quickstart guide.

---

## Summary

**Planning Complete**: Email service implementation fully designed and ready for execution

**Artifacts Generated**:
- ✅ research.md - All technical decisions documented
- ✅ data-model.md - Configuration models and service interfaces defined
- ✅ contracts/IEmailService.md - Service contract documented
- ✅ quickstart.md - Step-by-step implementation guide
- ✅ Agent context updated (CLAUDE.md)

**Constitutional Compliance**: ✅ All principles followed, no violations

**Implementation Complexity**: Low - 8 file modifications/creations, single new NuGet package

**Ready for**: `/speckit.tasks` command to generate actionable implementation tasks

---

**Feature Branch**: `003-email-service-spec`
**Plan File**: `/Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee/specs/003-email-service-spec/plan.md`
**Spec File**: `/Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee/specs/003-email-service-spec/spec.md`
