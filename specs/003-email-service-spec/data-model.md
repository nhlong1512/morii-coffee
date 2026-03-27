# Data Model: Email Service

**Feature**: 003-email-service-spec
**Date**: 2026-03-27
**Status**: Complete

## Overview

This document defines the data structures and models for the email service implementation. Since email sending is fire-and-forget with no persistence layer, this document focuses on configuration models, DTOs, and in-memory structures.

---

## Configuration Models

### EmailSettings

**Purpose**: Strongly-typed configuration model for email service settings

**Location**: `source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs`

**Structure**:
```csharp
namespace MoriiCoffee.Domain.Shared.Settings;

public class EmailSettings
{
    /// <summary>
    /// Sender email address (must be verified in Brevo dashboard)
    /// </summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>
    /// Display name for sender
    /// </summary>
    public string FromName { get; set; } = null!;

    /// <summary>
    /// Frontend base URL for storefront links
    /// </summary>
    public string StorefrontUrl { get; set; } = null!;

    /// <summary>
    /// Frontend password reset page URL
    /// </summary>
    public string ResetPasswordBaseUrl { get; set; } = null!;

    /// <summary>
    /// Brevo-specific configuration
    /// </summary>
    public BrevoSettings Brevo { get; set; } = null!;
}

public class BrevoSettings
{
    /// <summary>
    /// Brevo API key (xkeysib-... format)
    /// </summary>
    public string ApiKey { get; set; } = null!;
}
```

**Validation Rules**:
- All fields are required (null validation via `= null!`)
- `FromEmail` must be valid email format (validated by Brevo API)
- `FromEmail` must be verified in Brevo dashboard (runtime validation by API)
- `ApiKey` must be valid Brevo API key (runtime validation by API)
- URLs must be valid HTTP/HTTPS URLs (enforced by configuration binding)

**Configuration Binding** (in SettingsConfiguration.cs):
```csharp
var emailSettings = configuration
    .GetSection(nameof(EmailSettings))
    .Get<EmailSettings>();

services.AddSingleton(emailSettings!);
```

**appsettings.json Mapping**:
```json
{
  "EmailSettings": {
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "http://localhost:3000",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "Brevo": {
      "ApiKey": "xkeysib-xxxx..."
    }
  }
}
```

---

## Service Interface

### IEmailService

**Purpose**: Abstraction for email sending operations

**Location**: `source/MoriiCoffee.Application/SeedWork/Abstractions/IEmailService.cs`

**Existing Interface** (no changes required):
```csharp
namespace MoriiCoffee.Application.SeedWork.Abstractions;

public interface IEmailService
{
    /// <summary>
    /// Send welcome email to newly registered user
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient display name</param>
    Task SendWelcomeEmailAsync(string toEmail, string toName);

    /// <summary>
    /// Send password reset email with reset link
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="resetUrl">Complete password reset URL with token</param>
    Task SendPasswordResetEmailAsync(string toEmail, string resetUrl);
}
```

**Design Notes**:
- Interface already exists and is used by SignUpCommandHandler and ForgotPasswordCommandHandler
- No changes needed to interface
- Implementation will be swapped from stub to Brevo-backed service

**Optional Methods** (for future extensions):
```csharp
// Not implementing in current scope, but interface is extensible:
// Task SendAsync(string toEmail, string toName, string subject, string htmlContent);
// Task SendWithTemplateAsync(string toEmail, string toName, long templateId, object templateParams);
```

---

## Template Models

### Email Template Metadata

**Purpose**: Define structure and placeholders for email templates

**Templates**:

1. **Welcome Email** (`welcome.html`)
   - **Subject**: `"Welcome to Morii Coffee, {toName}!"`
   - **Placeholders**:
     - `{{UserName}}` - Recipient's display name
     - `{{StorefrontUrl}}` - Link to main storefront
   - **Content**: Friendly welcome message, introduction to services, call-to-action to browse products

2. **Password Reset Email** (`password-reset.html`)
   - **Subject**: `"Reset Your Morii Coffee Password"`
   - **Placeholders**:
     - `{{UserName}}` - Recipient's display name
     - `{{ResetUrl}}` - Complete password reset URL with token
   - **Content**: Security-focused message, clear reset button/link, expiration notice, support contact

**Template Loading Pattern**:
```csharp
// Helper class in MoriiCoffee.Infrastructure/Services/Email/EmailTemplates.cs
public static class EmailTemplates
{
    private const string ResourcePrefix = "MoriiCoffee.Infrastructure.Resources.EmailTemplates";

    public static string LoadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{ResourcePrefix}.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Email template '{templateName}' not found. " +
                $"Ensure it is marked as an embedded resource in the .csproj file."
            );
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
```

---

## Brevo SDK Models

**Note**: These models are provided by the `brevo_csharp` NuGet package and do not need to be defined in our codebase.

### SendSmtpEmail (from SDK)

Used to construct email messages:
```csharp
var sendSmtpEmail = new SendSmtpEmail
{
    Sender = new SendSmtpEmailSender(name: "Morii Coffee", email: "no-reply@moriicoffee.com"),
    To = new List<SendSmtpEmailTo>
    {
        new SendSmtpEmailTo(email: "user@example.com", name: "John Doe")
    },
    Subject = "Welcome to Morii Coffee!",
    HtmlContent = "<html>...</html>"
};
```

### CreateSmtpEmail (from SDK)

Returned by API after successful send:
```csharp
public class CreateSmtpEmail
{
    public string MessageId { get; set; }  // Used for logging and tracking
}
```

---

## Internal Service Models

### BrevoEmailService State

**Purpose**: Service implementation with injected dependencies

**Structure**:
```csharp
public class BrevoEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly UserManager<UserEntity> _userManager;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly TransactionalEmailsApi _apiInstance;

    public BrevoEmailService(
        EmailSettings emailSettings,
        UserManager<UserEntity> userManager,
        ILogger<BrevoEmailService> logger)
    {
        _emailSettings = emailSettings;
        _userManager = userManager;
        _logger = logger;

        // Configure Brevo SDK
        Configuration.Default.ApiKey["api-key"] = _emailSettings.Brevo.ApiKey;
        _apiInstance = new TransactionalEmailsApi();
    }

    // ... implementation methods
}
```

**Field Descriptions**:
- `_emailSettings`: Configuration for sender, URLs, API key
- `_userManager`: Used to lookup user display name for password reset emails
- `_logger`: Serilog logger for audit trail and error logging
- `_apiInstance`: Brevo SDK client for sending emails

---

## No Database Persistence

**Important**: Email sending is fire-and-forget with no database persistence.

**No new tables or entities**:
- Email delivery status is not tracked in database
- Email history is not stored (logging only)
- No email queue table (future: Hangfire for background jobs)

**Audit Trail**:
- All email operations logged via Serilog
- Logs include: recipient email, subject, MessageId (on success), error details (on failure)
- Logs can be shipped to external logging service (Seq, Application Insights, etc.)

---

## Error Handling Models

**No custom exception types** - email failures are logged but not thrown

**Error Scenarios**:

1. **Brevo API Failure**
   - Invalid API key
   - Unverified sender address
   - Rate limit exceeded
   - Network timeout
   - **Handling**: Catch all exceptions, log error, return gracefully (no throw)

2. **Template Loading Failure**
   - Template file not found
   - Embedded resource not configured correctly
   - **Handling**: Throw `FileNotFoundException` (indicates deployment/build issue)

3. **Configuration Failure**
   - Missing EmailSettings in appsettings.json
   - Null/empty API key
   - **Handling**: Fail at startup (DI registration will throw)

---

## Dependency Injection Lifetime

**Service Registration**:
```csharp
// In MoriiCoffee.Infrastructure/DependencyInjection.cs
services.AddScoped<IEmailService, BrevoEmailService>();
```

**Lifetime**: Scoped
- One instance per HTTP request
- Shared across command handler pipeline within same request
- HttpClient connection pooling benefits

---

## State Transitions

### Email Lifecycle (Non-Persisted)

```
[Request Received]
    ↓
[Handler Invoked]
    ↓
[Email Service Called] (fire-and-forget)
    ↓
[Template Loaded] → FileNotFoundException if missing
    ↓
[Placeholders Replaced]
    ↓
[Brevo API Called]
    ↓
    ├─ [Success] → Log MessageId → End
    └─ [Failure] → Log Error → End (no throw)
    ↓
[User Response Sent] (independent of email success/failure)
```

**Key Point**: Email success/failure does NOT affect user-facing operation (signup, password reset)

---

## Summary

**Configuration Models**:
- `EmailSettings` - Sender info, URLs, Brevo API key
- `BrevoSettings` - Nested Brevo-specific config

**Service Models**:
- `IEmailService` - Existing interface (no changes)
- `BrevoEmailService` - Implementation with injected dependencies

**Template Models**:
- `welcome.html` - Welcome email template with `{{UserName}}`, `{{StorefrontUrl}}`
- `password-reset.html` - Reset email template with `{{UserName}}`, `{{ResetUrl}}`
- `EmailTemplates` - Helper class for loading embedded resources

**No Database Entities**: Email is fire-and-forget, no persistence layer

**Next Phase**: Contract definitions and quickstart guide
