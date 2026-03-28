# API Contract: External Login Request

**Feature**: 005-google-oauth
**Endpoint**: `POST /api/v1/auth/external-login`
**Created**: 2026-03-28

## Overview

Initiates the OAuth 2.0 authorization code flow with Google. This endpoint prepares the OAuth challenge and redirects the user to Google's authentication page.

---

## Request

### HTTP Method
`POST`

### URL
```
POST /api/v1/auth/external-login
```

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `provider` | string | Yes | - | OAuth provider name. Must be "Google" (case-insensitive). |
| `returnUrl` | string | No | "/" | URL to redirect after successful authentication. Must be a valid relative or absolute URL. |

### Request Headers

No special headers required. Standard CORS headers apply for cross-origin requests.

### Request Body

None (uses query parameters only).

### Example Request

```http
POST /api/v1/auth/external-login?provider=Google&returnUrl=http://localhost:3000/dashboard HTTP/1.1
Host: localhost:8002
Content-Length: 0
```

---

## Response

### Success Response (HTTP 302 Found)

The endpoint does not return JSON. Instead, it issues an HTTP 302 redirect to Google's OAuth authorization page.

**Redirect URL Format**:
```
https://accounts.google.com/o/oauth2/v2/auth?
  client_id={GOOGLE_CLIENT_ID}
  &redirect_uri={CALLBACK_URL}
  &response_type=code
  &scope=openid%20profile%20email
  &state={CSRF_STATE_TOKEN}
```

**Response Headers**:
```
HTTP/1.1 302 Found
Location: https://accounts.google.com/o/oauth2/v2/auth?client_id=...
Set-Cookie: .AspNetCore.Correlation.Google.{correlation_id}={state_value}; path=/; secure; httponly; samesite=lax
```

**State Cookie**:
- Name: `.AspNetCore.Correlation.Google.{correlation_id}`
- Purpose: CSRF protection - validates callback matches the initiated request
- Lifespan: 15 minutes (deleted after successful callback)
- Flags: `Secure`, `HttpOnly`, `SameSite=Lax`

---

## Error Responses

### 400 Bad Request - Invalid Provider

**Scenario**: Provider parameter is missing or not "Google"

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Unsupported provider 'Facebook'. Only 'Google' is supported.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

### 400 Bad Request - Invalid Return URL

**Scenario**: returnUrl contains invalid characters or open redirect attempt

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid return URL. Must be a valid relative or absolute URL.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

### 500 Internal Server Error - OAuth Configuration Missing

**Scenario**: Google OAuth credentials not configured in appsettings.json

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "External authentication is not configured. Contact system administrator.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

## Business Rules

1. **Provider Restriction**: Only "Google" is accepted. Other providers (Facebook, Microsoft) are rejected with 400 error.
2. **Return URL Validation**: The `returnUrl` parameter is validated to prevent open redirect attacks. Only URLs matching the application's domain are allowed.
3. **State Parameter Generation**: ASP.NET Core Identity automatically generates a cryptographically secure state parameter for CSRF protection.
4. **Cookie-Based State Storage**: The state value is stored in a temporary cookie that must be present during the callback phase.
5. **Session Independence**: This endpoint does not require an authenticated session. Anonymous users can initiate OAuth flow.

---

## Security Considerations

### CSRF Protection
- State parameter is generated using cryptographically secure random number generator
- State cookie is `HttpOnly` and `Secure` to prevent client-side tampering
- Callback endpoint validates state matches the cookie value before processing

### Open Redirect Prevention
- `returnUrl` parameter is validated against whitelist or domain restriction
- Absolute URLs must match application domain
- Relative URLs are allowed (e.g., `/dashboard`, `/products`)

### HTTPS Requirement
- Production environment MUST use HTTPS for OAuth flow
- Google requires HTTPS redirect URIs in production OAuth credentials
- Development environment can use HTTP for localhost testing

---

## Testing Scenarios

### Scenario 1: Successful OAuth Initiation
```bash
curl -X POST "http://localhost:8002/api/v1/auth/external-login?provider=Google&returnUrl=/dashboard" \
  -v \
  -L  # Follow redirects to see Google OAuth page
```

**Expected**: HTTP 302 redirect to Google authentication page with state cookie set.

### Scenario 2: Invalid Provider
```bash
curl -X POST "http://localhost:8002/api/v1/auth/external-login?provider=Facebook" \
  -H "Content-Type: application/json"
```

**Expected**: HTTP 400 with error message "Unsupported provider 'Facebook'".

### Scenario 3: Missing Provider Parameter
```bash
curl -X POST "http://localhost:8002/api/v1/auth/external-login" \
  -H "Content-Type: application/json"
```

**Expected**: HTTP 400 with validation error.

### Scenario 4: Default Return URL
```bash
curl -X POST "http://localhost:8002/api/v1/auth/external-login?provider=Google" \
  -v \
  -L
```

**Expected**: HTTP 302 redirect to Google, `returnUrl` defaults to "/" (home page).

---

## Integration Notes

### Frontend Integration

**Step 1**: Redirect user to external-login endpoint when "Sign in with Google" button is clicked.

```javascript
// React example
const handleGoogleSignIn = () => {
  const returnUrl = encodeURIComponent(window.location.pathname);
  window.location.href = `${API_BASE_URL}/api/v1/auth/external-login?provider=Google&returnUrl=${returnUrl}`;
};
```

**Step 2**: User is automatically redirected to Google authentication page.

**Step 3**: After Google authentication, user is redirected to `external-auth-callback` endpoint (handled by backend).

**Step 4**: Backend processes callback and redirects to `returnUrl` with token cookie.

### Mobile App Integration

Mobile apps should use in-app browser (WebView) or system browser with custom URL scheme for OAuth flow. Deep linking configuration required to handle callback.

---

## Related Endpoints

- **POST /api/v1/auth/external-auth-callback** - Processes OAuth callback from Google
- **POST /api/v1/auth/refresh-token** - Refreshes expired access tokens
- **POST /api/v1/auth/sign-in** - Standard email/password sign-in (alternative method)

---

## Changelog

| Date | Version | Change |
|------|---------|--------|
| 2026-03-28 | 1.0 | Initial contract definition for Google OAuth feature |
