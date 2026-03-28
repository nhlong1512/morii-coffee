# Quickstart: Testing Google OAuth External Authentication

**Feature**: 005-google-oauth
**Created**: 2026-03-28

## Overview

This guide provides step-by-step instructions for manually testing the Google OAuth external authentication feature in the MoriiCoffee development environment.

---

## Prerequisites

### 1. Google Cloud Console Setup

Before testing, you must configure Google OAuth 2.0 credentials.

**Step 1**: Visit [Google Cloud Console](https://console.cloud.google.com/)

**Step 2**: Create a new project or select existing project:
- Project Name: `MoriiCoffee-Dev` (or your preferred name)
- Click "Create"

**Step 3**: Enable Google+ API (required for OAuth):
- Navigate to "APIs & Services" > "Library"
- Search for "Google+ API"
- Click "Enable"

**Step 4**: Create OAuth 2.0 Credentials:
- Navigate to "APIs & Services" > "Credentials"
- Click "Create Credentials" > "OAuth client ID"
- Application type: "Web application"
- Name: "MoriiCoffee Development"
- Authorized JavaScript origins:
  - `http://localhost:8002`
  - `http://localhost:3000` (if frontend runs on different port)
- Authorized redirect URIs:
  - `http://localhost:8002/api/v1/auth/external-auth-callback`
  - `http://localhost:8002/signin-google` (ASP.NET Core default callback path)
- Click "Create"

**Step 5**: Save credentials:
- Copy **Client ID** (format: `123456789-abc123def456.apps.googleusercontent.com`)
- Copy **Client Secret** (format: `GOCSPX-abc123def456xyz789`)

---

### 2. Configure MoriiCoffee Application

**Step 1**: Add Google OAuth configuration to `appsettings.Development.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID_HERE",
      "ClientSecret": "YOUR_CLIENT_SECRET_HERE"
    }
  }
}
```

**Alternative (Recommended for Security)**: Use .NET User Secrets:

```bash
cd source/MoriiCoffee.Presentation

dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID_HERE"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET_HERE"
```

**Step 2**: Verify configuration is loaded:

```bash
cd source/MoriiCoffee.Presentation
dotnet run --urls "http://localhost:8002"
```

Check console output for:
```
info: Microsoft.AspNetCore.Authentication.Google.GoogleHandler[0]
      Google authentication scheme is configured.
```

---

### 3. Start Development Environment

**Option 1**: Using Docker (Recommended):

```bash
cd deploy
bash run-docker-development.sh
```

**Option 2**: Using .NET CLI:

```bash
cd source/MoriiCoffee.Presentation
dotnet build --no-incremental
dotnet run --urls "http://localhost:8002"
```

**Verify API is running**:
```bash
curl http://localhost:8002/health
```

Expected response:
```json
{
  "status": "Healthy"
}
```

---

## Test Scenario 1: New User Sign-In with Google

**Objective**: Verify that a new user can sign in with Google and a customer account is created automatically.

### Step 1: Initiate OAuth Flow

**Using Browser**:
1. Open browser (incognito mode recommended)
2. Navigate to:
   ```
   http://localhost:8002/api/v1/auth/external-login?provider=Google&returnUrl=/dashboard
   ```
3. You should be redirected to Google's authentication page

**Using cURL**:
```bash
curl -X POST "http://localhost:8002/api/v1/auth/external-login?provider=Google&returnUrl=/dashboard" \
  -v \
  -L
```

**Expected**:
- HTTP 302 redirect to `https://accounts.google.com/o/oauth2/v2/auth?...`
- Response includes `Set-Cookie: .AspNetCore.Correlation.Google.{id}=...`

---

### Step 2: Complete Google Authentication

1. Sign in with your Google account (use a test account, not production)
2. Grant permissions when prompted:
   - "MoriiCoffee Development wants to access your Google Account"
   - Permissions: View email address, View basic profile info
3. Click "Allow"

**Expected**:
- Google redirects to: `http://localhost:8002/api/v1/auth/external-auth-callback?code=...&state=...`
- Backend processes callback automatically

---

### Step 3: Verify Callback Success

**Expected Redirect**:
- HTTP 302 redirect to: `http://localhost:8002/dashboard` (or your returnUrl)
- Response includes `Set-Cookie: AuthTokenHolder=...`

**Extract Token Cookie**:

If testing via browser:
1. Open browser DevTools (F12)
2. Go to "Application" > "Cookies"
3. Find cookie named `AuthTokenHolder`
4. Copy cookie value (URL-encoded JSON)
5. Decode using online tool or JavaScript console:
   ```javascript
   JSON.parse(decodeURIComponent('YOUR_COOKIE_VALUE_HERE'))
   ```

Expected cookie structure:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

### Step 4: Verify Database Entries

**Connect to SQL Server**:
```bash
docker exec -it morii-coffee-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourPassword123'
```

**Query 1: Verify User Created**:
```sql
USE MoriiCoffeeDb;
GO

SELECT Id, Email, UserName, FullName, EmailConfirmed, Status, CreatedAt
FROM AspNetUsers
WHERE Email = 'your-google-email@gmail.com';
GO
```

Expected result:
- 1 row returned
- `EmailConfirmed` = 1 (true)
- `Status` = 0 (Active)
- `CreatedAt` = recent timestamp

**Query 2: Verify Google Link**:
```sql
SELECT ul.LoginProvider, ul.ProviderKey, ul.ProviderDisplayName, u.Email
FROM AspNetUserLogins ul
JOIN AspNetUsers u ON ul.UserId = u.Id
WHERE u.Email = 'your-google-email@gmail.com';
GO
```

Expected result:
- 1 row returned
- `LoginProvider` = "Google"
- `ProviderKey` = Google User ID (long numeric string)
- `ProviderDisplayName` = "Google"

**Query 3: Verify CUSTOMER Role Assigned**:
```sql
SELECT u.Email, r.Name AS RoleName
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'your-google-email@gmail.com';
GO
```

Expected result:
- 1 row returned
- `RoleName` = "CUSTOMER"

**Query 4: Verify Refresh Token Stored**:
```sql
SELECT ut.LoginProvider, ut.Name, LEFT(ut.Value, 20) AS TokenPreview
FROM AspNetUserTokens ut
JOIN AspNetUsers u ON ut.UserId = u.Id
WHERE u.Email = 'your-google-email@gmail.com';
GO
```

Expected result:
- 1 row returned
- `LoginProvider` = "Google"
- `Name` = "REFRESH"
- `TokenPreview` = (first 20 characters of refresh token)

---

### Step 5: Test Access Token Authentication

**Extract Access Token from Cookie** (see Step 3).

**Make Authenticated API Request**:
```bash
curl -X GET "http://localhost:8002/api/v1/users/me" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```

Expected response:
```json
{
  "id": "a1b2c3d4-...",
  "email": "your-google-email@gmail.com",
  "fullName": "Your Name",
  "roles": ["CUSTOMER"],
  "status": "Active"
}
```

---

### Step 6: Verify Welcome Email Sent

**Check Email Inbox**:
- Check inbox for `your-google-email@gmail.com`
- Look for email with subject: "Welcome to MoriiCoffee!"
- Verify email contains:
  - User's full name
  - Account creation confirmation
  - MoriiCoffee branding

**If Email Not Received**:
1. Check spam/junk folder
2. Verify email service configuration in `appsettings.json`
3. Check application logs for email service errors:
   ```bash
   docker logs morii-coffee-api | grep -i "email"
   ```

---

## Test Scenario 2: Existing User Linking

**Objective**: Verify that signing in with Google links to an existing account with matching email.

### Precondition: Create User with Email/Password

**Step 1**: Sign up with email/password first:

```bash
curl -X POST "http://localhost:8002/api/v1/auth/sign-up" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "existing-user@gmail.com",
    "password": "Password123!",
    "fullName": "Existing User"
  }'
```

**Step 2**: Verify user exists in database:
```sql
SELECT Id, Email, UserName, FullName
FROM AspNetUsers
WHERE Email = 'existing-user@gmail.com';
GO
```

---

### Test: Sign In with Google Using Same Email

**Step 1**: Initiate OAuth flow:
```
http://localhost:8002/api/v1/auth/external-login?provider=Google&returnUrl=/
```

**Step 2**: Sign in with Google account that has email `existing-user@gmail.com`

**Step 3**: Verify Google is linked to existing account:
```sql
SELECT u.Id, u.Email, ul.LoginProvider, ul.ProviderKey
FROM AspNetUsers u
LEFT JOIN AspNetUserLogins ul ON u.Id = ul.UserId
WHERE u.Email = 'existing-user@gmail.com';
GO
```

Expected result:
- 1 user row returned
- `LoginProvider` = "Google"
- `ProviderKey` = Google User ID
- User ID matches the ID from precondition step

**Step 4**: Verify NO duplicate user created:
```sql
SELECT COUNT(*) AS UserCount
FROM AspNetUsers
WHERE Email = 'existing-user@gmail.com';
GO
```

Expected result:
- `UserCount` = 1 (not 2)

**Step 5**: Verify NO welcome email sent (user already exists).

---

## Test Scenario 3: Returning User Sign-In

**Objective**: Verify that a user who previously linked Google can sign in again.

### Precondition: Complete Test Scenario 1 or 2

User must have Google already linked in AspNetUserLogins table.

---

### Test: Sign In Again with Same Google Account

**Step 1**: Clear browser cookies (simulate new session)

**Step 2**: Initiate OAuth flow:
```
http://localhost:8002/api/v1/auth/external-login?provider=Google&returnUrl=/products
```

**Step 3**: Sign in with same Google account as before

**Step 4**: Verify tokens are issued:
- Extract `AuthTokenHolder` cookie
- Decode access token and refresh token

**Step 5**: Verify refresh token was REPLACED in database:
```sql
SELECT ut.LoginProvider, ut.Name, ut.Value, u.Email
FROM AspNetUserTokens ut
JOIN AspNetUsers u ON ut.UserId = u.Id
WHERE u.Email = 'your-google-email@gmail.com'
  AND ut.LoginProvider = 'Google'
  AND ut.Name = 'REFRESH';
GO
```

Expected result:
- 1 row returned (not 2)
- Token value is different from previous sign-in (replaced, not duplicated)

---

## Test Scenario 4: Token Refresh Flow

**Objective**: Verify that refresh tokens can obtain new access tokens without re-authentication.

### Precondition: Complete OAuth sign-in to obtain refresh token

**Step 1**: Extract refresh token from `AuthTokenHolder` cookie (see Scenario 1, Step 3)

**Step 2**: Wait for access token to expire (or use expired token for testing)

**Step 3**: Call refresh-token endpoint:
```bash
curl -X POST "http://localhost:8002/api/v1/auth/refresh-token" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

Expected response:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-03-28T20:00:00Z"
}
```

**Step 4**: Verify new access token works:
```bash
curl -X GET "http://localhost:8002/api/v1/users/me" \
  -H "Authorization: Bearer NEW_ACCESS_TOKEN_HERE"
```

Expected: HTTP 200 with user profile.

---

## Test Scenario 5: Error Scenarios

### Test 5A: User Denies Permission

**Step 1**: Initiate OAuth flow
**Step 2**: On Google consent screen, click "Cancel" or "Deny"

**Expected**:
- Redirect to error page or login page with error message
- Query parameter: `error=access_denied`
- No user account created
- No tokens issued

---

### Test 5B: Invalid Provider

**Step 1**: Call external-login with unsupported provider:
```bash
curl -X POST "http://localhost:8002/api/v1/auth/external-login?provider=Facebook" \
  -H "Content-Type: application/json"
```

**Expected**:
- HTTP 400 Bad Request
- Error message: "Unsupported provider 'Facebook'. Only 'Google' is supported."

---

### Test 5C: Invalid State Parameter (CSRF Attack Simulation)

**Step 1**: Initiate OAuth flow and get redirect to Google
**Step 2**: After Google authentication, intercept callback URL
**Step 3**: Modify `state` parameter to arbitrary value
**Step 4**: Submit modified callback URL

```bash
curl "http://localhost:8002/api/v1/auth/external-auth-callback?code=VALID_CODE&state=TAMPERED_STATE" \
  -H "Cookie: .AspNetCore.Correlation.Google.abc123=ORIGINAL_STATE"
```

**Expected**:
- HTTP 401 Unauthorized
- Error message: "Invalid OAuth state. Please restart the sign-in process."
- No tokens issued

---

### Test 5D: Expired OAuth Flow

**Step 1**: Initiate OAuth flow
**Step 2**: Wait 20 minutes (correlation cookie expires after 15 minutes)
**Step 3**: Complete Google authentication

**Expected**:
- HTTP 401 Unauthorized
- Error message: "OAuth session expired. Please restart the sign-in process."

---

### Test 5E: Inactive Account

**Precondition**: Create user account and mark as inactive:
```sql
UPDATE AspNetUsers
SET Status = 1  -- Inactive
WHERE Email = 'inactive-user@gmail.com';
GO
```

**Step 1**: Sign in with Google using `inactive-user@gmail.com`

**Expected**:
- HTTP 403 Forbidden
- Error message: "Your account has been deactivated. Contact support for assistance."
- No tokens issued

---

## Test Scenario 6: Multiple Sign-In Sessions

**Objective**: Verify that signing in on multiple devices/browsers works correctly.

**Step 1**: Sign in with Google on Device A (e.g., Chrome)
- Extract access token A and refresh token A

**Step 2**: Sign in with Google on Device B (e.g., Firefox)
- Extract access token B and refresh token B

**Step 3**: Verify both access tokens work:
```bash
# Device A token
curl -X GET "http://localhost:8002/api/v1/users/me" \
  -H "Authorization: Bearer ACCESS_TOKEN_A"

# Device B token
curl -X GET "http://localhost:8002/api/v1/users/me" \
  -H "Authorization: Bearer ACCESS_TOKEN_B"
```

**Expected**: Both return HTTP 200 with user profile.

**Step 4**: Check database for refresh tokens:
```sql
SELECT ut.LoginProvider, ut.Name, ut.Value
FROM AspNetUserTokens ut
JOIN AspNetUsers u ON ut.UserId = u.Id
WHERE u.Email = 'your-google-email@gmail.com'
  AND ut.LoginProvider = 'Google';
GO
```

**Expected**:
- 1 row returned (refresh token B)
- Refresh token A was replaced by refresh token B (latest sign-in wins)

---

## Troubleshooting

### Issue 1: "External authentication is not configured"

**Cause**: Google OAuth credentials missing from appsettings.json or User Secrets.

**Solution**:
1. Verify `Authentication:Google:ClientId` is set
2. Verify `Authentication:Google:ClientSecret` is set
3. Restart application after adding configuration

---

### Issue 2: "redirect_uri_mismatch" Error from Google

**Cause**: Redirect URI in OAuth request doesn't match authorized URIs in Google Cloud Console.

**Solution**:
1. Go to Google Cloud Console > Credentials
2. Edit OAuth 2.0 Client ID
3. Add exact redirect URI: `http://localhost:8002/api/v1/auth/external-auth-callback`
4. Click "Save"
5. Wait 1-2 minutes for changes to propagate
6. Retry OAuth flow

---

### Issue 3: Tokens Not in Cookie After Callback

**Cause**: Browser security settings blocking cookies, or cookie extraction failed.

**Solution**:
1. Check browser console for errors
2. Verify cookies are enabled in browser settings
3. Check cookie flags (Secure flag requires HTTPS in production, but HTTP is OK for localhost)
4. Inspect Network tab in DevTools to see `Set-Cookie` header in callback response

---

### Issue 4: "Invalid OAuth state" Error

**Cause**: Correlation cookie expired or was deleted during OAuth flow.

**Solution**:
1. Complete OAuth flow within 15 minutes
2. Don't clear cookies between `/external-login` and `/external-auth-callback` steps
3. Restart OAuth flow if error occurs

---

### Issue 5: Welcome Email Not Received

**Cause**: Email service configuration issue or SMTP failure.

**Solution**:
1. Check application logs for email errors
2. Verify Brevo API key is configured correctly
3. Verify sender email is verified in Brevo
4. Check Brevo dashboard for delivery status
5. Note: Email failure is non-blocking (user still authenticated)

---

## Verification Checklist

Use this checklist to verify complete OAuth implementation:

- [ ] New user can sign in with Google
- [ ] New user account created in AspNetUsers table
- [ ] Google link created in AspNetUserLogins table
- [ ] CUSTOMER role assigned in AspNetUserRoles table
- [ ] Refresh token stored in AspNetUserTokens table
- [ ] Welcome email sent to new user
- [ ] Existing user with matching email can link Google account
- [ ] No duplicate accounts created when linking
- [ ] Returning user can sign in with Google
- [ ] Refresh token replaced (not duplicated) on each sign-in
- [ ] Access token authenticates API requests
- [ ] Refresh token obtains new access tokens
- [ ] User denial error handled gracefully
- [ ] Invalid provider error returns 400
- [ ] Invalid state parameter returns 401
- [ ] Expired OAuth flow returns 401
- [ ] Inactive account returns 403
- [ ] Multiple devices can sign in simultaneously

---

## Performance Benchmarks

### Expected Timings

| Metric | Target | Measurement Method |
|--------|--------|--------------------|
| OAuth initiation | <200ms | Time from POST /external-login to redirect |
| Google authentication | <5s | User time on Google consent screen (user-dependent) |
| Callback processing | <1s | Time from callback to final redirect |
| Total flow (end-to-end) | <30s | Time from click to authenticated state |
| Database queries | <100ms | Time to create user + links + role |
| Email delivery | <60s | Time from sign-in to email received |

### Monitoring Query

```sql
-- Check OAuth sign-in count by date
SELECT CAST(CreatedAt AS DATE) AS SignInDate, COUNT(*) AS GoogleSignIns
FROM AspNetUsers
WHERE Id IN (
    SELECT UserId FROM AspNetUserLogins WHERE LoginProvider = 'Google'
)
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY SignInDate DESC;
GO
```

---

## Next Steps

After completing manual testing:

1. **Document Results**: Update test results in `specs/005-google-oauth/test-results.md`
2. **Create Summary Docs**: Generate VN and ENG summaries in `docs/explainations/`
3. **Update CLAUDE.md**: Run `.specify/scripts/bash/update-agent-context.sh claude`
4. **Commit Changes**: Create git commit with descriptive message
5. **Production Deployment**:
   - Update authorized redirect URIs in Google Cloud Console for production domain
   - Store ClientId and ClientSecret in production environment variables (not appsettings.json)
   - Enable HTTPS for all OAuth endpoints
   - Test OAuth flow in staging environment before production deployment

---

## References

- **Google OAuth 2.0 Documentation**: https://developers.google.com/identity/protocols/oauth2
- **ASP.NET Core External Authentication**: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/
- **Feature Specification**: [spec.md](spec.md)
- **Implementation Plan**: [plan.md](plan.md)
- **API Contracts**: [contracts/](contracts/)
