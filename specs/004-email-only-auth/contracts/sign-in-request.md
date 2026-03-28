# API Contract: Sign-In Request

**Endpoint**: `POST /api/v1/auth/signin`
**Feature**: 004-email-only-auth
**Status**: BREAKING CHANGE ⚠️

---

## Request Body

### JSON Schema

```json
{
  "identity": "user@example.com",
  "password": "SecurePass123!"
}
```

### Field Specifications

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `identity` | `string` | Yes | Valid email format (RFC 5322), max 256 chars | **CHANGED**: Must be a valid email address. Phone numbers are no longer accepted. |
| `password` | `string` | Yes | Min 1 char (validated against stored hash) | User's password credential |

---

## Breaking Change Details

### Before (v1.0 - v1.x)

The `identity` field accepted **both email and phone number**:

```json
// Valid requests (old behavior)
{ "identity": "user@example.com", "password": "..." }        // ✅ Email
{ "identity": "+84123456789", "password": "..." }            // ✅ Phone number
{ "identity": "0123456789", "password": "..." }              // ✅ Phone number (no country code)
```

### After (v2.0+)

The `identity` field accepts **email only**:

```json
// Valid request (new behavior)
{ "identity": "user@example.com", "password": "..." }        // ✅ Email

// Invalid requests (new behavior)
{ "identity": "+84123456789", "password": "..." }            // ❌ 400 Bad Request
{ "identity": "0123456789", "password": "..." }              // ❌ 400 Bad Request
```

---

## Validation Rules

### Email Format Validation

**Pattern**: Standard email validation (FluentValidation `.EmailAddress()` rule)

**Valid Examples**:
- `user@example.com`
- `john.doe+tag@company.co.uk`
- `test_user@subdomain.example.org`

**Invalid Examples**:
- `notanemail` → 400 Bad Request
- `user@` → 400 Bad Request
- `@example.com` → 400 Bad Request
- `user@.com` → 400 Bad Request
- `0123456789` → 400 Bad Request (phone number)
- `+84123456789` → 400 Bad Request (phone number)

### Case Sensitivity

Email matching is **case-insensitive** (normalized by ASP.NET Core Identity):
- `User@Example.Com` matches `user@example.com`

---

## Success Response (200 OK)

**Status Code**: `200 OK`

**Response Body**:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4e5f6...",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "johndoe",
    "email": "user@example.com",
    "phoneNumber": "0123456789",
    "fullName": "John Doe",
    "avatarUrl": "https://minio.example.com/avatars/user.jpg",
    "status": 0,
    "roles": ["CUSTOMER"]
  }
}
```

**Note**: The `phoneNumber` field is still returned in the user profile but cannot be used for authentication.

---

## Error Responses

### 400 Bad Request - Invalid Email Format

**Trigger**: `identity` field is not a valid email address

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

**Example Scenarios**:
- `identity: "notanemail"` → Invalid format
- `identity: "0123456789"` → Phone number (not an email)
- `identity: "+84987654321"` → Phone number with country code
- `identity: ""` → Empty string

---

### 400 Bad Request - Missing Required Fields

**Trigger**: `identity` or `password` is null or empty

**Response Body**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Identity": ["'Identity' must not be empty."],
    "Password": ["'Password' must not be empty."]
  }
}
```

---

### 401 Unauthorized - Invalid Credentials

**Trigger**: Email or password is incorrect, OR user account is inactive/deleted

**Response Body**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid credentials."
}
```

**Security Note**: The API does NOT distinguish between:
- Email not found
- Password incorrect
- Account inactive
- Account deleted

This prevents email enumeration attacks.

---

## Migration Guide for API Consumers

### For Client Applications

**Before** (old code):

```javascript
// Clients could use either email or phone
const signIn = async (identity, password) => {
  return fetch('/api/v1/auth/signin', {
    method: 'POST',
    body: JSON.stringify({ identity, password })
  });
};

// Both worked
await signIn('user@example.com', 'password123');      // ✅
await signIn('0123456789', 'password123');            // ✅
```

**After** (updated code):

```javascript
// Clients MUST use email only
const signIn = async (email, password) => {
  return fetch('/api/v1/auth/signin', {
    method: 'POST',
    body: JSON.stringify({
      identity: email,  // Must be email format
      password
    })
  });
};

await signIn('user@example.com', 'password123');      // ✅
await signIn('0123456789', 'password123');            // ❌ 400 Bad Request
```

### Recommended Client Changes

1. **Update UI**: Change input field label from "Email or Phone" to "Email"
2. **Update Validation**: Add client-side email format validation
3. **Update Error Handling**: Handle 400 Bad Request for invalid email format
4. **User Communication**: Notify users that phone-based sign-in is deprecated

---

## Testing Checklist

- [ ] Sign in with valid email + correct password → 200 OK
- [ ] Sign in with valid email + wrong password → 401 Unauthorized
- [ ] Sign in with phone number → 400 Bad Request (invalid email format)
- [ ] Sign in with invalid email format → 400 Bad Request
- [ ] Sign in with empty identity → 400 Bad Request
- [ ] Sign in with empty password → 400 Bad Request
- [ ] Sign in with case-insensitive email → 200 OK (normalized)
- [ ] Sign in with inactive user account → 401 Unauthorized
- [ ] Sign in with deleted user account → 401 Unauthorized

---

## Backward Compatibility

**Breaking Change**: ✅ YES

**Impact**: Clients using phone numbers for sign-in will receive 400 Bad Request errors

**Mitigation**:
- API version should be bumped (if versioning strategy exists)
- Release notes must document this breaking change
- Clients must be notified in advance
- Grace period recommended before enforcement (out of scope for implementation)

---

## Related Endpoints

These endpoints are NOT affected by this change (already email-only or identity-agnostic):

- `POST /api/v1/auth/signup` - Already requires email (no change)
- `POST /api/v1/auth/forgot-password` - Already email-only (no change)
- `POST /api/v1/auth/reset-password` - Already email-only (no change)
- `POST /api/v1/auth/refresh-token` - Uses JWT sub claim, not identity lookup (no change)
