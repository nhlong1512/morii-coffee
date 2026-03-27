# Research: Remove AWS SES Email Provider Support

**Feature**: 002-remove-aws-ses
**Date**: 2026-03-27
**Status**: Complete

## Overview

This research phase identifies all AWS SES-related code in the Morii Coffee codebase and confirms the strategy for safe removal without impacting existing SendGrid email functionality.

## Research Questions

### 1. Where is AWS SES code currently located?

**Decision**: AWS SES code exists in only ONE source file: `EmailSettings.cs`

**Findings**:

Global search for `AwsSes|AWS.*SES|SesOptions` revealed AWS SES references in:

**Source code** (actual implementation):
- `source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs`:
  - Line 11: XML comment mentioning `"AwsSes"` as accepted provider value
  - Line 14: `Provider` property default value is `"SendGrid"`
  - Line 37-38: `AwsSes` property of type `AwsSesOptions`
  - Lines 49-59: `AwsSesOptions` class definition with Region, AccessKey, SecretKey properties

**Documentation** (references only):
- `specs/001-email-social-auth/plan.md` - mentions "AwsSesEmailService" but this service was NEVER implemented
- `specs/001-email-social-auth/quickstart.md` - documents Provider accepts `"AwsSes"` value
- `specs/summaries/summary-email-service-integration-*.md` - mentions AWS SES support in architecture

**Critical Finding**: Despite documentation mentioning "AwsSesEmailService", **NO IMPLEMENTATION FILE EXISTS**. There is no `AwsSesEmailService.cs` file in the codebase.

**Rationale**: The AWS SES feature was architecturally planned (configuration model exists) but never implemented. Only SendGrid has a concrete service implementation.

---

### 2. Is there an AwsSesEmailService implementation?

**Decision**: NO - AWS SES email service was never implemented

**Findings**:

- Searched for `AwsSesEmailService.cs` or similar files: **NOT FOUND**
- Searched for `SES` in file names: **NO RESULTS**
- Reviewed `source/MoriiCoffee.Infrastructure/Services/Email/` directory:
  - ✅ `SendGridEmailService.cs` exists and is fully implemented
  - ❌ No AWS SES service file exists

**Verified in `DependencyInjection.cs` (lines 44-52)**:
```csharp
services.AddScoped<IEmailService>(sp =>
{
    var settings = sp.GetRequiredService<EmailSettings>();
    return settings.Provider switch
    {
        "SendGrid" => ActivatorUtilities.CreateInstance<SendGridEmailService>(sp),
        _ => ActivatorUtilities.CreateInstance<SendGridEmailService>(sp) // Default to SendGrid
    };
});
```

**Analysis**: The switch statement checks `Provider` value but:
- Only "SendGrid" case exists
- Default case (`_`) ALSO returns SendGridEmailService
- No "AwsSes" case is present
- Even if Provider were set to "AwsSes", it would fall through to default and use SendGrid

**Rationale**: The provider-switching logic is already non-functional for AWS SES. Removing it simplifies code without changing behavior.

---

### 3. Are there AWS SES NuGet packages in the project?

**Decision**: NO AWS SES packages are installed

**Findings**:

Reviewed `source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj`:
- ✅ SendGrid package: `<PackageReference Include="SendGrid" Version="9.29.3" />`
- ❌ No AWSSDK.SimpleEmailService or similar packages
- ✅ Note: AWSSDK.S3 package exists but that's for file storage (separate feature), not email

**Rationale**: No AWS SES SDK to uninstall. No package cleanup required.

---

### 4. What configuration changes are needed?

**Decision**: Remove AwsSes property and AwsSesOptions class from EmailSettings.cs

**Current State** (`EmailSettings.cs`):
```csharp
public class EmailSettings
{
    public string Provider { get; set; } = "SendGrid";  // ⚠️ TO SIMPLIFY: Remove property
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string StorefrontUrl { get; set; } = null!;
    public string ResetPasswordBaseUrl { get; set; } = null!;
    public SendGridOptions SendGrid { get; set; } = new();
    public AwsSesOptions AwsSes { get; set; } = new();  // ⚠️ TO REMOVE
}

public class SendGridOptions
{
    public string ApiKey { get; set; } = null!;
}

public class AwsSesOptions  // ⚠️ TO REMOVE (entire class)
{
    public string Region { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
}
```

**Target State** (after removal):
```csharp
public class EmailSettings
{
    // Provider property removed - SendGrid is the only implementation
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string StorefrontUrl { get; set; } = null!;
    public string ResetPasswordBaseUrl { get; set; } = null!;
    public SendGridOptions SendGrid { get; set; } = new();
    // AwsSes property removed
}

public class SendGridOptions
{
    public string ApiKey { get; set; } = null!;
}

// AwsSesOptions class completely removed
```

**appsettings.json State**:
- Production template (`appsettings.json`): EmailSettings section NOT PRESENT (empty template)
- Development config (`appsettings.Development.json`): EmailSettings section ALREADY SendGrid-only (lines 35-44)
- No AwsSes configuration exists in either file

**Rationale**: Configuration files already reflect SendGrid-only usage. No appsettings.json changes required.

---

### 5. What dependency injection changes are needed?

**Decision**: Simplify IEmailService registration to directly instantiate SendGridEmailService

**Current Code** (`DependencyInjection.cs` lines 44-52):
```csharp
services.AddScoped<IEmailService>(sp =>
{
    var settings = sp.GetRequiredService<EmailSettings>();
    return settings.Provider switch
    {
        "SendGrid" => ActivatorUtilities.CreateInstance<SendGridEmailService>(sp),
        _ => ActivatorUtilities.CreateInstance<SendGridEmailService>(sp) // Default to SendGrid
    };
});
```

**Target Code** (simplified):
```csharp
services.AddScoped<IEmailService, SendGridEmailService>();
```

**Rationale**: Since SendGrid is the only implementation and the switch statement already defaults to SendGrid in all cases, the provider-switching logic adds no value. Direct registration is clearer and follows the "Simplicity First" principle.

**Alternative Considered**: Keep the switch statement with only the SendGrid case.
**Rejected Because**: The switch statement implies multiple providers exist. Removing it entirely communicates the architectural decision more clearly: SendGrid is THE email service, not one option among many.

---

### 6. What are the risks of this removal?

**Decision**: MINIMAL RISK - AWS SES was never used in production

**Risk Assessment**:

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Breaking production email delivery | **None** | Critical | AWS SES never implemented; production uses SendGrid exclusively |
| Configuration errors on startup | **Low** | Medium | appsettings.Development.json already valid; no AwsSes section exists |
| Breaking existing deployments | **None** | Critical | Existing deployments don't reference AwsSes configuration |
| Future need for multiple providers | **Low** | Low | If needed, can add back with proper implementation; current code is non-functional anyway |
| Regression in email functionality | **None** | Critical | SendGridEmailService unchanged; IEmailService interface unchanged |

**Evidence of Zero Production Usage**:
1. No AwsSesEmailService.cs implementation file exists
2. DependencyInjection switch defaults to SendGrid for unknown providers
3. appsettings.Development.json contains no AwsSes configuration
4. Feature 001 summary docs confirm "AWS SES implementation (architecture supports it, but not implemented)"

**Rationale**: This is purely dead code removal. The "architecture supports it" claim in documentation is misleading—the architecture has placeholders (config class) but no actual implementation.

---

### 7. How do we verify successful removal?

**Decision**: Three-tier verification strategy

**Verification Steps**:

1. **Static Code Analysis** (zero AWS references):
   ```bash
   # Must return NO results in source/ directory:
   rg "AwsSes|AWS.*SES|SesOptions" source/
   ```

2. **Build Verification** (clean compilation):
   ```bash
   dotnet build source/MoriiCoffee.sln
   # Must succeed with no errors or warnings
   ```

3. **Runtime Verification** (functional email delivery):
   - Start application with appsettings.Development.json (SendGrid config)
   - Execute signup flow → verify welcome email delivered
   - Execute forgot-password flow → verify password reset email delivered
   - Check logs for errors → verify no new errors or warnings

**Success Criteria**:
- ✅ Zero AWS SES references in source code (grep returns empty)
- ✅ Application builds successfully
- ✅ Application starts without configuration errors
- ✅ SendGrid emails deliver identically to pre-removal behavior
- ✅ Logs contain no new errors or warnings

**Rationale**: Multi-level verification ensures both compile-time and runtime correctness.

---

## Technology Stack Decisions

### Email Delivery Service

**Decision**: SendGrid as the sole email provider

**Rationale**:
- SendGrid implementation is complete, tested, and production-ready
- AWS SES was planned but never implemented
- Single provider simplifies configuration and reduces maintenance burden
- SendGrid meets all current requirements (transactional emails, template support, delivery tracking)

**Alternatives Considered**:
- AWS SES: Never implemented, no production need identified
- SMTP: Less reliable, requires managing infrastructure
- Mailgun/Postmark: No requirement for alternative providers

---

### Configuration Architecture

**Decision**: Flat configuration model (no Provider switching)

**Current Architecture** (provider switching):
```csharp
EmailSettings.Provider = "SendGrid" | "AwsSes"
EmailSettings.SendGrid = SendGridOptions { ... }
EmailSettings.AwsSes = AwsSesOptions { ... }
```

**Target Architecture** (single provider):
```csharp
EmailSettings.SendGrid = SendGridOptions { ... }
EmailSettings.FromEmail = "..."
EmailSettings.FromName = "..."
```

**Rationale**: Provider property adds complexity without value. Configuration structure should reflect implementation reality (one provider, not many).

---

### Dependency Injection Pattern

**Decision**: Direct service registration (not factory pattern)

**Pattern Comparison**:

| Pattern | Current | Proposed | Justification |
|---------|---------|----------|---------------|
| Factory with switch | ✅ Used | ❌ Remove | Implies multiple implementations; adds cognitive load |
| Direct registration | ❌ Not used | ✅ Adopt | Clearest expression of "one implementation" |
| Strategy pattern | ❌ Not used | ❌ Skip | Overkill for single implementation |

**Rationale**: Direct registration (`services.AddScoped<IEmailService, SendGridEmailService>()`) is the simplest, clearest pattern when only one implementation exists.

---

## Summary

### Files to Modify

1. **`source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs`**:
   - Remove `Provider` property (line 14)
   - Remove `AwsSes` property (line 38)
   - Remove `AwsSesOptions` class (lines 49-59)
   - Update XML comments to remove AwsSes references (line 11, 37)

2. **`source/MoriiCoffee.Infrastructure/DependencyInjection.cs`**:
   - Replace factory registration (lines 44-52) with direct registration:
     ```csharp
     services.AddScoped<IEmailService, SendGridEmailService>();
     ```

3. **Documentation to update** (optional, not blocking):
   - `specs/001-email-social-auth/quickstart.md` - Remove AwsSes from Provider options
   - Summary docs - Clarify AWS SES was never implemented

### Files Requiring No Changes

- ✅ `SendGridEmailService.cs` - Implementation perfect as-is
- ✅ `IEmailService.cs` - Interface unchanged
- ✅ `appsettings.Development.json` - Already SendGrid-only
- ✅ `MoriiCoffee.Infrastructure.csproj` - No AWS SES packages to remove

### Key Insights

1. **AWS SES was architectural vaporware**: Configuration existed but implementation never materialized
2. **Current switch statement is non-functional**: Default case returns SendGrid for unknown providers
3. **Zero production impact**: No deployments use AWS SES configuration
4. **Simplification opportunity**: Remove provider abstraction entirely since only one provider exists

---

## Next Steps

Proceed to **Phase 1: Design & Contracts**:
1. Generate `data-model.md` documenting simplified EmailSettings structure
2. Generate `quickstart.md` for simplified SendGrid-only configuration
3. Update agent context with this feature's technical details
