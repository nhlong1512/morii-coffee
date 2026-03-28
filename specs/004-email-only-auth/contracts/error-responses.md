# API Contract: Error Responses for Email-Only Authentication

**Feature**: 004-email-only-auth
**Created**: 2026-03-28

---

## Overview

This document defines the error response format for authentication failures related to the email-only identity restriction. All error responses follow RFC 7807 (Problem Details for HTTP APIs) format, consistent with ASP.NET Core conventions.

---

## Error Response Format

### Standard Structure

```json
{
  "type": "string (URI reference)",
  "title": "string (short, human-readable summary)",
  "status": "integer (HTTP status code)",
  "detail": "string (optional, detailed explanation)",
  "errors": {
    "FieldName": ["array of validation error messages"]
  }
}
```

---

## 400 Bad Request Errors

### Error 1: Invalid Email Format

**Scenario**: The `identity` field does not match email format

**HTTP Status**: `400 Bad Request`

**Response Body**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Identity": [
      "'Identity' is not a valid email address."
    ]
  }
}
```

**Trigger Examples**:
- `identity: "notanemail"`
- `identity: "user@"`
- `identity: "@example.com"`
- `identity: "0123456789"` (phone number)
- `identity: "+84987654321"` (international phone)

**Client Action**: Display validation error, prompt user to enter a valid email address

---

### Error 2: Empty Identity Field

**Scenario**: The `identity` field is null, empty string, or whitespace

**HTTP Status**: `400 Bad Request`

**Response Body**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Identity": [
      "'Identity' must not be empty."
    ]
  }
}
```

**Client Action**: Display validation error, prompt user to enter email address

---

### Error 3: Empty Password Field

**Scenario**: The `password` field is null, empty string, or whitespace

**HTTP Status**: `400 Bad Request`

**Response Body**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Password": [
      "'Password' must not be empty."
    ]
  }
}
```

**Client Action**: Display validation error, prompt user to enter password

---

### Error 4: Multiple Validation Errors

**Scenario**: Multiple fields are invalid simultaneously

**HTTP Status**: `400 Bad Request`

**Response Body**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Identity": [
      "'Identity' must not be empty."
    ],
    "Password": [
      "'Password' must not be empty."
    ]
  }
}
```

**Client Action**: Display all validation errors, prompt user to correct all fields

---

## 401 Unauthorized Errors

### Error 5: Invalid Credentials (Generic)

**Scenario**: Any of the following:
- Email address not found in database
- Password does not match stored hash
- User account is inactive (`Status = Inactive`)
- User account is soft-deleted (`IsDeleted = true`)

**HTTP Status**: `401 Unauthorized`

**Response Body**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid credentials."
}
```

**Security Note**: The API intentionally returns the same generic error for all authentication failures to prevent:
- Email enumeration (attacker cannot determine if email exists)
- Account status disclosure (attacker cannot determine if account is locked/deleted)
- Password hint leakage

**Client Action**:
- Display generic error: "Invalid email or password"
- Do NOT distinguish between email not found vs password incorrect
- Suggest password reset flow if user forgot password

---

## Error Handling Best Practices for Clients

### 1. Email Format Validation (Client-Side)

Validate email format on the client before sending the request to reduce unnecessary API calls:

```javascript
const isValidEmail = (email) => {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
};

const signIn = async (identity, password) => {
  if (!isValidEmail(identity)) {
    throw new Error('Please enter a valid email address');
  }
  // Proceed with API call
};
```

### 2. Graceful Degradation for Phone-Based Sign-In

If your client previously supported phone-based sign-in, handle the breaking change gracefully:

```javascript
const signIn = async (identity, password) => {
  try {
    const response = await fetch('/api/v1/auth/signin', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ identity, password })
    });

    if (response.status === 400) {
      const error = await response.json();
      if (error.errors?.Identity) {
        // Check if user entered a phone number
        if (/^[\+\d][\d\s\-\(\)]+$/.test(identity)) {
          return {
            success: false,
            message: 'Phone number sign-in is no longer supported. Please use your email address.'
          };
        }
      }
    }

    // Handle other errors...
  } catch (err) {
    // Handle network errors...
  }
};
```

### 3. Error Message Mapping

Map API errors to user-friendly messages:

```javascript
const getErrorMessage = (error) => {
  if (error.status === 400) {
    if (error.errors?.Identity?.includes('not a valid email address')) {
      return 'Please enter a valid email address';
    }
    if (error.errors?.Identity?.includes('must not be empty')) {
      return 'Email address is required';
    }
    if (error.errors?.Password?.includes('must not be empty')) {
      return 'Password is required';
    }
  }

  if (error.status === 401) {
    return 'Invalid email or password. Please try again.';
  }

  return 'An unexpected error occurred. Please try again later.';
};
```

---

## Testing Error Scenarios

### Manual Testing Checklist

- [ ] **Empty identity**: `identity: ""` → 400 with Identity error
- [ ] **Empty password**: `password: ""` → 400 with Password error
- [ ] **Both empty**: `identity: "", password: ""` → 400 with both errors
- [ ] **Invalid email format**: `identity: "notanemail"` → 400 with email validation error
- [ ] **Phone number**: `identity: "0123456789"` → 400 with email validation error
- [ ] **International phone**: `identity: "+84987654321"` → 400 with email validation error
- [ ] **Valid email, wrong password**: → 401 with generic error
- [ ] **Non-existent email**: → 401 with generic error (same as wrong password)
- [ ] **Inactive account**: → 401 with generic error
- [ ] **Deleted account**: → 401 with generic error
- [ ] **Case-insensitive email**: `User@Example.COM` → Should work (normalized)

### Automated Test Examples (Pseudocode)

```csharp
[Fact]
public async Task SignIn_WithPhoneNumber_Returns400BadRequest()
{
    var request = new SignInCommand
    {
        Identity = "0123456789",
        Password = "ValidPass123!"
    };

    var result = await _client.PostAsJsonAsync("/api/v1/auth/signin", request);

    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    var error = await result.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Contains("not a valid email address", error.Errors["Identity"][0]);
}

[Fact]
public async Task SignIn_WithInvalidEmail_Returns400BadRequest()
{
    var request = new SignInCommand
    {
        Identity = "notanemail",
        Password = "ValidPass123!"
    };

    var result = await _client.PostAsJsonAsync("/api/v1/auth/signin", request);

    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
}

[Fact]
public async Task SignIn_WithNonExistentEmail_Returns401Unauthorized()
{
    var request = new SignInCommand
    {
        Identity = "nonexistent@example.com",
        Password = "ValidPass123!"
    };

    var result = await _client.PostAsJsonAsync("/api/v1/auth/signin", request);

    Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    var error = await result.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Equal("Invalid credentials.", error.Detail);
}
```

---

## Error Code Summary Table

| HTTP Status | Error Scenario | Error Message | Client Action |
|-------------|----------------|---------------|---------------|
| 400 | Invalid email format | "'Identity' is not a valid email address." | Prompt for valid email |
| 400 | Empty identity | "'Identity' must not be empty." | Prompt for email |
| 400 | Empty password | "'Password' must not be empty." | Prompt for password |
| 401 | Wrong email | "Invalid credentials." | Generic error message |
| 401 | Wrong password | "Invalid credentials." | Generic error message |
| 401 | Inactive account | "Invalid credentials." | Generic error message |
| 401 | Deleted account | "Invalid credentials." | Generic error message |

---

## Security Considerations

### 1. Email Enumeration Prevention

The API does NOT reveal whether an email exists in the system:
- Non-existent email → 401 "Invalid credentials"
- Existing email with wrong password → 401 "Invalid credentials"

**Same generic error for both scenarios** prevents attackers from harvesting valid email addresses.

### 2. Account Status Disclosure Prevention

The API does NOT reveal account status:
- Active account with wrong password → 401 "Invalid credentials"
- Inactive account → 401 "Invalid credentials"
- Deleted account → 401 "Invalid credentials"

**Same generic error for all scenarios** prevents attackers from determining account status.

### 3. Timing Attack Mitigation

Password hashing (ASP.NET Core Identity's `CheckPasswordAsync`) uses constant-time comparison to prevent timing attacks that could distinguish between valid and invalid emails based on response time.

### 4. Rate Limiting Recommendation

Although out of scope for this feature, consider implementing rate limiting to prevent brute-force attacks:
- Limit failed sign-in attempts per IP address
- Implement exponential backoff after failed attempts
- Lock accounts after N failed attempts (Identity's lockout feature)

---

## Related Documentation

- [sign-in-request.md](sign-in-request.md) - Sign-in request contract
- [Authentication API Structure](../../docs/api/auth-api-structure.md) - Full auth API documentation
