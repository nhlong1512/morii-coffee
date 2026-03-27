# Contract: IEmailService

**Feature**: 003-email-service-spec
**Date**: 2026-03-27
**Contract Type**: Internal Service Interface
**Status**: Stable (No Breaking Changes)

## Overview

`IEmailService` is an internal abstraction in the Application layer that defines the contract for sending transactional emails. This is an **internal interface** used by command handlers (SignUpCommandHandler, ForgotPasswordCommandHandler) and is not directly exposed to external consumers.

**Location**: `MoriiCoffee.Application/SeedWork/Abstractions/IEmailService.cs`

**Implementation**: `MoriiCoffee.Infrastructure/Services/Email/BrevoEmailService.cs`

---

## Interface Definition

```csharp
namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Service for sending transactional emails (welcome, password reset, etc.)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a welcome email to a newly registered user.
    /// </summary>
    /// <param name="toEmail">Recipient's email address (must be valid email format)</param>
    /// <param name="toName">Recipient's display name (used for personalization)</param>
    /// <returns>Task that completes when email has been sent or logged as failed</returns>
    /// <remarks>
    /// This method is fire-and-forget. Email failures are logged but do not throw exceptions.
    /// The returned Task completes successfully regardless of email delivery status.
    /// </remarks>
    Task SendWelcomeEmailAsync(string toEmail, string toName);

    /// <summary>
    /// Send a password reset email with a secure reset link.
    /// </summary>
    /// <param name="toEmail">Recipient's email address (must be valid email format)</param>
    /// <param name="resetUrl">Complete password reset URL including token parameter</param>
    /// <returns>Task that completes when email has been sent or logged as failed</returns>
    /// <remarks>
    /// This method is fire-and-forget. Email failures are logged but do not throw exceptions.
    /// The returned Task completes successfully regardless of email delivery status.
    /// The resetUrl should be a complete URL constructed by the caller (e.g., ForgotPasswordCommandHandler).
    /// </remarks>
    Task SendPasswordResetEmailAsync(string toEmail, string resetUrl);
}
```

---

## Method Contracts

### SendWelcomeEmailAsync

**Purpose**: Send welcome email to newly registered users

**Parameters**:
- `toEmail` (string, required): Valid email address of the recipient
- `toName` (string, required): Display name for personalization (e.g., username or full name)

**Returns**: `Task` (completes when send attempt finishes, regardless of success/failure)

**Behavior**:
1. Load `welcome.html` template from embedded resources
2. Replace `{{UserName}}` with `toName`
3. Replace `{{StorefrontUrl}}` with configured storefront URL
4. Send email via Brevo API with subject: `"Welcome to Morii Coffee, {toName}!"`
5. Log success with MessageId or log failure with error details
6. Return without throwing exceptions (fire-and-forget)

**Guarantees**:
- ✅ Will NOT throw exceptions (all errors caught and logged)
- ✅ Will NOT block caller beyond API call duration
- ✅ Will log all send attempts (success or failure)
- ❌ Does NOT guarantee email delivery
- ❌ Does NOT retry on failure

**Example Usage** (from SignUpCommandHandler):
```csharp
// Fire-and-forget pattern - caller does not await
_ = _emailService.SendWelcomeEmailAsync(user.Email!, user.UserName!);
```

---

### SendPasswordResetEmailAsync

**Purpose**: Send password reset email with secure reset link

**Parameters**:
- `toEmail` (string, required): Valid email address of the recipient
- `resetUrl` (string, required): Complete password reset URL with token and email parameters

**Returns**: `Task` (completes when send attempt finishes, regardless of success/failure)

**Behavior**:
1. Look up user by email via `UserManager` to get display name
2. Fallback to "there" if user not found or name unavailable
3. Load `password-reset.html` template from embedded resources
4. Replace `{{UserName}}` with display name
5. Replace `{{ResetUrl}}` with provided reset URL
6. Send email via Brevo API with subject: `"Reset Your Morii Coffee Password"`
7. Log success with MessageId or log failure with error details
8. Return without throwing exceptions (fire-and-forget)

**Guarantees**:
- ✅ Will NOT throw exceptions (all errors caught and logged)
- ✅ Will NOT block caller beyond API call duration
- ✅ Will log all send attempts (success or failure)
- ✅ Will look up user display name for personalization
- ❌ Does NOT guarantee email delivery
- ❌ Does NOT retry on failure

**Example Usage** (from ForgotPasswordCommandHandler):
```csharp
var resetUrl = $"{_emailSettings.ResetPasswordBaseUrl}?token={urlEncodedToken}&email={urlEncodedEmail}";

// Fire-and-forget pattern - caller does not await
_ = _emailService.SendPasswordResetEmailAsync(user.Email!, resetUrl);
```

---

## Error Handling Contract

**No Exceptions Thrown**: All email failures are caught and logged internally

**Error Scenarios**:

| Scenario | Behavior | Caller Impact |
|----------|----------|---------------|
| Invalid API key | Log error, return successfully | None - caller proceeds normally |
| Unverified sender | Log error, return successfully | None - caller proceeds normally |
| Network timeout | Log error, return successfully | None - caller proceeds normally |
| Rate limit exceeded | Log error, return successfully | None - caller proceeds normally |
| Invalid recipient email | Log error, return successfully | None - caller proceeds normally |
| Template not found | **Throws FileNotFoundException** | Caller receives exception (indicates deployment issue) |

**Template Not Found Exception**:
- This is the ONLY scenario where an exception is thrown
- Indicates a critical deployment/build issue (embedded resource missing)
- Should fail fast to alert developers

---

## Logging Contract

**Log Entries** (via Serilog):

**Success**:
```
[INF] [BrevoEmailService] Email 'Welcome to Morii Coffee, John Doe!' sent to john@example.com. MessageId: <abc123@smtp-relay.mailin.fr>
```

**Failure**:
```
[ERR] [BrevoEmailService] Failed to send email to john@example.com. Subject: 'Reset Your Morii Coffee Password'
Exception: ApiException: Unauthorized - Invalid API key
```

**Log Structure**:
- **Success**: Level = Information, includes Subject, Recipient Email, MessageId
- **Failure**: Level = Error, includes Subject, Recipient Email, Exception Details

**No PII Concerns**:
- Email addresses are logged (needed for audit trail)
- Subjects are logged (needed for debugging)
- Email content is NOT logged (may contain sensitive URLs/tokens)
- API keys are NOT logged

---

## Configuration Dependencies

**Required Configuration** (via EmailSettings):
- `FromEmail` - Verified sender address
- `FromName` - Sender display name
- `StorefrontUrl` - Frontend base URL for links
- `ResetPasswordBaseUrl` - Password reset page URL
- `Brevo.ApiKey` - Brevo API key

**Configuration Validation**:
- Startup will fail if EmailSettings is null or missing required fields
- Runtime validation of API key and sender address happens during first send attempt

---

## Implementation Contract

**Implementation Requirements**:

1. **Must NOT throw exceptions** for email send failures (except template loading)
2. **Must log all send attempts** with structured logging
3. **Must use configured EmailSettings** for sender and URLs
4. **Must support HTML email content** with UTF-8 encoding
5. **Must replace template placeholders** with actual values
6. **Must use fire-and-forget pattern** (no blocking of caller beyond API call)

**Current Implementation**: `BrevoEmailService`
- Uses Brevo TransactionalEmailsApi
- Loads templates from embedded resources
- Logs via Serilog
- Fire-and-forget error handling

---

## Extensibility

**Future Methods** (not currently required by callers):

```csharp
// Generic email sending
Task SendAsync(string toEmail, string toName, string subject, string htmlContent);

// Brevo dashboard template support
Task SendWithTemplateAsync(string toEmail, string toName, long templateId, object templateParams);
```

**Adding New Email Types**:
1. Add new method to IEmailService interface
2. Create new HTML template in Resources/EmailTemplates
3. Mark template as embedded resource in .csproj
4. Implement method in BrevoEmailService
5. Call from appropriate command handler

**Swapping Implementations**:
- Change DI registration in DependencyInjection.cs
- New implementation must honor contract (fire-and-forget, logging, no exceptions)
- Example: Switch from Brevo to SendGrid by creating SendGridEmailService

---

## Testing Contract

**How to Verify Implementation**:

1. **Unit Tests** (when test project added):
   - Mock IEmailService in handlers
   - Verify SendWelcomeEmailAsync called after signup
   - Verify SendPasswordResetEmailAsync called after forgot password request

2. **Integration Tests** (when test project added):
   - Test template loading and placeholder replacement
   - Test error handling with invalid API key
   - Verify logs contain expected entries

3. **Manual Testing** (current approach):
   - Trigger signup → verify email received and links work
   - Trigger forgot password → verify email received and reset link works
   - Check logs for MessageId on success
   - Test with invalid API key → verify graceful failure and error log

---

## Breaking Changes Policy

**This interface is internal to the application and NOT exposed to external consumers.**

**Breaking Changes Allowed**:
- Adding new methods (e.g., SendOrderConfirmationEmailAsync)
- Adding optional parameters to existing methods
- Changing return type from Task to Task<EmailResult> (if delivery tracking needed)

**Breaking Changes Discouraged**:
- Changing existing method signatures (breaks command handlers)
- Removing methods still used by handlers
- Changing error handling contract (throwing exceptions)

**Versioning**: Not required (internal interface)

---

## Summary

**Contract Type**: Internal service abstraction (not exposed to external consumers)

**Primary Consumers**: SignUpCommandHandler, ForgotPasswordCommandHandler

**Key Guarantees**:
- Fire-and-forget behavior (no exceptions except template loading)
- Comprehensive logging (audit trail and debugging)
- Non-blocking (returns quickly regardless of email status)

**Next Phase**: Quickstart guide for implementation
