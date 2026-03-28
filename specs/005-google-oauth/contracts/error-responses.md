# Error Responses: Google OAuth External Authentication

**Feature**: 005-google-oauth
**Created**: 2026-03-28

## Overview

This document catalogs all error scenarios for the Google OAuth external authentication feature, including HTTP status codes, error messages, and recommended client handling strategies.

---

## Error Response Format

All errors follow RFC 9110 Problem Details standard:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Human-readable error description",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

---

## External Login Endpoint Errors

### E001: Invalid Provider

**HTTP Status**: 400 Bad Request
**Endpoint**: `POST /api/v1/auth/external-login`

**Trigger**: Provider parameter is not "Google" (e.g., "Facebook", "Microsoft").

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Unsupported provider 'Facebook'. Only 'Google' is supported.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message to user
- Show only "Sign in with Google" button (hide other provider options)
- Do not retry with different provider

---

### E002: Missing Provider Parameter

**HTTP Status**: 400 Bad Request
**Endpoint**: `POST /api/v1/auth/external-login`

**Trigger**: `provider` query parameter is missing or empty.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The 'provider' parameter is required.",
  "errors": {
    "provider": ["The provider field is required."]
  },
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- This is a client-side bug (missing required parameter)
- Log error to client error tracking system
- Display generic error message to user

---

### E003: Invalid Return URL

**HTTP Status**: 400 Bad Request
**Endpoint**: `POST /api/v1/auth/external-login`

**Trigger**: `returnUrl` contains invalid characters, open redirect attempt, or malformed URL.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid return URL. Must be a valid relative or absolute URL.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Use relative URLs (e.g., `/dashboard`) instead of absolute URLs
- If absolute URL needed, ensure it matches application domain
- Retry with default returnUrl (`/`)

---

### E004: OAuth Configuration Missing

**HTTP Status**: 500 Internal Server Error
**Endpoint**: `POST /api/v1/auth/external-login`

**Trigger**: Google OAuth credentials (ClientId, ClientSecret) not configured in appsettings.json.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "External authentication is not configured. Contact system administrator.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display "Sign in with Google is temporarily unavailable" message
- Hide Google sign-in button until configuration is fixed
- Suggest alternative sign-in methods (email/password)
- Notify support team

---

## External Callback Endpoint Errors

### E005: User Denied Permission

**HTTP Status**: 400 Bad Request (or HTTP 302 redirect to error page)
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: User clicked "Cancel" or "Deny" on Google's consent screen.

**Response** (if JSON requested):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "You must grant permission to sign in with Google.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Response** (default redirect):
```http
HTTP/1.1 302 Found
Location: /login?error=access_denied&message=You+must+grant+permission+to+sign+in+with+Google
```

**Client Handling**:
- Display friendly message: "Google sign-in cancelled. Please try again to continue."
- Show "Sign in with Google" button again
- Offer alternative sign-in method
- Do not auto-retry (user intentionally cancelled)

---

### E006: Missing Email from Google

**HTTP Status**: 400 Bad Request
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: Google account has no verified email address (extremely rare).

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Google account must have a verified email address to sign in.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message to user
- Suggest: "Please verify your email address in Google account settings and try again"
- Offer alternative sign-in method

---

### E007: Invalid State Parameter (CSRF)

**HTTP Status**: 401 Unauthorized
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: State parameter does not match correlation cookie (CSRF attack or tampered request).

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid OAuth state. Please restart the sign-in process.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message: "Security check failed. Please try signing in again."
- Clear any stored OAuth state
- Redirect user back to sign-in page
- Log security event for monitoring

---

### E008: Missing Correlation Cookie

**HTTP Status**: 401 Unauthorized
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: User cleared cookies, OAuth flow expired (>15 minutes), or cookies disabled.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "OAuth session expired. Please restart the sign-in process.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message: "Sign-in session expired. Please try again."
- Check if cookies are enabled in browser
- If cookies disabled, display: "Cookies must be enabled to sign in with Google"
- Redirect user back to sign-in page

---

### E009: Inactive or Deleted Account

**HTTP Status**: 403 Forbidden
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: Existing user account is marked as `Status = Inactive` or `Status = Deleted`.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "Your account has been deactivated. Contact support for assistance.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message to user
- Provide support contact information (email, phone, chat)
- Do not allow retry (account locked by admin)
- Clear any stored tokens

---

### E010: Invalid Authorization Code

**HTTP Status**: 400 Bad Request
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: Authorization code is expired, already used, or invalid.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid authorization code. Please restart the sign-in process.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message: "Sign-in failed. Please try again."
- Redirect user back to sign-in page
- Auto-retry once if user just completed Google auth
- Log error for monitoring (may indicate timing issue)

---

### E011: Google Token Exchange Failure

**HTTP Status**: 500 Internal Server Error
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: Google's token endpoint is unavailable, rate limited, or returned an error.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Failed to authenticate with Google. Please try again later.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message: "Google sign-in is temporarily unavailable. Please try again in a few minutes."
- Offer alternative sign-in method (email/password)
- Implement exponential backoff for retry (wait 1s → 2s → 5s)
- Monitor for Google API status page updates

---

### E012: Database Error During Account Creation

**HTTP Status**: 500 Internal Server Error
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: Failed to create user account or store tokens in database (DB connection lost, constraint violation, timeout).

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Failed to complete sign-in. Please try again.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message: "Sign-in failed due to a technical issue. Please try again."
- Retry once after 2-second delay
- If retry fails, suggest alternative sign-in method
- Log error for support investigation

---

### E013: Email Service Failure (Non-Blocking)

**HTTP Status**: N/A (No error returned to client)
**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Trigger**: Failed to send welcome email (email service unavailable, invalid recipient).

**Behavior**:
- OAuth callback succeeds (user is authenticated)
- Tokens are issued successfully
- Welcome email failure is logged but does not block sign-in
- Background job may retry email delivery

**Client Handling**:
- No action required (error is transparent to user)
- User successfully signed in despite email failure

---

## Generic Errors

### E014: Network Timeout

**HTTP Status**: 504 Gateway Timeout
**Endpoint**: Any OAuth endpoint

**Trigger**: Request to Google OAuth service or database exceeds timeout threshold.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.5",
  "title": "Gateway Timeout",
  "status": 504,
  "detail": "The request timed out. Please try again.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message: "Request timed out. Please try again."
- Implement client-side timeout (30 seconds for OAuth flow)
- Retry with exponential backoff

---

### E015: Rate Limit Exceeded

**HTTP Status**: 429 Too Many Requests
**Endpoint**: Any OAuth endpoint

**Trigger**: User or IP address exceeded rate limit for OAuth requests.

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.30",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Too many sign-in attempts. Please wait 5 minutes and try again.",
  "traceId": "00-a1b2c3d4e5f6-01-00"
}
```

**Client Handling**:
- Display error message with wait time
- Disable "Sign in with Google" button for 5 minutes
- Show countdown timer if appropriate
- Do not auto-retry (respect rate limit)

---

## Error Handling Best Practices

### Client-Side Error Handling Strategy

```javascript
async function handleGoogleSignIn(returnUrl) {
  try {
    // Initiate OAuth flow
    const response = await fetch(
      `/api/v1/auth/external-login?provider=Google&returnUrl=${encodeURIComponent(returnUrl)}`,
      { method: 'POST', redirect: 'manual' }
    );

    if (response.status === 302) {
      // Success - redirect to Google
      window.location.href = response.headers.get('Location');
    } else if (response.status === 400) {
      // Client error
      const error = await response.json();
      showErrorMessage(error.detail);
    } else if (response.status === 500) {
      // Server error
      showErrorMessage('Google sign-in is temporarily unavailable. Please try again later.');
      // Fall back to email/password sign-in
      showAlternativeSignIn();
    } else if (response.status === 429) {
      // Rate limited
      showErrorMessage('Too many attempts. Please wait a few minutes.');
      disableGoogleSignInButton(300); // 5 minutes
    }
  } catch (error) {
    // Network error
    showErrorMessage('Network error. Please check your connection and try again.');
  }
}
```

### Server-Side Error Logging

All errors should be logged with:
- TraceId for correlation
- User context (if available)
- Error code (E001-E015)
- Timestamp
- Request details (endpoint, parameters)
- Stack trace (for 5xx errors)

```csharp
_logger.LogError(
    "[E007] OAuth state validation failed. TraceId: {TraceId}, State: {State}, IP: {IP}",
    traceId,
    stateParameter,
    httpContext.Connection.RemoteIpAddress
);
```

---

## Error Code Quick Reference

| Code | Description | Status | Endpoint | Retry? |
|------|-------------|--------|----------|--------|
| E001 | Invalid Provider | 400 | external-login | No |
| E002 | Missing Provider | 400 | external-login | No |
| E003 | Invalid Return URL | 400 | external-login | Yes (fix URL) |
| E004 | OAuth Config Missing | 500 | external-login | No |
| E005 | User Denied | 400 | external-auth-callback | Yes (user choice) |
| E006 | Missing Email | 400 | external-auth-callback | No |
| E007 | Invalid State (CSRF) | 401 | external-auth-callback | Yes (restart flow) |
| E008 | Missing Cookie | 401 | external-auth-callback | Yes (restart flow) |
| E009 | Account Inactive | 403 | external-auth-callback | No |
| E010 | Invalid Auth Code | 400 | external-auth-callback | Yes (once) |
| E011 | Google API Error | 500 | external-auth-callback | Yes (backoff) |
| E012 | Database Error | 500 | external-auth-callback | Yes (once) |
| E013 | Email Failure | N/A | external-auth-callback | N/A (non-blocking) |
| E014 | Timeout | 504 | Any | Yes (backoff) |
| E015 | Rate Limited | 429 | Any | No (wait period) |

---

## Monitoring and Alerting

### Critical Errors (Immediate Alert)

- **E004**: OAuth configuration missing (blocks all OAuth)
- **E011**: Google API error rate >5% (indicates Google outage)
- **E012**: Database error rate >10% (indicates DB issue)

### Warning Errors (Daily Report)

- **E007**: CSRF failures (potential security issue)
- **E009**: Account inactive (user experience issue)
- **E013**: Email failures (delayed user notification)

### Informational Errors (Weekly Report)

- **E005**: User denials (conversion metric)
- **E006**: Missing emails (rare edge case)
- **E010**: Invalid auth codes (timing issue)

---

## Changelog

| Date | Version | Change |
|------|---------|--------|
| 2026-03-28 | 1.0 | Initial error catalog for Google OAuth feature |
