# Research: Email Service Implementation with Brevo

**Feature**: 003-email-service-spec
**Date**: 2026-03-27
**Status**: Complete

## Overview

This document consolidates research findings for implementing a production email service using Brevo (formerly Sendinblue) to replace the current stub implementation.

---

## Decision 1: Email Service Provider

**Decision**: Use Brevo (formerly Sendinblue) via official `brevo_csharp` NuGet package

**Rationale**:
- Configuration already exists in appsettings.json with Brevo section
- Official C# SDK available and actively maintained
- Transactional email tier supports our use case (welcome emails, password resets)
- Simple HTTP API-based integration fits synchronous sending pattern
- Free tier supports development and initial production usage
- Verified sender addresses can be configured via dashboard

**Alternatives Considered**:
1. **SendGrid**: Popular alternative with robust .NET SDK, but would require appsettings.json restructuring
2. **AWS SES**: Already integrated for other services, but requires more setup (domain verification, sandbox removal) and lacks template management features
3. **SMTP Direct**: Simple but no delivery tracking, analytics, or template features

**Implementation Approach**:
- Install `brevo_csharp` NuGet package in Infrastructure project
- Create `BrevoEmailService` implementing `IEmailService`
- Use `TransactionalEmailsApi` from SDK for sending
- Map configuration from `EmailSettings.Brevo.ApiKey` to SDK authentication

---

## Decision 2: Email Template Management

**Decision**: Store templates as embedded HTML resources in Infrastructure project

**Rationale**:
- Templates are code assets that should be versioned with the application
- Embedded resources ensure templates are always available (no file system dependencies)
- Simple placeholder replacement via `string.Replace()` is sufficient for current needs
- Aligns with existing resource management patterns in the project
- Easy to test and preview templates locally

**Alternatives Considered**:
1. **Brevo Dashboard Templates**: Templates stored in Brevo, referenced by ID
   - ❌ Requires manual dashboard configuration per environment
   - ❌ Templates not versioned with code
   - ✅ Supports advanced template features (conditionals, loops)
2. **Database Storage**: Templates in SQL Server
   - ❌ Adds complexity for minimal benefit
   - ❌ Requires migration and seeding
3. **File System**: Templates in wwwroot or similar
   - ❌ Deployment dependency (must ensure files copied)
   - ❌ Can be accidentally deleted or modified

**Implementation Approach**:
- Create `Resources/EmailTemplates/` directory in Infrastructure project
- Add `welcome.html` and `password-reset.html` templates
- Mark as embedded resources in `.csproj`:
  ```xml
  <EmbeddedResource Include="Resources\EmailTemplates\*.html" />
  ```
- Create `EmailTemplates.cs` helper class to load templates via `Assembly.GetManifestResourceStream()`
- Use simple placeholders: `{{UserName}}`, `{{StorefrontUrl}}`, `{{ResetUrl}}`

---

## Decision 3: Configuration Model

**Decision**: Create `EmailSettings.cs` in Domain.Shared/Settings to map appsettings.json

**Rationale**:
- Follows existing pattern (JwtOptions, MinioSettings, AwsS3Settings)
- Strongly-typed configuration prevents typos and enables IntelliSense
- Settings validation happens at startup (fail fast)
- Easy to inject into services via DI

**Configuration Structure**:
```csharp
public class EmailSettings
{
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string StorefrontUrl { get; set; } = null!;
    public string ResetPasswordBaseUrl { get; set; } = null!;
    public BrevoSettings Brevo { get; set; } = null!;
}

public class BrevoSettings
{
    public string ApiKey { get; set; } = null!;
}
```

**Binding in SettingsConfiguration.cs**:
```csharp
var emailSettings = configuration.GetSection(nameof(EmailSettings)).Get<EmailSettings>();
services.AddSingleton(emailSettings!);
```

---

## Decision 4: Error Handling Strategy

**Decision**: Fire-and-forget with comprehensive logging, no exception propagation

**Rationale**:
- Email failures should NOT block user operations (signup, password reset)
- Users care about completing their action (account created, reset requested), not email confirmation
- Logging provides audit trail and debugging capability
- Matches existing pattern in SignUpCommandHandler and ForgotPasswordCommandHandler

**Implementation Approach**:
- Wrap all Brevo API calls in try-catch
- Log success with MessageId: `[BrevoEmailService] Email '{Subject}' sent to {Email}. MessageId: {MessageId}`
- Log failures with details: `[BrevoEmailService] Failed to send email to {Email}. Subject: {Subject}`
- Do NOT rethrow exceptions
- Handlers already use fire-and-forget pattern: `_ = _emailService.SendWelcomeEmailAsync(...)`

**Future Enhancement** (not in scope):
- Hangfire background jobs for retry logic
- Dead letter queue for failed emails
- User notification of email failures

---

## Decision 5: Dependency Injection Lifetime

**Decision**: Register as Scoped (`AddScoped<IEmailService, BrevoEmailService>`)

**Rationale**:
- Matches existing service registration pattern (ITokenService, IFileService)
- Email service is used within request context (command handlers)
- Scoped lifetime allows sharing across handler pipeline (if needed)
- HttpClient (used by Brevo SDK) benefits from connection pooling with scoped lifetime

**Registration in DependencyInjection.cs**:
```csharp
services.AddScoped<IEmailService, BrevoEmailService>();
```

**Remove existing stub**:
```csharp
// DELETE: services.AddScoped<IEmailService, EmailService>();
```

---

## Decision 6: SDK Usage Pattern

**Decision**: Use `TransactionalEmailsApi` from brevo_csharp SDK

**Rationale**:
- Official SDK handles authentication, serialization, and error handling
- Transactional emails API supports all required features (sender, recipient, subject, HTML content)
- Returns `CreateSmtpEmail` response with MessageId for logging
- Well-documented and type-safe

**Code Pattern**:
```csharp
var apiInstance = new TransactionalEmailsApi();
var sendSmtpEmail = new SendSmtpEmail
{
    Sender = new SendSmtpEmailSender(_emailSettings.FromName, _emailSettings.FromEmail),
    To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(toEmail, toName) },
    Subject = subject,
    HtmlContent = htmlContent
};

Configuration.Default.ApiKey["api-key"] = _emailSettings.Brevo.ApiKey;
var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
_logger.LogInformation("[BrevoEmailService] Email '{Subject}' sent to {Email}. MessageId: {MessageId}",
    subject, toEmail, result.MessageId);
```

---

## Decision 7: Template Placeholder Strategy

**Decision**: Simple string replacement with minimal placeholders

**Rationale**:
- Current requirements only need basic personalization (name, URLs)
- String.Replace() is simple, fast, and sufficient
- No need for complex template engines (Razor, Liquid, etc.)
- Easy to test and debug

**Supported Placeholders**:
- `{{UserName}}` - Recipient's display name
- `{{StorefrontUrl}}` - Frontend base URL
- `{{ResetUrl}}` - Password reset link with token

**Template Loading & Replacement Pattern**:
```csharp
public static class EmailTemplates
{
    public static string LoadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MoriiCoffee.Infrastructure.Resources.EmailTemplates.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Email template '{templateName}' not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

// Usage in BrevoEmailService:
var template = EmailTemplates.LoadTemplate("welcome.html");
var htmlContent = template
    .Replace("{{UserName}}", toName)
    .Replace("{{StorefrontUrl}}", _emailSettings.StorefrontUrl);
```

---

## Decision 8: Password Reset Email Security

**Decision**: Leverage existing ASP.NET Identity token generation and validation

**Rationale**:
- Password reset tokens already generated by `UserManager.GeneratePasswordResetTokenAsync()`
- Tokens are URL-encoded, single-use, time-limited (configurable via Identity options)
- Email service only needs to construct URL with token, not validate or manage tokens
- Security is handled by Identity framework, not email service

**Implementation**:
- `SendPasswordResetEmailAsync(string toEmail, string resetUrl)` receives pre-constructed URL
- Handler (ForgotPasswordCommandHandler) constructs URL: `{ResetPasswordBaseUrl}?token={urlEncodedToken}&email={urlEncodedEmail}`
- Template receives full URL as `{{ResetUrl}}`
- No token logic in email service layer

---

## Decision 9: Email Verification for Sender Address

**Decision**: Require manual sender address verification in Brevo dashboard before production use

**Rationale**:
- Email service providers require verified sender addresses to prevent spam
- Verification is one-time setup per environment
- Configuration validation can check if FromEmail is set, but cannot verify Brevo account status
- Clear documentation in README or deployment guide

**Setup Steps** (documented for deployment):
1. Create Brevo account
2. Verify sender email address via Brevo dashboard
3. Generate API key with transactional email permissions
4. Configure API key in appsettings.json or environment variables
5. Test email sending in development environment

**Error Handling**:
- Brevo API will return error if sender address is unverified
- Error will be logged but not crash application (fire-and-forget pattern)

---

## Decision 10: Testing Strategy

**Decision**: Manual testing via development environment, automated testing deferred

**Rationale**:
- No existing test infrastructure in project yet
- Manual testing sufficient for initial implementation:
  - Trigger signup flow → verify welcome email received
  - Trigger forgot password → verify reset email received
  - Check logs for successful MessageId
  - Check logs for error handling when API key is invalid
- Automated testing can be added later with xUnit project

**Testing Checklist** (for implementation verification):
- [ ] Welcome email sent after signup
- [ ] Password reset email sent after forgot password request
- [ ] Email contains correct personalization (name, URLs)
- [ ] Links in email are functional
- [ ] Logs show successful MessageId
- [ ] Error handling works when API key is invalid
- [ ] Error handling works when Brevo API is unreachable
- [ ] Signup/password reset succeeds even if email fails

---

## NuGet Package Requirements

**Primary Dependency**:
```xml
<PackageReference Include="brevo_csharp" Version="6.0.0" />
```

**Existing Dependencies** (already in project):
- `Serilog` - Logging
- `Microsoft.AspNetCore.Identity` - User management and token generation
- `Microsoft.Extensions.Options` - Configuration binding

---

## Security Considerations

1. **API Key Management**:
   - NEVER commit API key to Git
   - Use `dotnet user-secrets` for local development
   - Use environment variables or Azure Key Vault for production

2. **Sensitive Data in Logs**:
   - Log email addresses (audit trail)
   - Log subjects (debugging)
   - Do NOT log email content (may contain sensitive URLs/tokens)
   - Do NOT log API keys

3. **Rate Limiting**:
   - Brevo free tier has daily send limits
   - No rate limiting implemented in code (out of scope)
   - Future: Add rate limiting for password reset requests to prevent abuse

4. **Email Enumeration Prevention**:
   - ForgotPasswordCommandHandler already returns success even if user doesn't exist
   - Email service should not expose whether email was sent or not

---

## Migration Path from Stub

**Current State**:
- `EmailService.cs` logs emails to console
- Registered in DI as `AddScoped<IEmailService, EmailService>()`
- Used in SignUpCommandHandler and ForgotPasswordCommandHandler

**Migration Steps**:
1. Create `EmailSettings.cs` in Domain.Shared/Settings
2. Bind EmailSettings in SettingsConfiguration.cs
3. Install brevo_csharp NuGet package
4. Create EmailTemplates directory and add HTML templates
5. Mark templates as embedded resources in .csproj
6. Create EmailTemplates.cs helper class
7. Create BrevoEmailService.cs implementing IEmailService
8. Update DI registration to use BrevoEmailService
9. Delete old EmailService.cs stub
10. Test manually in development environment

**Backward Compatibility**: None required (stub was never production)

---

## Performance Considerations

**Current Approach**: Synchronous HTTP calls to Brevo API
- Emails sent inline with request processing
- Fire-and-forget pattern prevents blocking user response
- Acceptable for low-to-moderate volume (<1000 emails/day)

**Future Enhancement** (out of scope):
- Hangfire background jobs for async email processing
- Email queue with retry logic
- Batch sending for multiple recipients

**Timeout Handling**:
- Brevo SDK uses default HttpClient timeout (100 seconds)
- Acceptable for transactional emails (send-and-forget)
- Logging captures timeout errors without crashing application

---

## Summary

All technical unknowns resolved. Implementation can proceed with:
- Brevo SDK for email delivery
- Embedded HTML templates with placeholder replacement
- Fire-and-forget error handling with comprehensive logging
- Strongly-typed configuration following existing patterns
- Manual testing for initial verification

**Next Phase**: Design (data model, contracts, quickstart)
