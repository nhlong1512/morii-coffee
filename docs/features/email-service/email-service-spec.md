# EmailService Specification

## Overview
Morii Coffee uses Brevo (formerly Sendinblue) as the email delivery provider.
The EmailService handles all transactional emails via the official `brevo_csharp` SDK.
Templates are stored as embedded HTML resources inside the Infrastructure project.

---

## Architecture

- **Interface**: `IEmailService` — defined in Application layer
- **Implementation**: `BrevoEmailService` — defined in Infrastructure layer
- **Templates**: `EmailTemplates` static class — loads embedded `.html` files
- **Config model**: `EmailSettings` — defined in Domain.Shared layer
- **DI lifetime**: Scoped (`AddScoped<IEmailService, BrevoEmailService>`)

---

## EmailSettings Configuration
```json
{
  "EmailSettings": {
    "FromEmail": "string (verified sender in Brevo)",
    "FromName": "string (display name)",
    "StorefrontUrl": "string (frontend base URL)",
    "ResetPasswordBaseUrl": "string (reset password page URL)",
    "Brevo": {
      "ApiKey": "string (xkeysib-... from Brevo dashboard)"
    }
  }
}
```

---

## IEmailService Interface
```csharp
public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string toName);
    Task SendPasswordResetEmailAsync(string toEmail, string resetUrl);
    Task SendAsync(string toEmail, string toName, string subject, string htmlContent);
    Task SendWithTemplateAsync(string toEmail, string toName, long templateId, object templateParams = null);
}
```

---

## Methods

### SendWelcomeEmailAsync
- Triggered by: `SignUpCommandHandler` after user account is created
- Template: `Resources/EmailTemplates/welcome.html`
- Placeholders replaced: `{{UserName}}`, `{{StorefrontUrl}}`
- Subject: `"Welcome to Morii Coffee, {toName}!"`

### SendPasswordResetEmailAsync
- Triggered by: `ForgotPasswordCommandHandler`
- Template: `Resources/EmailTemplates/password-reset.html`
- Placeholders replaced: `{{UserName}}`, `{{ResetUrl}}`
- Subject: `"Reset Your Morii Coffee Password"`
- Looks up display name via `UserManager.FindByEmailAsync`
- Falls back to: `FullName ?? UserName ?? "there"`

### SendAsync (core)
- Called internally by all other Send methods
- Builds `SendSmtpEmail` object with sender, recipient, subject, htmlContent
- Calls `TransactionalEmailsApi.SendTransacEmailAsync`
- Logs success with MessageId via Serilog
- Catches all exceptions, logs error, does NOT rethrow (fire-and-forget)

### SendWithTemplateAsync (optional)
- Uses Brevo dashboard template by `templateId`
- Passes `templateParams` as dynamic object for placeholder replacement
- Same error handling as `SendAsync`

---

## Template System

- Templates stored as embedded resources in `.csproj`:
```xml
  <EmbeddedResource Include="Resources\EmailTemplates\*.html" />
```
- Loaded via `Assembly.GetManifestResourceStream`
- Resource name format: `MoriiCoffee.Infrastructure.Resources.EmailTemplates.{filename}`
- Throws `FileNotFoundException` if template not found
- Placeholder replacement via simple `string.Replace`

---

## Error Handling

- All exceptions caught in `SendAsync` and `SendWithTemplateAsync`
- Errors logged via Serilog: `[BrevoEmailService] Failed to send email to {Email}`
- Exceptions are NOT rethrown — fire-and-forget behavior
- Email failure does not fail the parent command (signup/forgot password still succeeds)

---

## Logging

- Success: `[BrevoEmailService] Email '{Subject}' sent to {Email}. MessageId: {MessageId}`
- Failure: `[BrevoEmailService] Failed to send email to {Email}. Subject: {Subject}`

---

## Current Limitations

- Email sending is **synchronous** — HTTP request blocks while waiting for Brevo API
- No Hangfire integration yet — planned for future async background job processing
- No rate limiting on password reset emails

---
## Security Notes

- API key must NOT be committed to Git
- Use `dotnet user-secrets` for local dev
- Use environment variables or Azure Key Vault for production
- Sender email must be verified in Brevo dashboard before use
- Password reset tokens are URL-encoded, single-use, time-limited (ASP.NET Identity)

---

## Dependencies

| Package | Purpose |
|---|---|
| `brevo_csharp` | Official Brevo .NET SDK |
| `Serilog` | Structured logging |
| `Microsoft.AspNetCore.Identity` | Password reset token generation & user lookup |

---

## File Structure
```
MoriiCoffee.Infrastructure/
├── Services/
│   └── Email/
│       ├── BrevoEmailService.cs
│       └── EmailTemplates.cs
└── Resources/
    └── EmailTemplates/
        ├── welcome.html
        └── password-reset.html

MoriiCoffee.Application/
└── Abstractions/
    └── IEmailService.cs

MoriiCoffee.Domain.Shared/
└── Settings/
    └── EmailSettings.cs
```