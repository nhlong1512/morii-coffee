# Quickstart: Testing Email-Only Authentication

**Feature**: 004-email-only-auth
**Created**: 2026-03-28

---

## Overview

This guide provides step-by-step instructions for manually testing the email-only authentication feature. Follow these steps to verify that the implementation meets all acceptance criteria from the specification.

---

## Prerequisites

### 1. Start the Development Environment

```bash
cd /Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee
bash deploy/run-docker-development.sh
```

**Expected Output**:
- Docker containers start (SQL Server, Redis, MinIO)
- API starts on `http://localhost:8002`
- Swagger UI available at `http://localhost:8002/swagger`

### 2. Verify API is Running

```bash
curl http://localhost:8002/api/health
```

**Expected Response**: `200 OK` with health status

---

## Test Setup: Create Test Users

### Option 1: Use Existing Seed Data

The development database may already contain seed users. Check the `ApplicationDbContextSeed` class for default users.

### Option 2: Register New Test Users via API

**User 1: Email-based user (primary test user)**

```bash
curl -X POST http://localhost:8002/api/v1/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test-email@example.com",
    "phoneNumber": "+84123456789",
    "password": "TestPass123!",
    "userName": "testuser"
  }'
```

**Expected Response**: `200 OK` with `accessToken`, `refreshToken`, and user profile

**User 2: Second test user for verification**

```bash
curl -X POST http://localhost:8002/api/v1/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "another-user@example.com",
    "phoneNumber": "+84987654321",
    "password": "AnotherPass123!",
    "userName": "anotheruser"
  }'
```

---

## Test Scenarios

### Test 1: Sign In with Email (Should Succeed) ✅

**Purpose**: Verify that sign-in with a valid email address works correctly

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "test-email@example.com",
    "password": "TestPass123!"
  }'
```

**Expected Response**:
- HTTP Status: `200 OK`
- Body contains:
  - `accessToken` (JWT string)
  - `refreshToken` (opaque string)
  - `user` object with:
    - `email: "test-email@example.com"`
    - `phoneNumber: "+84123456789"` (still present in profile)
    - `id`, `userName`, `fullName`, `avatarUrl`, `status`, `roles`

**Verification**:
- ✅ Authentication succeeds with email
- ✅ Tokens are returned
- ✅ Phone number is still present in user profile (but not used for auth)

---

### Test 2: Sign In with Phone Number (Should Fail) ❌

**Purpose**: Verify that sign-in with a phone number is rejected

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "+84123456789",
    "password": "TestPass123!"
  }'
```

**Expected Response**:
- HTTP Status: `400 Bad Request`
- Body contains:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
      "Identity": ["'Identity' is not a valid email address."]
    }
  }
  ```

**Verification**:
- ✅ Request is rejected with 400 Bad Request
- ✅ Error message indicates invalid email format
- ✅ Phone number is NOT accepted as identity

---

### Test 3: Sign In with Invalid Email Format (Should Fail) ❌

**Purpose**: Verify that malformed email addresses are rejected

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "notanemail",
    "password": "TestPass123!"
  }'
```

**Expected Response**:
- HTTP Status: `400 Bad Request`
- Body contains validation error for `Identity` field

**Additional Invalid Formats to Test**:
- `"identity": "user@"` → 400 Bad Request
- `"identity": "@example.com"` → 400 Bad Request
- `"identity": "user@.com"` → 400 Bad Request
- `"identity": ""` → 400 Bad Request (empty)

**Verification**:
- ✅ All invalid email formats are rejected
- ✅ Error message indicates invalid email address

---

### Test 4: Sign In with Correct Email, Wrong Password (Should Fail) ❌

**Purpose**: Verify that incorrect passwords are rejected

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "test-email@example.com",
    "password": "WrongPassword123!"
  }'
```

**Expected Response**:
- HTTP Status: `401 Unauthorized`
- Body contains:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
    "title": "Unauthorized",
    "status": 401,
    "detail": "Invalid credentials."
  }
  ```

**Verification**:
- ✅ Request is rejected with 401 Unauthorized
- ✅ Generic error message (no distinction between wrong email vs wrong password)

---

### Test 5: Sign In with Non-Existent Email (Should Fail) ❌

**Purpose**: Verify that non-existent emails return the same error as wrong passwords (prevent enumeration)

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "nonexistent@example.com",
    "password": "TestPass123!"
  }'
```

**Expected Response**:
- HTTP Status: `401 Unauthorized`
- Body contains: `"detail": "Invalid credentials."`

**Verification**:
- ✅ Same error as wrong password (prevents email enumeration)

---

### Test 6: Forgot Password Flow (Should Still Work) ✅

**Purpose**: Verify that password reset flow uses email only (unchanged behavior)

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test-email@example.com"
  }'
```

**Expected Response**:
- HTTP Status: `200 OK`
- Body: `{ "success": true }`
- Email sent with password reset link (check logs or email provider)

**Verification**:
- ✅ Forgot password flow still works with email
- ✅ No changes to this endpoint (already email-only)

---

### Test 7: Refresh Token Flow (Should Still Work) ✅

**Purpose**: Verify that refresh token flow doesn't rely on identity lookup

**Step 1: Sign in to get tokens**

```bash
RESPONSE=$(curl -s -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "test-email@example.com",
    "password": "TestPass123!"
  }')

ACCESS_TOKEN=$(echo $RESPONSE | jq -r '.accessToken')
REFRESH_TOKEN=$(echo $RESPONSE | jq -r '.refreshToken')
```

**Step 2: Use refresh token to get new tokens**

```bash
curl -X POST http://localhost:8002/api/v1/auth/refresh-token \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"accessToken\": \"$ACCESS_TOKEN\",
    \"refreshToken\": \"$REFRESH_TOKEN\"
  }"
```

**Expected Response**:
- HTTP Status: `200 OK`
- Body contains new `accessToken` and `refreshToken`

**Verification**:
- ✅ Refresh token flow works correctly
- ✅ No identity lookup required (uses JWT sub claim)

---

### Test 8: User Profile Retrieval (Phone Number Still Present) ✅

**Purpose**: Verify that phone number remains in user profile (not removed)

**Step 1: Sign in to get access token**

```bash
RESPONSE=$(curl -s -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "test-email@example.com",
    "password": "TestPass123!"
  }')

ACCESS_TOKEN=$(echo $RESPONSE | jq -r '.accessToken')
```

**Step 2: Get user profile**

```bash
curl -X GET http://localhost:8002/api/v1/users/me \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

**Expected Response**:
- HTTP Status: `200 OK`
- Body contains:
  ```json
  {
    "id": "...",
    "email": "test-email@example.com",
    "phoneNumber": "+84123456789",
    "fullName": "...",
    "userName": "testuser",
    ...
  }
  ```

**Verification**:
- ✅ Phone number is still present in profile
- ✅ Phone number is not removed from database
- ✅ Phone number can still be viewed/updated (just not used for auth)

---

### Test 9: Case-Insensitive Email Matching ✅

**Purpose**: Verify that email matching is case-insensitive

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "Test-Email@Example.COM",
    "password": "TestPass123!"
  }'
```

**Expected Response**:
- HTTP Status: `200 OK`
- Authentication succeeds (email normalized by Identity)

**Verification**:
- ✅ Email case doesn't matter (normalized by ASP.NET Core Identity)

---

### Test 10: Sign Up Still Works (Email Required) ✅

**Purpose**: Verify that sign-up flow still requires email (unchanged)

**Request**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "new-user@example.com",
    "phoneNumber": "+84111222333",
    "password": "NewUser123!",
    "userName": "newuser"
  }'
```

**Expected Response**:
- HTTP Status: `200 OK`
- User created with email as authentication identity

**Then verify sign-in works with email**:

```bash
curl -X POST http://localhost:8002/api/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "identity": "new-user@example.com",
    "password": "NewUser123!"
  }'
```

**Verification**:
- ✅ Sign-up requires email (already existing behavior)
- ✅ New users can sign in with email

---

## Test Matrix Summary

| Test # | Scenario | Identity Value | Expected Result | Status |
|--------|----------|----------------|-----------------|--------|
| 1 | Valid email + correct password | `test-email@example.com` | 200 OK, tokens returned | ✅ |
| 2 | Phone number + correct password | `+84123456789` | 400 Bad Request, invalid email | ❌ |
| 3 | Invalid email format | `notanemail` | 400 Bad Request, invalid email | ❌ |
| 4 | Valid email + wrong password | `test-email@example.com` | 401 Unauthorized | ❌ |
| 5 | Non-existent email | `nonexistent@example.com` | 401 Unauthorized | ❌ |
| 6 | Forgot password with email | `test-email@example.com` | 200 OK, email sent | ✅ |
| 7 | Refresh token flow | JWT + refresh token | 200 OK, new tokens | ✅ |
| 8 | User profile retrieval | (authenticated) | 200 OK, phone in profile | ✅ |
| 9 | Case-insensitive email | `Test-Email@Example.COM` | 200 OK, normalized | ✅ |
| 10 | Sign up new user | `new-user@example.com` | 200 OK, account created | ✅ |

---

## Using Swagger UI

### 1. Open Swagger UI

Navigate to: `http://localhost:8002/swagger`

### 2. Expand `/api/v1/auth/signin` Endpoint

Click on the `POST /api/v1/auth/signin` endpoint

### 3. Click "Try it out"

### 4. Enter Test Data

**Valid Sign-In**:
```json
{
  "identity": "test-email@example.com",
  "password": "TestPass123!"
}
```

**Invalid Sign-In (Phone Number)**:
```json
{
  "identity": "+84123456789",
  "password": "TestPass123!"
}
```

### 5. Execute and Verify Response

- Check HTTP status code
- Review response body
- Verify error messages match expectations

---

## Verification Checklist

After completing all tests, verify these acceptance criteria from the spec:

### User Story 1: Sign In with Email Only (P1)

- [ ] Sign in with valid email + correct password → 200 OK with tokens
- [ ] Sign in with phone number → 400 Bad Request with error message
- [ ] Sign in with invalid email format → 400 Bad Request with error message

### User Story 2: Sign Up with Email as Primary Identity (P2)

- [ ] New user can register with email
- [ ] New user cannot sign in with their phone number
- [ ] Duplicate email registration fails appropriately

### User Story 3: Maintain Phone Number as Profile Field (P3)

- [ ] User profile includes phone number
- [ ] Phone number can be viewed in profile API
- [ ] Phone number cannot be used for authentication

### Edge Cases

- [ ] Empty identity field → 400 Bad Request
- [ ] Email with special characters/unicode → Handled correctly
- [ ] Case-insensitive email matching works
- [ ] Phone number in E.164 format (+1-234-567-8900) → 400 Bad Request

### Related Flows

- [ ] Forgot password uses email only
- [ ] Refresh token flow works (no identity lookup)
- [ ] All auth endpoints function correctly

---

## Troubleshooting

### Issue: API Not Starting

**Check Docker containers**:
```bash
docker ps
```

**View API logs**:
```bash
docker logs morii-coffee-api
```

### Issue: 401 Unauthorized for Valid Credentials

**Verify user exists**:
- Check if user was created successfully during sign-up
- Verify password is correct
- Check user status is Active (not Inactive or Deleted)

### Issue: 500 Internal Server Error

**Check API logs**:
```bash
docker logs morii-coffee-api
```

**Common causes**:
- Database connection failure
- Migration not applied
- Configuration error in appsettings.json

### Issue: Email Validation Not Working

**Verify changes were deployed**:
```bash
cd source/MoriiCoffee.Presentation
dotnet build --no-incremental
```

**Check validator was updated**:
- Verify `SignInCommandValidator.cs` includes `.EmailAddress()` rule

---

## Clean Up

### Remove Test Users (Optional)

If you want to clean up test data:

```sql
-- Connect to SQL Server
DELETE FROM AspNetUsers WHERE Email LIKE '%@example.com';
```

### Stop Development Environment

```bash
docker-compose down
```

---

## Next Steps

After completing all tests:

1. Document any failures or unexpected behavior
2. Fix issues and re-run affected tests
3. Update this quickstart guide if new scenarios are discovered
4. Proceed to write summary documentation (VN + ENG) per CLAUDE.md workflow
