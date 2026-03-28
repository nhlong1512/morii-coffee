# API Contract: External Login Callback

**Feature**: 005-google-oauth
**Endpoint**: `GET /api/v1/auth/external-auth-callback`
**Created**: 2026-03-28

## Overview

Processes the OAuth 2.0 callback from Google after user authentication. This endpoint receives the authorization code, exchanges it for user profile information, creates or links the user account, generates JWT tokens, and redirects the user to the requested page with tokens in a secure cookie.

---

## Request

### HTTP Method
`GET`

### URL
```
GET /api/v1/auth/external-auth-callback
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `code` | string | Yes | Authorization code issued by Google. Single-use token valid for ~10 seconds. |
| `state` | string | Yes | CSRF protection token. Must match the state value stored in the correlation cookie. |
| `returnUrl` | string | No | URL to redirect after successful authentication. Defaults to "/" if not provided. |
| `error` | string | No | Error code from Google if user denied permission (e.g., "access_denied"). |
| `error_description` | string | No | Human-readable error description from Google. |

### Request Headers

**Required Cookies**:
- `.AspNetCore.Correlation.Google.{correlation_id}`: State validation cookie set during `/external-login` request.

### Request Body

None (OAuth callback uses query parameters only).

### Example Request - Success

```http
GET /api/v1/auth/external-auth-callback?code=4/0AXyz123abc&state=CfDJ8NQw...&returnUrl=%2Fdashboard HTTP/1.1
Host: localhost:8002
Cookie: .AspNetCore.Correlation.Google.abc123=CfDJ8NQw...
```

### Example Request - User Denial

```http
GET /api/v1/auth/external-auth-callback?error=access_denied&error_description=User+denied+permission&state=CfDJ8NQw... HTTP/1.1
Host: localhost:8002
Cookie: .AspNetCore.Correlation.Google.abc123=CfDJ8NQw...
```

---

## Response

### Success Response (HTTP 302 Found) - New User

**Scenario**: User signs in with Google for the first time, new account created.

**Redirect URL**:
```
HTTP/1.1 302 Found
Location: /dashboard
Set-Cookie: AuthTokenHolder=eyJhY2Nlc3NUb2tlbiI6ImV5SmhiR2NpT2lKSVV6STFOaUlzSW5SNWNDSTZJa3BYVkNKOS4uLiIsInJlZnJlc2hUb2tlbiI6ImV5SmhiR2NpT2lKSVV6STFOaUlzSW5SNWNDSTZJa3BYVkNKOS4uLiJ9; path=/; max-age=300; secure; httponly; samesite=strict
```

**Cookie Contents** (AuthTokenHolder):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Cookie Properties**:
- Name: `AuthTokenHolder`
- Max-Age: 300 seconds (5 minutes)
- Flags: `Secure`, `HttpOnly`, `SameSite=Strict`
- Path: `/`

**Backend Processing**:
1. Exchange authorization code for Google user profile
2. Check if email exists in AspNetUsers table
3. Create new User entity with:
   - Email from Google profile
   - UserName generated from email prefix
   - FullName from Google profile
   - PhoneNumber from Google profile (if available)
   - EmailConfirmed = true
   - Status = Active
4. Insert into AspNetUserLogins:
   - LoginProvider = "Google"
   - ProviderKey = Google User ID
   - UserId = new User ID
5. Assign CUSTOMER role via AspNetUserRoles
6. Generate access token and refresh token
7. Store refresh token in AspNetUserTokens
8. Send welcome email
9. Redirect to returnUrl with token cookie

---

### Success Response (HTTP 302 Found) - Existing User

**Scenario**: User with matching email signs in with Google for the first time, account linked.

**Same redirect format as above**. Backend processing differs:

1. Exchange authorization code for Google user profile
2. Find existing User by email
3. Check if Google is already linked (query AspNetUserLogins)
4. If not linked, insert into AspNetUserLogins:
   - LoginProvider = "Google"
   - ProviderKey = Google User ID
   - UserId = existing User ID
5. Generate access token and refresh token
6. Store refresh token in AspNetUserTokens
7. No welcome email (user already exists)
8. Redirect to returnUrl with token cookie

---

### Success Response (HTTP 302 Found) - Returning User

**Scenario**: User who previously linked Google account signs in again.

**Same redirect format**. Backend processing:

1. Exchange authorization code for Google user profile
2. Find existing User via AspNetUserLogins by ProviderKey
3. Generate new access token and refresh token
4. Replace existing refresh token in AspNetUserTokens
5. Redirect to returnUrl with token cookie

---

## Error Responses

### 400 Bad Request - User Denied Permission

**Scenario**: User clicked "Cancel" or "Deny" on Google's consent screen.

```http
HTTP/1.1 302 Found
Location: /login?error=access_denied&message=You+must+grant+permission+to+sign+in+with+Google
```

**Alternative**: Return JSON error if `Accept: application/json` header is present:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "You must grant permission to sign in with Google.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

### 400 Bad Request - Missing Email from Google

**Scenario**: Google account has no verified email (rare, should not happen with `openid email` scope).

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Google account must have a verified email address to sign in.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

### 401 Unauthorized - Invalid State Parameter

**Scenario**: State parameter does not match the correlation cookie (CSRF attack attempt or expired flow).

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid OAuth state. Please restart the sign-in process.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

### 401 Unauthorized - Missing Correlation Cookie

**Scenario**: User cleared cookies or OAuth flow expired (>15 minutes since `/external-login` call).

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "OAuth session expired. Please restart the sign-in process.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

### 403 Forbidden - Inactive Account

**Scenario**: Existing user account is marked as Inactive or Deleted.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "Your account has been deactivated. Contact support for assistance.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

### 500 Internal Server Error - Token Exchange Failure

**Scenario**: Google's token endpoint is unavailable or returned an error.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Failed to authenticate with Google. Please try again later.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

### 500 Internal Server Error - Database Failure

**Scenario**: Failed to create user account or store tokens in database.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Failed to complete sign-in. Please try again.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

## Business Rules

### 1. Email Matching for Account Linking

**Rule**: If Google email matches an existing MoriiCoffee user's email, link Google to that account instead of creating a duplicate.

**Logic**:
```csharp
var existingUser = await _userManager.FindByEmailAsync(googleEmail);
if (existingUser != null)
{
    // Link Google to existing account
    await _userManager.AddLoginAsync(existingUser, new UserLoginInfo("Google", googleUserId, "Google"));
}
else
{
    // Create new account
}
```

---

### 2. Automatic Role Assignment

**Rule**: All new users created via Google OAuth receive the CUSTOMER role automatically.

**Logic**:
```csharp
var newUser = new User { ... };
await _userManager.CreateAsync(newUser);
await _userManager.AddToRoleAsync(newUser, "CUSTOMER");
```

---

### 3. Email Confirmation Bypass

**Rule**: Users who sign in with Google automatically have their email marked as confirmed (EmailConfirmed = true).

**Rationale**: Google already verified the email address during their account creation process.

---

### 4. Refresh Token Replacement

**Rule**: Each successful Google sign-in replaces the previous refresh token for that user + provider combination.

**Logic**:
```csharp
// Remove old token
await _userManager.RemoveAuthenticationTokenAsync(user, "Google", "REFRESH");
// Store new token
await _userManager.SetAuthenticationTokenAsync(user, "Google", "REFRESH", newRefreshToken);
```

---

### 5. Username Generation

**Rule**: Username is generated from the email prefix (before @) with a suffix if conflicts exist.

**Example**:
- Email: `john.doe@gmail.com` → Username: `john.doe`
- If "john.doe" exists → Username: `john.doe1`
- If "john.doe1" exists → Username: `john.doe2`

---

## Security Considerations

### State Validation (CSRF Protection)

1. Callback endpoint MUST validate state parameter matches correlation cookie
2. If state mismatch, reject request with 401 Unauthorized
3. Correlation cookie is deleted after successful validation

### Authorization Code Usage

1. Authorization code is single-use and expires in ~10 seconds
2. Code is exchanged for user profile via Google's token endpoint
3. Code cannot be reused (Google rejects duplicate exchange attempts)

### Token Storage

1. Access tokens are NOT stored in database (short-lived, client-managed)
2. Refresh tokens are stored in AspNetUserTokens with encryption at rest
3. Refresh tokens can be revoked by deleting from database

### Cookie Security

1. `AuthTokenHolder` cookie uses `HttpOnly` flag (prevents XSS attacks)
2. `Secure` flag requires HTTPS (prevents man-in-the-middle attacks)
3. `SameSite=Strict` prevents CSRF attacks
4. 5-minute expiration minimizes exposure window

---

## Testing Scenarios

### Scenario 1: Complete OAuth Flow (New User)

**Step 1**: Call `/external-login?provider=Google&returnUrl=/dashboard`
**Step 2**: Complete Google authentication
**Step 3**: Google redirects to `/external-auth-callback?code=...&state=...`

**Expected**:
- HTTP 302 redirect to `/dashboard`
- `AuthTokenHolder` cookie set with access token and refresh token
- New entry in AspNetUsers table
- New entry in AspNetUserLogins table
- CUSTOMER role assigned in AspNetUserRoles table
- Welcome email sent

**Verification**:
```sql
-- Check user created
SELECT * FROM AspNetUsers WHERE Email = 'testuser@gmail.com';

-- Check Google link
SELECT * FROM AspNetUserLogins WHERE LoginProvider = 'Google' AND ProviderKey = '{google_user_id}';

-- Check role assignment
SELECT * FROM AspNetUserRoles WHERE UserId = '{user_id}';

-- Check refresh token stored
SELECT * FROM AspNetUserTokens WHERE UserId = '{user_id}' AND LoginProvider = 'Google' AND Name = 'REFRESH';
```

---

### Scenario 2: Link Existing Account

**Precondition**: User with email `existing@example.com` already exists in database.

**Step 1**: Sign in with Google using `existing@example.com`
**Step 2**: Complete OAuth flow

**Expected**:
- No new user created
- Google linked to existing account in AspNetUserLogins table
- No welcome email (user already exists)

**Verification**:
```sql
-- Verify only one user with this email
SELECT COUNT(*) FROM AspNetUsers WHERE Email = 'existing@example.com';
-- Should be 1

-- Verify Google is linked
SELECT * FROM AspNetUserLogins WHERE UserId = '{existing_user_id}' AND LoginProvider = 'Google';
```

---

### Scenario 3: User Denies Permission

**Step 1**: Call `/external-login?provider=Google`
**Step 2**: Click "Cancel" on Google consent screen

**Expected**:
- HTTP 302 redirect to `/login?error=access_denied&message=...`
- No user account created
- Clear error message displayed to user

---

### Scenario 4: Invalid State Parameter (CSRF Attack)

**Step 1**: Call `/external-auth-callback` with tampered state parameter

```bash
curl "http://localhost:8002/api/v1/auth/external-auth-callback?code=validcode&state=TAMPERED_STATE"
```

**Expected**:
- HTTP 401 Unauthorized
- Error: "Invalid OAuth state. Please restart the sign-in process."
- No authentication tokens issued

---

### Scenario 5: Expired OAuth Flow

**Step 1**: Call `/external-login?provider=Google`
**Step 2**: Wait 20 minutes (correlation cookie expires after 15 minutes)
**Step 3**: Complete Google authentication

**Expected**:
- HTTP 401 Unauthorized
- Error: "OAuth session expired. Please restart the sign-in process."
- User must restart OAuth flow

---

## Integration Notes

### Frontend Token Extraction

After successful OAuth callback, frontend must extract tokens from the `AuthTokenHolder` cookie:

```javascript
// JavaScript cookie extraction
function getCookie(name) {
  const value = `; ${document.cookie}`;
  const parts = value.split(`; ${name}=`);
  if (parts.length === 2) return parts.pop().split(';').shift();
}

const authData = JSON.parse(decodeURIComponent(getCookie('AuthTokenHolder')));
const accessToken = authData.accessToken;
const refreshToken = authData.refreshToken;

// Store tokens securely (e.g., memory, secure storage)
localStorage.setItem('accessToken', accessToken);
localStorage.setItem('refreshToken', refreshToken);

// Delete the cookie after extraction
document.cookie = 'AuthTokenHolder=; Max-Age=0; path=/;';
```

---

## Related Endpoints

- **POST /api/v1/auth/external-login** - Initiates OAuth flow
- **POST /api/v1/auth/refresh-token** - Refreshes expired access tokens
- **POST /api/v1/auth/sign-out** - Revokes tokens and signs out user

---

## Changelog

| Date | Version | Change |
|------|---------|--------|
| 2026-03-28 | 1.0 | Initial contract definition for Google OAuth callback |
