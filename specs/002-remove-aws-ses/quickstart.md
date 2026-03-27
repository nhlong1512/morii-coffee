# Quickstart: Simplified SendGrid Email Configuration

**Feature**: 002-remove-aws-ses
**Date**: 2026-03-27
**Audience**: Developers configuring email delivery for Morii Coffee

## Overview

After AWS SES removal, the Morii Coffee email system uses SendGrid exclusively for transactional email delivery (welcome emails, password resets). This guide shows how to configure email settings for local development and production deployments.

---

## Prerequisites

- SendGrid account with verified sender domain
- SendGrid API key with "Mail Send" permissions
- Access to edit `appsettings.Development.json` (local) or environment-specific configuration (production)

---

## Configuration Steps

### 1. Obtain SendGrid API Key

1. Log in to [SendGrid Dashboard](https://app.sendgrid.com/)
2. Navigate to **Settings** → **API Keys**
3. Click **Create API Key**
4. Name: `MoriiCoffee-{Environment}` (e.g., `MoriiCoffee-Development`, `MoriiCoffee-Production`)
5. Permissions: Select **Restricted Access**
   - Enable **Mail Send** → **Full Access**
   - Disable all other permissions
6. Click **Create & View**
7. **Copy the API key** (shown only once)

---

### 2. Configure Email Settings

#### Local Development

Edit `source/MoriiCoffee.Presentation/appsettings.Development.json`:

```json
{
  "EmailSettings": {
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "http://localhost:3000",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "SendGrid": {
      "ApiKey": "SG.paste-your-sendgrid-api-key-here"
    }
  }
}
```

**Field Descriptions**:

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `FromEmail` | string | Sender email address (must be verified in SendGrid) | `no-reply@moriicoffee.com` |
| `FromName` | string | Display name shown in email clients | `Morii Coffee` |
| `StorefrontUrl` | string | Homepage URL for "Visit Store" button in welcome emails | `http://localhost:3000` (dev)<br>`https://moriicoffee.com` (prod) |
| `ResetPasswordBaseUrl` | string | Password reset page URL (token appended as query param) | `http://localhost:3000/reset-password` |
| `SendGrid.ApiKey` | string | SendGrid API key (see Step 1) | `SG.abc123...` |

---

#### Production Deployment

**Option A: appsettings.Production.json** (file-based configuration)

Create `source/MoriiCoffee.Presentation/appsettings.Production.json`:

```json
{
  "EmailSettings": {
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "https://moriicoffee.com",
    "ResetPasswordBaseUrl": "https://moriicoffee.com/reset-password",
    "SendGrid": {
      "ApiKey": "SG.production-api-key-from-secrets-manager"
    }
  }
}
```

**⚠️ Security Warning**: Do NOT commit production API keys to version control. Use environment variables (Option B) or secrets management instead.

---

**Option B: Environment Variables** (recommended for Docker/Kubernetes)

Set environment variables with double-underscore notation:

```bash
export EmailSettings__FromEmail="no-reply@moriicoffee.com"
export EmailSettings__FromName="Morii Coffee"
export EmailSettings__StorefrontUrl="https://moriicoffee.com"
export EmailSettings__ResetPasswordBaseUrl="https://moriicoffee.com/reset-password"
export EmailSettings__SendGrid__ApiKey="SG.production-api-key"
```

Or in Docker Compose / Kubernetes manifests:

```yaml
environment:
  - EmailSettings__FromEmail=no-reply@moriicoffee.com
  - EmailSettings__FromName=Morii Coffee
  - EmailSettings__StorefrontUrl=https://moriicoffee.com
  - EmailSettings__ResetPasswordBaseUrl=https://moriicoffee.com/reset-password
  - EmailSettings__SendGrid__ApiKey=SG.production-api-key
```

---

### 3. Verify Configuration

#### Start the Application

```bash
cd source/MoriiCoffee.Presentation
dotnet run --launch-profile "Development"
```

**Expected Output** (logs):
```
[Information] Application started
[Information] Email service: SendGridEmailService registered
```

**Configuration Error Example**:
If SendGrid API key is missing, you'll see:
```
[Error] Configuration binding failed: EmailSettings.SendGrid.ApiKey is required
```

---

#### Test Email Delivery

**Test 1: Welcome Email**

1. Send POST request to signup endpoint:
   ```bash
   curl -X POST http://localhost:8002/api/v1/auth/signup \
     -H "Content-Type: application/json" \
     -d '{
       "email": "testuser@example.com",
       "password": "Test@123456",
       "fullName": "Test User"
     }'
   ```

2. Check logs for email send confirmation:
   ```
   [Information] [SendGridEmailService] Email 'Welcome to Morii Coffee, Test User!' sent to testuser@example.com
   ```

3. Verify email received at `testuser@example.com` inbox

---

**Test 2: Password Reset Email**

1. Send POST request to forgot-password endpoint:
   ```bash
   curl -X POST http://localhost:8002/api/v1/auth/forgot-password \
     -H "Content-Type: application/json" \
     -d '{
       "email": "testuser@example.com"
     }'
   ```

2. Check logs for email send confirmation:
   ```
   [Information] [SendGridEmailService] Email 'Reset Your Morii Coffee Password' sent to testuser@example.com
   ```

3. Verify password reset email received

---

## Troubleshooting

### Email Not Sending

**Symptom**: No email received, logs show successful send

**Possible Causes**:
1. **SendGrid API key invalid**
   - Verify key in SendGrid dashboard (Settings → API Keys)
   - Ensure key has Mail Send permissions
   - Check for typos in appsettings.json

2. **Sender email not verified**
   - SendGrid requires sender verification
   - Go to SendGrid → Settings → Sender Authentication
   - Verify domain or single sender email

3. **Email in spam folder**
   - Check recipient spam/junk folder
   - SendGrid deliverability may require domain authentication (SPF/DKIM)

**Solution**:
```bash
# Check SendGrid API key validity
curl -X GET https://api.sendgrid.com/v3/scopes \
  -H "Authorization: Bearer YOUR_API_KEY"
# Should return 200 OK with scopes list
```

---

### Application Fails to Start

**Symptom**: Exception on startup

**Error Message**:
```
System.InvalidOperationException: Unable to resolve service for type 'MoriiCoffee.Domain.Shared.Settings.EmailSettings'
```

**Cause**: EmailSettings section missing from appsettings.json

**Solution**: Add EmailSettings section to your active configuration file (see Step 2)

---

### Logs Show SendGrid Error

**Symptom**: Logs show `Failed to send email` with status code

**Common Status Codes**:

| Status | Meaning | Solution |
|--------|---------|----------|
| 401 | Unauthorized (invalid API key) | Verify API key is correct and active |
| 403 | Forbidden (insufficient permissions) | Ensure API key has Mail Send permissions |
| 400 | Bad Request (invalid email format) | Check FromEmail is properly formatted |
| 429 | Rate limit exceeded | Reduce send frequency or upgrade SendGrid plan |

**Example Error Log**:
```
[Error] [SendGridEmailService] Failed to send email to testuser@example.com. Status: 401. Body: {"errors":[{"message":"The provided authorization grant is invalid, expired, or revoked"}]}
```

**Solution**: Generate new API key and update configuration

---

## What Changed (vs. AWS SES Support)

### Before (Multi-Provider Support)

```json
{
  "EmailSettings": {
    "Provider": "SendGrid",  // ❌ REMOVED
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "http://localhost:3000",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "SendGrid": {
      "ApiKey": "your-sendgrid-api-key"
    },
    "AwsSes": {  // ❌ REMOVED
      "Region": "ap-southeast-1",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key"
    }
  }
}
```

### After (SendGrid-Only)

```json
{
  "EmailSettings": {
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "http://localhost:3000",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "SendGrid": {
      "ApiKey": "your-sendgrid-api-key"
    }
  }
}
```

**Changes**:
- ❌ Removed `Provider` field (no provider selection needed)
- ❌ Removed `AwsSes` section (AWS SES was never implemented)
- ✅ All other fields unchanged

**Migration**: If your existing configuration has Provider or AwsSes fields, they will be ignored (no breaking changes). You can remove them for cleanliness, but it's optional.

---

## Email Templates

Email templates are embedded resources in the Infrastructure project. No configuration changes needed.

**Available Templates**:
- `welcome.html` - Sent on user signup
- `password-reset.html` - Sent on forgot-password request

**Location**: `source/MoriiCoffee.Infrastructure/Resources/EmailTemplates/`

**Customization**: Edit HTML files directly and rebuild the project. Templates are embedded at compile time.

---

## Production Checklist

Before deploying to production:

- [ ] SendGrid API key created with Mail Send permissions only (principle of least privilege)
- [ ] Sender email/domain verified in SendGrid (prevents deliverability issues)
- [ ] `FromEmail` matches verified sender in SendGrid
- [ ] `StorefrontUrl` and `ResetPasswordBaseUrl` point to production frontend URL (HTTPS)
- [ ] API key stored securely (environment variables, not committed to git)
- [ ] Test signup and password reset flows in staging environment
- [ ] Monitor SendGrid dashboard for delivery metrics and bounces
- [ ] Set up SPF/DKIM records for domain authentication (optional but recommended for deliverability)

---

## Further Reading

- [SendGrid API Documentation](https://docs.sendgrid.com/api-reference/mail-send/mail-send)
- [SendGrid Best Practices](https://docs.sendgrid.com/ui/sending-email/deliverability)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Feature 001: Email Service Integration](../001-email-social-auth/spec.md) (original implementation)

---

## Summary

- **Configuration**: One section (`EmailSettings` with `SendGrid` subsection)
- **Required Fields**: FromEmail, FromName, StorefrontUrl, ResetPasswordBaseUrl, SendGrid.ApiKey
- **Testing**: Use signup and forgot-password endpoints to verify email delivery
- **Production**: Use environment variables for API key security
- **No Migration Needed**: Existing SendGrid configurations work without changes

For implementation details, see [plan.md](./plan.md) and [data-model.md](./data-model.md).
