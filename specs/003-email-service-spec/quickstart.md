# Quickstart: Email Service Implementation

**Feature**: 003-email-service-spec
**Date**: 2026-03-27
**Estimated Implementation Time**: 2-3 hours
**Difficulty**: Intermediate

## Overview

This quickstart guide walks through implementing the Brevo-based email service to replace the current stub implementation. Follow these steps sequentially to implement, configure, and verify the email service.

---

## Prerequisites

Before starting implementation:

- [ ] **Brevo Account**: Create free account at https://www.brevo.com
- [ ] **Verify Sender Email**: Verify sender email address in Brevo dashboard (Settings → Senders)
- [ ] **Generate API Key**: Create API key in Brevo dashboard (Settings → API Keys → SMTP & API)
- [ ] **Development Environment**: .NET 8 SDK installed
- [ ] **Project Understanding**: Review `research.md` and `data-model.md` in this specs directory

---

## Step 1: Configuration Setup

### 1.1 Create EmailSettings Class

**Location**: `source/MoriiCoffee.Domain.Shared/Settings/EmailSettings.cs`

**Action**: Create new file with the following content:

```csharp
namespace MoriiCoffee.Domain.Shared.Settings;

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

**Verification**: Build Domain.Shared project - should compile without errors

---

### 1.2 Bind Configuration in SettingsConfiguration

**Location**: `source/MoriiCoffee.Infrastructure/Configurations/SettingsConfiguration.cs`

**Action**: Add the following to `ConfigureSettings` method:

```csharp
// Add this with other settings bindings (after JwtOptions, MinioSettings, etc.)
var emailSettings = configuration
    .GetSection(nameof(EmailSettings))
    .Get<EmailSettings>();

if (emailSettings == null)
{
    throw new InvalidOperationException(
        "EmailSettings configuration is missing in appsettings.json"
    );
}

services.AddSingleton(emailSettings);
```

**Verification**: Application should still run (email settings already exist in appsettings.json)

---

### 1.3 Configure API Key (Local Development)

**Using User Secrets** (recommended for local dev):

```bash
cd source/MoriiCoffee.Presentation

# Set Brevo API key
dotnet user-secrets set "EmailSettings:Brevo:ApiKey" "xkeysib-YOUR-API-KEY-HERE"

# Verify
dotnet user-secrets list
```

**Alternative: appsettings.Development.json** (less secure):

```json
{
  "EmailSettings": {
    "Brevo": {
      "ApiKey": "xkeysib-YOUR-API-KEY-HERE"
    }
  }
}
```

**⚠️ IMPORTANT**: Never commit API keys to Git. Add to .gitignore if using appsettings files.

**Verification**: API key should be accessible via configuration at runtime

---

## Step 2: Install NuGet Package

### 2.1 Add brevo_csharp Package

**Location**: `source/MoriiCoffee.Infrastructure/`

**Action**: Run the following command:

```bash
cd source/MoriiCoffee.Infrastructure

dotnet add package brevo_csharp --version 6.0.0
```

**Verification**: Check `MoriiCoffee.Infrastructure.csproj` contains:
```xml
<PackageReference Include="brevo_csharp" Version="6.0.0" />
```

---

## Step 3: Create Email Templates

### 3.1 Create Templates Directory

**Action**: Create directory structure:

```bash
cd source/MoriiCoffee.Infrastructure
mkdir -p Resources/EmailTemplates
```

---

### 3.2 Create Welcome Email Template

**Location**: `source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/welcome.html`

**Action**: Create file with this content (or customize):

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Welcome to Morii Coffee</title>
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
    <div style="background-color: #4A2C2A; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
        <h1 style="margin: 0;">Welcome to Morii Coffee!</h1>
    </div>

    <div style="background-color: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px;">
        <p style="font-size: 16px;">Hi <strong>{{UserName}}</strong>,</p>

        <p>Thank you for joining Morii Coffee! We're excited to have you as part of our community.</p>

        <p>Your account has been successfully created and you can now:</p>
        <ul>
            <li>Browse our premium coffee selection</li>
            <li>Place orders online</li>
            <li>Track your order history</li>
            <li>Manage your account preferences</li>
        </ul>

        <div style="text-align: center; margin: 30px 0;">
            <a href="{{StorefrontUrl}}"
               style="background-color: #4A2C2A; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;">
                Start Shopping
            </a>
        </div>

        <p>If you have any questions, feel free to reach out to our support team.</p>

        <p style="margin-top: 30px;">
            Cheers,<br>
            <strong>The Morii Coffee Team</strong>
        </p>
    </div>

    <div style="text-align: center; padding: 20px; font-size: 12px; color: #666;">
        <p>© 2026 Morii Coffee. All rights reserved.</p>
    </div>
</body>
</html>
```

**Note**: Placeholders `{{UserName}}` and `{{StorefrontUrl}}` will be replaced at runtime

---

### 3.3 Create Password Reset Email Template

**Location**: `source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/password-reset.html`

**Action**: Create file with this content (or customize):

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Reset Your Password</title>
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
    <div style="background-color: #D32F2F; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
        <h1 style="margin: 0;">Password Reset Request</h1>
    </div>

    <div style="background-color: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px;">
        <p style="font-size: 16px;">Hi <strong>{{UserName}}</strong>,</p>

        <p>We received a request to reset your Morii Coffee account password.</p>

        <p>Click the button below to create a new password:</p>

        <div style="text-align: center; margin: 30px 0;">
            <a href="{{ResetUrl}}"
               style="background-color: #D32F2F; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;">
                Reset Password
            </a>
        </div>

        <p style="background-color: #FFF3CD; border-left: 4px solid #FFC107; padding: 12px; border-radius: 4px;">
            <strong>⚠️ Security Notice:</strong> This link will expire soon and can only be used once. If you didn't request this password reset, please ignore this email and your password will remain unchanged.
        </p>

        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
        <p style="word-break: break-all; background-color: #eee; padding: 10px; border-radius: 4px; font-size: 12px;">
            {{ResetUrl}}
        </p>

        <p style="margin-top: 30px;">
            Best regards,<br>
            <strong>The Morii Coffee Team</strong>
        </p>
    </div>

    <div style="text-align: center; padding: 20px; font-size: 12px; color: #666;">
        <p>© 2026 Morii Coffee. All rights reserved.</p>
        <p>If you need assistance, please contact our support team.</p>
    </div>
</body>
</html>
```

**Note**: Placeholders `{{UserName}}` and `{{ResetUrl}}` will be replaced at runtime

---

### 3.4 Mark Templates as Embedded Resources

**Location**: `source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj`

**Action**: Add this ItemGroup to the .csproj file:

```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\EmailTemplates\*.html" />
</ItemGroup>
```

**Verification**: Rebuild project and check that templates are included as embedded resources

---

## Step 4: Implement Email Service

### 4.1 Create EmailTemplates Helper

**Location**: `source/MoriiCoffee.Infrastructure/Services/Email/EmailTemplates.cs`

**Action**: Create new file:

```csharp
using System.Reflection;

namespace MoriiCoffee.Infrastructure.Services.Email;

public static class EmailTemplates
{
    private const string ResourcePrefix = "MoriiCoffee.Infrastructure.Resources.EmailTemplates";

    /// <summary>
    /// Load an email template from embedded resources
    /// </summary>
    /// <param name="templateName">Template filename (e.g., "welcome.html")</param>
    /// <returns>Template content as string</returns>
    /// <exception cref="FileNotFoundException">Thrown if template not found</exception>
    public static string LoadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{ResourcePrefix}.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Email template '{templateName}' not found as embedded resource. " +
                $"Expected resource name: {resourceName}. " +
                $"Ensure the template is marked as an EmbeddedResource in the .csproj file."
            );
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
```

**Verification**: Compile project - should build without errors

---

### 4.2 Create BrevoEmailService Implementation

**Location**: `source/MoriiCoffee.Infrastructure/Services/Email/BrevoEmailService.cs`

**Action**: Create new file:

```csharp
using brevo_csharp.Api;
using brevo_csharp.Client;
using brevo_csharp.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Infrastructure.Persistence.Entities;

namespace MoriiCoffee.Infrastructure.Services.Email;

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
        _emailSettings = emailSettings ?? throw new ArgumentNullException(nameof(emailSettings));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure Brevo SDK
        Configuration.Default.ApiKey["api-key"] = _emailSettings.Brevo.ApiKey;
        _apiInstance = new TransactionalEmailsApi();
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName)
    {
        var template = EmailTemplates.LoadTemplate("welcome.html");
        var htmlContent = template
            .Replace("{{UserName}}", toName)
            .Replace("{{StorefrontUrl}}", _emailSettings.StorefrontUrl);

        var subject = $"Welcome to Morii Coffee, {toName}!";

        await SendAsync(toEmail, toName, subject, htmlContent);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetUrl)
    {
        // Lookup user display name for personalization
        var user = await _userManager.FindByEmailAsync(toEmail);
        var displayName = user?.FullName ?? user?.UserName ?? "there";

        var template = EmailTemplates.LoadTemplate("password-reset.html");
        var htmlContent = template
            .Replace("{{UserName}}", displayName)
            .Replace("{{ResetUrl}}", resetUrl);

        var subject = "Reset Your Morii Coffee Password";

        await SendAsync(toEmail, displayName, subject, htmlContent);
    }

    /// <summary>
    /// Core email sending logic using Brevo API
    /// </summary>
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        try
        {
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(
                    name: _emailSettings.FromName,
                    email: _emailSettings.FromEmail
                ),
                To = new List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(email: toEmail, name: toName)
                },
                Subject = subject,
                HtmlContent = htmlContent
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);

            _logger.LogInformation(
                "[BrevoEmailService] Email '{Subject}' sent to {Email}. MessageId: {MessageId}",
                subject,
                toEmail,
                result.MessageId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[BrevoEmailService] Failed to send email to {Email}. Subject: '{Subject}'",
                toEmail,
                subject
            );

            // Fire-and-forget: do not rethrow
        }
    }
}
```

**Verification**: Compile project - should build without errors

---

### 4.3 Delete Old Stub Implementation

**Location**: `source/MoriiCoffee.Infrastructure/Services/EmailService.cs`

**Action**: Delete the file (it's the stub that only logs emails)

```bash
rm source/MoriiCoffee.Infrastructure/Services/EmailService.cs
```

**Verification**: Ensure no build errors after deletion

---

### 4.4 Update Dependency Injection

**Location**: `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`

**Action**: Find the `ConfigureDependencyInjection` method and update:

```csharp
public static IServiceCollection ConfigureDependencyInjection(this IServiceCollection services)
{
    services.AddTransient<IDateTimeProvider, DateTimeProvider>();
    services.AddScoped<ITokenService, TokenService>();

    // OLD: services.AddScoped<IEmailService, EmailService>();
    // NEW:
    services.AddScoped<IEmailService, BrevoEmailService>();

    // ... other services

    return services;
}
```

**Verification**: Application should compile successfully

---

## Step 5: Verification & Testing

### 5.1 Build and Run Application

```bash
cd source/MoriiCoffee.Presentation

dotnet build
dotnet run
```

**Expected**: Application starts without errors, Swagger UI loads at http://localhost:8002/swagger

---

### 5.2 Test Welcome Email (Signup Flow)

**Via Swagger UI**:
1. Navigate to http://localhost:8002/swagger
2. Find `/api/Auth/signup` endpoint
3. Execute POST request with test data:
   ```json
   {
     "userName": "testuser",
     "email": "your-actual-email@example.com",
     "password": "Test@1234",
     "fullName": "Test User"
   }
   ```
4. Check response (should be 200 OK with auth tokens)
5. **Check your email inbox** - welcome email should arrive within 1 minute
6. **Check application logs** - should see:
   ```
   [INF] [BrevoEmailService] Email 'Welcome to Morii Coffee, Test User!' sent to your-actual-email@example.com. MessageId: <...>
   ```

**Verification Checklist**:
- [ ] Signup succeeds (returns tokens)
- [ ] Welcome email received
- [ ] Email contains correct name
- [ ] "Start Shopping" link works
- [ ] Log shows successful send with MessageId

---

### 5.3 Test Password Reset Email

**Via Swagger UI**:
1. Navigate to `/api/Auth/forgot-password` endpoint
2. Execute POST request:
   ```json
   {
     "email": "your-actual-email@example.com"
   }
   ```
3. Check response (should be 200 OK with success message)
4. **Check your email inbox** - password reset email should arrive within 1 minute
5. **Click reset link** - should navigate to frontend reset password page
6. **Check application logs** - should see:
   ```
   [INF] [BrevoEmailService] Email 'Reset Your Morii Coffee Password' sent to your-actual-email@example.com. MessageId: <...>
   ```

**Verification Checklist**:
- [ ] Forgot password succeeds (returns success message)
- [ ] Password reset email received
- [ ] Email contains correct name
- [ ] "Reset Password" button works
- [ ] Reset URL includes token and email parameters
- [ ] Log shows successful send with MessageId

---

### 5.4 Test Error Handling (Invalid API Key)

**Action**: Temporarily set invalid API key in user secrets:

```bash
dotnet user-secrets set "EmailSettings:Brevo:ApiKey" "invalid-key"
```

**Test**:
1. Trigger signup or forgot password
2. **Expected Behavior**:
   - Request still succeeds (fire-and-forget)
   - No email is received
   - **Logs show error**:
     ```
     [ERR] [BrevoEmailService] Failed to send email to test@example.com. Subject: 'Welcome to Morii Coffee, Test!'
     ApiException: Unauthorized...
     ```

**Action**: Restore valid API key after test

**Verification**: Application gracefully handles email failures without crashing

---

### 5.5 Test Template Loading Failure

**Action**: Temporarily rename a template file:

```bash
mv source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/welcome.html source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/welcome.html.bak
dotnet build
```

**Test**:
1. Trigger signup
2. **Expected Behavior**:
   - Signup request FAILS with 500 Internal Server Error
   - Logs show FileNotFoundException

**Action**: Restore template file and rebuild

**Rationale**: Template loading failures indicate deployment issues and should fail fast

---

## Step 6: Production Deployment Configuration

### 6.1 Environment Variables (Production)

**For Docker/Kubernetes**:

```yaml
environment:
  - EmailSettings__Brevo__ApiKey=xkeysib-production-key
  - EmailSettings__FromEmail=no-reply@moriicoffee.com
  - EmailSettings__StorefrontUrl=https://moriicoffee.com
  - EmailSettings__ResetPasswordBaseUrl=https://moriicoffee.com/reset-password
```

**For Azure App Service**:
- Add application settings in Azure Portal
- Use double underscore (`__`) for nested configuration

---

### 6.2 Verify Sender Address in Brevo

**Before production deployment**:
1. Log into Brevo dashboard
2. Navigate to Settings → Senders
3. Add and verify production sender email (e.g., no-reply@moriicoffee.com)
4. Use DNS verification for domain-based sender addresses

---

### 6.3 Production Checklist

Before deploying to production:

- [ ] Brevo API key configured via environment variable (not committed to Git)
- [ ] Sender email verified in Brevo dashboard
- [ ] Storefront URL points to production frontend
- [ ] Password reset URL points to production frontend
- [ ] Templates tested with actual email addresses
- [ ] Error logging configured (Serilog sinks set up)
- [ ] Rate limits reviewed (Brevo free tier: 300 emails/day, upgrade if needed)

---

## Troubleshooting

### Issue: Welcome email not received

**Possible Causes**:
1. **Invalid API key**: Check logs for ApiException: Unauthorized
2. **Unverified sender**: Check Brevo dashboard for sender verification status
3. **Rate limit exceeded**: Check Brevo dashboard for usage limits
4. **Spam folder**: Check recipient's spam/junk folder
5. **Invalid recipient email**: Check logs for validation errors

**Debugging Steps**:
- Check application logs for send attempt
- Look for MessageId in logs (indicates successful send to Brevo)
- Check Brevo dashboard → Logs → Transactional Emails for delivery status
- Verify sender email is verified in Brevo dashboard

---

### Issue: Template not found error

**Possible Causes**:
1. Template not marked as embedded resource in .csproj
2. Template filename typo
3. Wrong resource namespace

**Debugging Steps**:
```bash
# Check if templates are embedded
dotnet publish -c Release
unzip -l bin/Release/net8.0/publish/MoriiCoffee.Infrastructure.dll | grep EmailTemplates

# Should show:
# Resources/EmailTemplates/welcome.html
# Resources/EmailTemplates/password-reset.html
```

---

### Issue: Signup succeeds but no log entry for email

**Possible Cause**: Email service not registered in DI

**Debugging Steps**:
1. Check DependencyInjection.cs for `AddScoped<IEmailService, BrevoEmailService>()`
2. Verify EmailSettings is bound in SettingsConfiguration.cs
3. Add breakpoint in BrevoEmailService constructor to verify instantiation

---

## Next Steps

After successful implementation:

1. **Update Documentation**: Document Brevo setup in deployment guide
2. **Add Monitoring**: Set up alerts for email send failures (e.g., via Serilog sinks)
3. **Upgrade Brevo Plan**: If email volume exceeds free tier (300/day)
4. **Add Unit Tests**: Create test project and add tests for email service
5. **Future Enhancements**:
   - Hangfire background jobs for async email sending
   - Email retry logic
   - Email delivery tracking and analytics
   - Additional email types (order confirmation, shipping notification, etc.)

---

## Summary

**Implementation Steps Completed**:
1. ✅ Created EmailSettings configuration model
2. ✅ Installed brevo_csharp NuGet package
3. ✅ Created HTML email templates
4. ✅ Implemented BrevoEmailService with fire-and-forget pattern
5. ✅ Updated dependency injection
6. ✅ Verified email sending via manual testing

**Total Files Modified/Created**:
- Created: `EmailSettings.cs`, `BrevoEmailService.cs`, `EmailTemplates.cs`
- Created: `welcome.html`, `password-reset.html`
- Modified: `SettingsConfiguration.cs`, `DependencyInjection.cs`, `MoriiCoffee.Infrastructure.csproj`
- Deleted: `EmailService.cs` (old stub)

**Feature Ready**: Email service is production-ready and operational
