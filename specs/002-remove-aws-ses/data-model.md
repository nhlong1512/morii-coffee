# Data Model: Remove AWS SES Email Provider Support

**Feature**: 002-remove-aws-ses
**Date**: 2026-03-27
**Phase**: Phase 1 - Design

## Overview

This document defines the simplified configuration model after AWS SES removal. The changes are isolated to the `EmailSettings` class in the Domain.Shared layer. No database schema changes are required (email configuration is file-based, not persisted in the database).

## Configuration Model Changes

### EmailSettings Class (MODIFIED)

**Location**: `source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs`

**Current State** (with AWS SES support):

```csharp
namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Top-level configuration for the transactional email system.
/// Bound from the <c>EmailSettings</c> section in appsettings.json.
/// The <see cref="Provider"/> value selects which concrete implementation is registered at startup.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Email provider to use. Accepted values: <c>"SendGrid"</c> or <c>"AwsSes"</c>.
    /// Defaults to <c>"SendGrid"</c>.
    /// </summary>
    public string Provider { get; set; } = "SendGrid";

    /// <summary>Sender email address shown in the From field (e.g., no-reply@moriicoffee.com).</summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>Sender display name shown in the From field (e.g., "Morii Coffee").</summary>
    public string FromName { get; set; } = null!;

    /// <summary>
    /// Base URL of the storefront homepage (e.g., http://localhost:3000 or https://moriicoffee.com).
    /// Used in welcome email CTA button.
    /// </summary>
    public string StorefrontUrl { get; set; } = null!;

    /// <summary>
    /// Base URL of the password-reset page on the frontend (e.g., https://moriicoffee.com/reset-password).
    /// The token is appended as a <c>token</c> query parameter.
    /// </summary>
    public string ResetPasswordBaseUrl { get; set; } = null!;

    /// <summary>SendGrid-specific configuration. Required when <see cref="Provider"/> is <c>"SendGrid"</c>.</summary>
    public SendGridOptions SendGrid { get; set; } = new();

    /// <summary>AWS SES-specific configuration. Required when <see cref="Provider"/> is <c>"AwsSes"</c>.</summary>
    public AwsSesOptions AwsSes { get; set; } = new();
}

/// <summary>Configuration for the SendGrid email provider.</summary>
public class SendGridOptions
{
    /// <summary>SendGrid API key. Obtain from the SendGrid dashboard under Settings → API Keys.</summary>
    public string ApiKey { get; set; } = null!;
}

/// <summary>Configuration for the AWS Simple Email Service provider.</summary>
public class AwsSesOptions
{
    /// <summary>AWS region where SES is configured (e.g., <c>ap-southeast-1</c>).</summary>
    public string Region { get; set; } = null!;

    /// <summary>AWS IAM access key ID with SES send permissions.</summary>
    public string AccessKey { get; set; } = null!;

    /// <summary>AWS IAM secret access key corresponding to <see cref="AccessKey"/>.</summary>
    public string SecretKey { get; set; } = null!;
}
```

**Target State** (SendGrid-only):

```csharp
namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Top-level configuration for the transactional email system.
/// Bound from the <c>EmailSettings</c> section in appsettings.json.
/// Email delivery is handled exclusively by SendGrid.
/// </summary>
public class EmailSettings
{
    /// <summary>Sender email address shown in the From field (e.g., no-reply@moriicoffee.com).</summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>Sender display name shown in the From field (e.g., "Morii Coffee").</summary>
    public string FromName { get; set; } = null!;

    /// <summary>
    /// Base URL of the storefront homepage (e.g., http://localhost:3000 or https://moriicoffee.com).
    /// Used in welcome email CTA button.
    /// </summary>
    public string StorefrontUrl { get; set; } = null!;

    /// <summary>
    /// Base URL of the password-reset page on the frontend (e.g., https://moriicoffee.com/reset-password).
    /// The token is appended as a <c>token</c> query parameter.
    /// </summary>
    public string ResetPasswordBaseUrl { get; set; } = null!;

    /// <summary>SendGrid-specific configuration (API key and delivery settings).</summary>
    public SendGridOptions SendGrid { get; set; } = new();
}

/// <summary>Configuration for the SendGrid email provider.</summary>
public class SendGridOptions
{
    /// <summary>SendGrid API key. Obtain from the SendGrid dashboard under Settings → API Keys.</summary>
    public string ApiKey { get; set; } = null!;
}
```

**Changes Summary**:
- ❌ **REMOVED**: `Provider` property (line 14) - no longer needed with single provider
- ❌ **REMOVED**: `AwsSes` property (line 38) - AWS SES not implemented
- ❌ **REMOVED**: `AwsSesOptions` class (lines 49-59) - entire class deleted
- ✅ **KEPT**: All other properties (FromEmail, FromName, StorefrontUrl, ResetPasswordBaseUrl)
- ✅ **KEPT**: `SendGridOptions` class unchanged
- 📝 **UPDATED**: XML doc comments to remove references to Provider selection and AwsSes

---

## Configuration File Schema

### appsettings.json Schema

**Current Schema** (with AwsSes option):

```json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "http://localhost:3000",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "SendGrid": {
      "ApiKey": "your-sendgrid-api-key-here"
    },
    "AwsSes": {
      "Region": "ap-southeast-1",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key"
    }
  }
}
```

**Target Schema** (SendGrid-only):

```json
{
  "EmailSettings": {
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "http://localhost:3000",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "SendGrid": {
      "ApiKey": "your-sendgrid-api-key-here"
    }
  }
}
```

**Changes Summary**:
- ❌ **REMOVED**: `Provider` field
- ❌ **REMOVED**: `AwsSes` section
- ✅ **KEPT**: All other fields unchanged

**Note**: The existing `appsettings.Development.json` already matches the target schema (no Provider or AwsSes fields). The base `appsettings.json` template has no EmailSettings section at all (empty template).

---

## Dependency Injection Changes

### ConfigureDependencyInjection Method (MODIFIED)

**Location**: `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`

**Current Implementation** (factory pattern with provider switching):

```csharp
public static IServiceCollection ConfigureDependencyInjection(this IServiceCollection services)
{
    services.AddTransient<IDateTimeProvider, DateTimeProvider>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IEmailService>(sp =>
    {
        var settings = sp.GetRequiredService<EmailSettings>();
        return settings.Provider switch
        {
            "SendGrid" => ActivatorUtilities.CreateInstance<SendGridEmailService>(sp),
            _ => ActivatorUtilities.CreateInstance<SendGridEmailService>(sp) // Default to SendGrid
        };
    });
    services.AddScoped<IFileService, AwsS3FileService>();
    return services;
}
```

**Target Implementation** (direct registration):

```csharp
public static IServiceCollection ConfigureDependencyInjection(this IServiceCollection services)
{
    services.AddTransient<IDateTimeProvider, DateTimeProvider>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IEmailService, SendGridEmailService>();
    services.AddScoped<IFileService, AwsS3FileService>();
    return services;
}
```

**Changes Summary**:
- ❌ **REMOVED**: Factory lambda function with service provider resolution
- ❌ **REMOVED**: EmailSettings.Provider switch statement
- ❌ **REMOVED**: ActivatorUtilities.CreateInstance calls
- ✅ **ADDED**: Direct service registration `AddScoped<IEmailService, SendGridEmailService>()`
- ✅ **KEPT**: All other service registrations unchanged

**Rationale**: Direct registration is the standard ASP.NET Core pattern when only one implementation exists. It's clearer, more performant (no lambda invocation), and follows the Open/Closed Principle correctly (closed for modification since no extension points needed).

---

## Entity Relationships

This feature does not affect domain entities. Email configuration is infrastructure-level only and has no database persistence.

**Unchanged Entities**:
- `User` (Domain.Aggregates.UserAggregate) - no changes
- Email templates - remain as embedded resources
- `IEmailService` interface - signature unchanged

**No Database Migrations Required**: Email settings are bound from appsettings.json at runtime and are not stored in the database.

---

## Validation Rules

### EmailSettings Validation

**Required Fields** (enforced by ASP.NET Core configuration binding with `= null!` marker):
- `FromEmail` - must be a valid email address format
- `FromName` - must not be empty
- `StorefrontUrl` - must be a valid URL
- `ResetPasswordBaseUrl` - must be a valid URL
- `SendGrid.ApiKey` - must not be empty

**Current Validation**: ASP.NET Core configuration system validates these fields at startup. If required values are missing, the application fails to start with a clear error message.

**Post-Removal Validation**: Identical. The validation rules do not change; only the schema is simplified.

**No Additional Validation Needed**: The configuration binding infrastructure handles validation automatically via the nullable reference types (`= null!` indicates required).

---

## Backward Compatibility

### Configuration Compatibility

**Scenario 1**: Existing deployments with SendGrid-only configuration
- **Impact**: ✅ NONE - configuration already matches target schema
- **Migration**: Not required

**Scenario 2**: Existing deployments with Provider + AwsSes configuration
- **Impact**: ⚠️ HARMLESS - Provider and AwsSes fields will be ignored (not bound to model)
- **Migration**: Optional - can remove unused fields at convenience
- **Behavior**: Application will function identically (AwsSes config was never used)

**Scenario 3**: Fresh deployments
- **Impact**: ✅ SIMPLIFIED - cleaner configuration template without unused fields
- **Migration**: Not applicable

### Code Compatibility

**Breaking Changes**: NONE

- `IEmailService` interface unchanged
- `SendGridEmailService` implementation unchanged
- Email sending behavior unchanged
- Public API surface unchanged

**Compile-Time Safety**: Removal of `Provider` property and `AwsSesOptions` class may cause warnings in code that references them, but:
- Global search confirms no other code references `Provider` or `AwsSes` properties
- Only `EmailSettings` class itself and `DependencyInjection.cs` reference these members
- Both files are being modified as part of this feature

---

## Summary

### Model Changes

| Entity/Class | Change Type | Details |
|-------------|-------------|---------|
| `EmailSettings` | Modified | Remove Provider, AwsSes properties; update XML docs |
| `SendGridOptions` | Unchanged | No modifications |
| `AwsSesOptions` | Deleted | Entire class removed |
| `DependencyInjection` | Modified | Replace factory with direct registration |

### No Changes Required

- ✅ Database schema (email config is file-based)
- ✅ Domain entities (User, etc.)
- ✅ Email service interfaces (IEmailService)
- ✅ Email service implementations (SendGridEmailService)
- ✅ Email templates (welcome.html, password-reset.html)
- ✅ Application configuration files (already correct)

### Migration Path

**For Developers**:
1. Pull latest code with simplified EmailSettings
2. (Optional) Remove Provider and AwsSes fields from local appsettings.json if present
3. Ensure SendGrid.ApiKey is configured
4. Application starts normally

**For Operations**:
1. No deployment changes required
2. Existing configuration continues to work (unused fields ignored)
3. Future deployments use simplified template without Provider/AwsSes

---

## Next Steps

Proceed to generate `quickstart.md` for developer onboarding with simplified configuration.
