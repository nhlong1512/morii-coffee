# Quickstart Guide: Email Integration and Social Login

**Feature**: Email Integration and Social Login Planning
**Date**: 2026-03-23
**Audience**: Developers

This guide provides step-by-step instructions for testing email integration (Part 1) and setting up OAuth2 social login credentials (Part 2 - for future implementation).

---

## Part 1: Email Testing Quickstart

### Prerequisites

Before testing email functionality, ensure you have:

1. **SendGrid Account**: Sign up at [sendgrid.com](https://sendgrid.com/)
2. **Verified Sender Email**: Complete SendGrid sender verification for `no-reply@moriicoffee.com` (or your domain)
3. **API Key**: Generate SendGrid API key with "Mail Send" permissions

### Configuration Setup

#### Step 1: Update appsettings.Development.json

Add SendGrid configuration to the development settings file:

```json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "FromEmail": "no-reply@moriicoffee.com",
    "FromName": "Morii Coffee",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password",
    "SendGrid": {
      "ApiKey": "SG.xxxx..."
    }
  }
}
```

**Important**: Never commit `appsettings.Development.json` with real API keys to version control.

#### Step 2: Set Environment Variable (Alternative)

For production or sensitive environments, use environment variables instead of appsettings:

**macOS/Linux**:
```bash
export EmailSettings__SendGrid__ApiKey="SG.xxxx..."
export EmailSettings__ResetPasswordBaseUrl="http://localhost:3000/reset-password"
```

**Windows (PowerShell)**:
```powershell
$env:EmailSettings__SendGrid__ApiKey = "SG.xxxx..."
$env:EmailSettings__ResetPasswordBaseUrl = "http://localhost:3000/reset-password"
```

**Docker Compose**:
```yaml
services:
  api:
    environment:
      - EmailSettings__SendGrid__ApiKey=SG.xxxx...
      - EmailSettings__ResetPasswordBaseUrl=http://localhost:3000/reset-password
```

### Testing Welcome Email

#### Step 1: Start Backend

```bash
cd /Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee
dotnet run --project source/MoriiCoffee.Presentation
```

Verify backend is running: `http://localhost:5000/swagger`

#### Step 2: Create Test User Account

Open Swagger UI: `http://localhost:5000/swagger`

Navigate to `POST /api/v1/auth/signup` and execute with test data:

```json
{
  "email": "your-test-email@gmail.com",
  "userName": "testuser",
  "password": "Test@1234",
  "confirmPassword": "Test@1234"
}
```

**Expected Response (200 OK)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "opaque_refresh_token",
  "user": {
    "id": "...",
    "email": "your-test-email@gmail.com",
    "userName": "testuser",
    ...
  }
}
```

#### Step 3: Verify Welcome Email

1. Check your test email inbox (within 60 seconds)
2. Verify email from "Morii Coffee <no-reply@moriicoffee.com>"
3. Verify email contains:
   - Your username: "testuser"
   - Welcome message in Morii Coffee brand tone
   - "Shop Now" button linking to storefront (`http://localhost:3000`)
   - Morii Coffee branding (colors, logo)

**Email Preview**:
```
Subject: Welcome to Morii Coffee!

Hi testuser,

Welcome to Morii Coffee! We're thrilled to have you join our community of coffee lovers.

Your account has been successfully created. Start exploring our selection of premium coffee beans, brewing equipment, and more.

[Shop Now]

If you have any questions, feel free to reach out to our support team.

Best regards,
The Morii Coffee Team
```

#### Step 4: Check Backend Logs

Review backend logs to confirm email send attempt:

```bash
# Look for structured log entries
[INF] Welcome email sent to your-test-email@gmail.com
# or
[WRN] Welcome email failed: StatusCode 401 (if API key is invalid)
```

### Testing Password Reset Email

#### Step 1: Request Password Reset

In Swagger UI, navigate to `POST /api/v1/auth/forgot-password` and execute:

```json
{
  "email": "your-test-email@gmail.com"
}
```

**Expected Response (200 OK)**:
```json
{
  "success": true,
  "message": "If this email is registered, a password reset link has been sent."
}
```

**Note**: Response is always success (does not reveal if email exists for security reasons).

#### Step 2: Verify Password Reset Email

1. Check your test email inbox (within 60 seconds)
2. Verify email from "Morii Coffee <no-reply@moriicoffee.com>"
3. Verify email contains:
   - "Reset Password" call-to-action button
   - Reset link format: `http://localhost:3000/reset-password?token=...&email=your-test-email@gmail.com`
   - Expiry notice (e.g., "This link expires in 1 hour")

**Email Preview**:
```
Subject: Reset Your Morii Coffee Password

Hi,

We received a request to reset your password for your Morii Coffee account.

Click the button below to create a new password:

[Reset Password]

This link will expire in 1 hour. If you didn't request this password reset, you can safely ignore this email.

Best regards,
The Morii Coffee Team
```

#### Step 3: Click Reset Link

1. Click "Reset Password" button in email
2. Verify redirect to frontend: `http://localhost:3000/reset-password?token=...&email=...`
3. Verify frontend reset password form loads with email pre-filled
4. Submit new password on frontend

**Expected Frontend Behavior**:
- Form displays with token and email from URL query params
- Submit calls `POST /api/v1/auth/reset-password` with token and new password
- On success, redirect to sign-in page with success message

### Testing Email Failure Handling

#### Test 1: Invalid API Key (Graceful Degradation)

1. Update `appsettings.Development.json` with invalid API key:
```json
{
  "EmailSettings": {
    "SendGrid": {
      "ApiKey": "INVALID_KEY"
    }
  }
}
```

2. Restart backend
3. Call `POST /api/v1/auth/signup` with new user
4. **Expected Behavior**:
   - Signup still succeeds (returns 200 OK with JWT tokens)
   - User can sign in immediately
   - Backend logs email send failure: `[ERR] SendGrid exception sending welcome email to ...`

**Verification**: Confirm signup does not block on email failure (graceful degradation working)

#### Test 2: Network Timeout

1. Disconnect network or set invalid SendGrid base URL
2. Call `POST /api/v1/auth/signup`
3. **Expected Behavior**: Same as Test 1 (signup succeeds, email failure logged)

### Testing Password Reset Token Expiry

#### Step 1: Generate Reset Token

1. Call `POST /api/v1/auth/forgot-password`
2. Note the reset token from email

#### Step 2: Wait for Token Expiry

- **Default Expiry**: 1 hour (configured in ASP.NET Identity)
- For faster testing, temporarily reduce expiry in `IdentityOptions`:

```csharp
// In Program.cs or IdentityConfiguration.cs
services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(5);  // Test expiry
});
```

#### Step 3: Attempt to Use Expired Token

1. Wait for token to expire (5 minutes in test config)
2. Call `POST /api/v1/auth/reset-password` with expired token
3. **Expected Response (400 Bad Request)**:
```json
{
  "error": "INVALID_TOKEN",
  "message": "The password reset token is invalid or expired. Please request a new password reset."
}
```

### Testing Concurrent Password Reset Requests

#### Step 1: Request Reset Twice

1. Call `POST /api/v1/auth/forgot-password` → Email 1 (Token A)
2. Wait 10 seconds
3. Call `POST /api/v1/auth/forgot-password` again → Email 2 (Token B)

#### Step 2: Attempt to Use Old Token

1. Extract Token A from Email 1
2. Call `POST /api/v1/auth/reset-password` with Token A
3. **Expected Response (400 Bad Request)**: Token A is invalid (superseded by Token B)

#### Step 3: Use Latest Token

1. Extract Token B from Email 2
2. Call `POST /api/v1/auth/reset-password` with Token B
3. **Expected Response (200 OK)**: Password successfully reset

**Rationale**: ASP.NET Identity invalidates old tokens when new token is generated (security best practice).

---

## Part 2: Social Login Setup Quickstart

**Note**: Social login is planning only (Part 2). This section documents the setup process for future implementation.

### Google OAuth2 Setup

#### Step 1: Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click "Select a project" → "New Project"
3. Project name: "Morii Coffee"
4. Click "Create"

#### Step 2: Enable Google+ API

1. In the left sidebar, navigate to "APIs & Services" → "Enabled APIs & services"
2. Click "+ ENABLE APIS AND SERVICES"
3. Search for "Google+ API"
4. Click "Enable"

#### Step 3: Create OAuth2 Credentials

1. Navigate to "APIs & Services" → "Credentials"
2. Click "+ CREATE CREDENTIALS" → "OAuth client ID"
3. Configure consent screen (if prompted):
   - User type: "External"
   - App name: "Morii Coffee"
   - User support email: Your email
   - Developer contact: Your email
   - Scopes: None (use default)
   - Test users: Add your test email
4. Application type: "Web application"
5. Name: "Morii Coffee Web App"
6. Authorized redirect URIs:
   - `http://localhost:3000/auth/callback` (development)
   - `https://moriicoffee.com/auth/callback` (production)
7. Click "Create"
8. Copy **Client ID** and **Client Secret**

#### Step 4: Configure Backend

Add to `appsettings.Development.json`:

```json
{
  "OAuth2Settings": {
    "Google": {
      "ClientId": "123456789-abc.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-xxxxx",
      "RedirectUri": "http://localhost:3000/auth/callback"
    }
  }
}
```

### Meta (Facebook) OAuth2 Setup

#### Step 1: Create Meta Developer Account

1. Go to [Meta for Developers](https://developers.facebook.com/)
2. Click "My Apps" → "Create App"
3. App type: "Consumer"
4. App name: "Morii Coffee"
5. App contact email: Your email
6. Click "Create App"

#### Step 2: Add Facebook Login Product

1. In your app dashboard, click "+ Add Product"
2. Find "Facebook Login" → Click "Set Up"
3. Platform: "Web"
4. Site URL: `https://moriicoffee.com` (or `http://localhost:3000` for testing)

#### Step 3: Configure OAuth Redirect URIs

1. In the left sidebar, navigate to "Facebook Login" → "Settings"
2. Valid OAuth Redirect URIs:
   - `http://localhost:3000/auth/callback` (development)
   - `https://moriicoffee.com/auth/callback` (production)
3. Click "Save Changes"

#### Step 4: Get App Credentials

1. In the left sidebar, navigate to "Settings" → "Basic"
2. Copy **App ID** and **App Secret**

#### Step 5: Submit for App Review (Production Only)

**Note**: Email scope requires App Review for production. For development/testing, use test users.

1. Navigate to "App Review" → "Permissions and Features"
2. Request "email" permission
3. Provide use case and privacy policy URL
4. Submit for review (approval takes 2-5 business days)

#### Step 6: Configure Backend

Add to `appsettings.Development.json`:

```json
{
  "OAuth2Settings": {
    "Meta": {
      "AppId": "1234567890",
      "AppSecret": "abc123...",
      "RedirectUri": "http://localhost:3000/auth/callback"
    }
  }
}
```

### Testing Social Login (Post-Implementation)

#### Test 1: Google Sign-In

1. Start frontend: `pnpm dev` (in frontend directory)
2. Navigate to sign-in page: `http://localhost:3000/signin`
3. Click "Sign in with Google"
4. **Expected Behavior**:
   - Redirects to Google authorization page (`accounts.google.com`)
   - Shows Google consent screen ("Morii Coffee wants to access your Google Account")
   - After authorization, redirects back to `http://localhost:3000/auth/callback?code=...&state=...`
   - Frontend exchanges code for JWT tokens via `POST /api/v1/auth/social-login`
   - JWT tokens stored in Zustand authStore
   - User profile displayed in navigation bar
   - User can access protected routes (e.g., `/profile`, `/orders`)

#### Test 2: Meta Sign-In

1. Navigate to sign-in page: `http://localhost:3000/signin`
2. Click "Sign in with Meta"
3. **Expected Behavior**: Same as Google flow, but with Meta consent screen

#### Test 3: Account Linking (Email Match)

1. Create local account with email: `user@example.com` (via email/password signup)
2. Sign in with Google using same email: `user@example.com`
3. **Expected Behavior**:
   - Google account linked to existing local account
   - User can sign in via email/password OR Google
   - User profile shows `externalProvider: "Google"` and `hasPassword: true`

#### Test 4: Email Conflict (Different Provider)

1. Create account with Google: `user@example.com`
2. Attempt to sign in with Meta using same email: `user@example.com`
3. **Expected Behavior**:
   - 409 Conflict error returned
   - Frontend shows error message: "This email is already linked to Google. Please sign in with Google."

#### Test 5: Unverified Email (Security Check)

1. Use test Google account with unverified email
2. Attempt social login
3. **Expected Behavior**:
   - 400 Bad Request error returned
   - Frontend shows error message: "Please verify your email with Google before using social login."

---

## Troubleshooting

### Issue: Welcome Email Not Received

**Possible Causes**:
1. Invalid SendGrid API key → Check backend logs for authentication errors
2. Email in spam folder → Check spam; verify SendGrid domain authentication
3. Invalid sender email → Verify sender email is verified in SendGrid dashboard
4. Rate limit exceeded → Check SendGrid dashboard for rate limit errors

**Solution**:
- Verify API key has "Mail Send" permissions
- Complete SendGrid domain authentication (SPF, DKIM)
- Check SendGrid dashboard "Activity" tab for delivery status

### Issue: Password Reset Link Invalid

**Possible Causes**:
1. Token expired → Tokens expire after configured time (default 1 hour)
2. Token used twice → Tokens are single-use only
3. Token superseded → New reset request invalidates old token

**Solution**:
- Request new password reset
- Use latest reset email
- Verify frontend is not caching old tokens

### Issue: Signup Succeeds But Email Fails

**Expected Behavior**: This is graceful degradation (feature, not bug). Email failures do not block user operations.

**Verification**:
- Check backend logs for email send errors
- User can still sign in and use account
- Fix email configuration and retry

### Issue: OAuth2 Redirect URL Mismatch

**Error**: `redirect_uri_mismatch` from Google/Meta

**Solution**:
- Verify redirect URI in OAuth2 provider settings matches exactly (including protocol, port, path)
- For Google: Check "Authorized redirect URIs" in OAuth2 credentials
- For Meta: Check "Valid OAuth Redirect URIs" in Facebook Login settings

### Issue: State Parameter Invalid (CSRF Error)

**Possible Causes**:
1. State expired (10-minute timeout)
2. State mismatch (frontend and backend out of sync)
3. CSRF attack (legitimate security block)

**Solution**:
- Retry OAuth flow from beginning
- Clear browser session storage
- Verify state parameter is generated and validated correctly

---

## Configuration Reference

### EmailSettings (appsettings.json)

| Key | Type | Description | Example |
|-----|------|-------------|---------|
| `Provider` | string | Email service provider | `"SendGrid"` or `"AwsSes"` |
| `FromEmail` | string | Sender email address | `"no-reply@moriicoffee.com"` |
| `FromName` | string | Sender display name | `"Morii Coffee"` |
| `ResetPasswordBaseUrl` | string | Frontend reset password URL | `"http://localhost:3000/reset-password"` |
| `SendGrid.ApiKey` | string | SendGrid API key | `"SG.xxxx..."` |

### OAuth2Settings (appsettings.json) - Planned

| Key | Type | Description | Example |
|-----|------|-------------|---------|
| `Google.ClientId` | string | Google OAuth2 client ID | `"123456.apps.googleusercontent.com"` |
| `Google.ClientSecret` | string | Google OAuth2 client secret | `"GOCSPX-xxxxx"` |
| `Google.RedirectUri` | string | OAuth2 callback URL | `"http://localhost:3000/auth/callback"` |
| `Meta.AppId` | string | Meta (Facebook) app ID | `"1234567890"` |
| `Meta.AppSecret` | string | Meta (Facebook) app secret | `"abc123..."` |
| `Meta.RedirectUri` | string | OAuth2 callback URL | `"http://localhost:3000/auth/callback"` |

---

## Next Steps

### Email Integration (Immediate)

1. Follow configuration setup above
2. Test welcome email on signup
3. Test password reset email
4. Verify email failures do not block operations
5. Verify email templates display correctly in Gmail, Outlook, Apple Mail

### Social Login (Future Implementation)

1. Complete Google OAuth2 setup
2. Complete Meta OAuth2 setup
3. Implement backend endpoints (`POST /api/v1/auth/social-login`, `GET /api/v1/auth/social-login/{provider}/authorization-url`)
4. Implement frontend components (`SocialLoginButtons`, `OAuth2CallbackHandler`)
5. Test OAuth2 flows end-to-end
6. Test account linking and conflict scenarios
7. Test security features (state parameter validation, email verification)

---

## References

- [SendGrid Documentation](https://docs.sendgrid.com/)
- [Google OAuth2 Setup Guide](https://developers.google.com/identity/protocols/oauth2/web-server)
- [Meta OAuth2 Setup Guide](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow)
- [ASP.NET Core Identity Configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration)
