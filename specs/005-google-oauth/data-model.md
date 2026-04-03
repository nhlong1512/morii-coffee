# Data Model: Google OAuth External Authentication

**Feature**: 005-google-oauth
**Created**: 2026-03-28

## Overview

This feature does NOT modify the existing data model. The User entity already extends `IdentityUser<Guid>`, which includes built-in support for external logins through ASP.NET Core Identity. This document analyzes the existing ASP.NET Identity tables that will be used for Google OAuth authentication.

---

## ASP.NET Core Identity Tables

### AspNetUsers (Existing - No Changes)

**Purpose**: Stores user accounts. MoriiCoffee's User entity extends `IdentityUser<Guid>`.

**Columns** (relevant to OAuth):
| Column | Type | Description | OAuth Usage |
|--------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | Primary key | Links to AspNetUserLogins |
| `UserName` | NVARCHAR(256) | Unique username | Generated from email for Google users |
| `Email` | NVARCHAR(256) | User email address | **Matching key for linking existing accounts** |
| `PhoneNumber` | NVARCHAR(MAX) | Optional phone | Extracted from Google profile if available |
| `EmailConfirmed` | BIT | Email verification status | Auto-set to true for Google users |
| Custom fields... | Various | MoriiCoffee-specific fields | FullName extracted from Google profile |

**No Migration Required**: User entity already supports all necessary fields.

---

### AspNetUserLogins (Existing - Used by OAuth)

**Purpose**: Links user accounts to external login providers (Google, Facebook, etc.).

**Schema**:
```sql
CREATE TABLE AspNetUserLogins (
    LoginProvider NVARCHAR(128) NOT NULL,     -- "Google"
    ProviderKey NVARCHAR(128) NOT NULL,        -- Unique Google User ID
    ProviderDisplayName NVARCHAR(MAX),         -- "Google"
    UserId UNIQUEIDENTIFIER NOT NULL,          -- Links to AspNetUsers.Id
    PRIMARY KEY (LoginProvider, ProviderKey),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
```

**Example Data** (after Google sign-in):
| LoginProvider | ProviderKey | ProviderDisplayName | UserId |
|---------------|-------------|---------------------|--------|
| Google | 117234567890123456789 | Google | {user-guid} |

**How It Works**:
- When user signs in with Google for the first time, ASP.NET Identity creates entry
- `ProviderKey` is Google's unique identifier for the user (never changes)
- Allows same MoriiCoffee account to link multiple external providers (future: Facebook, Microsoft)
- Prevents duplicate account creation when user signs in with Google multiple times

---

### AspNetUserTokens (Existing - Used for Refresh Tokens)

**Purpose**: Stores authentication tokens associated with users and external providers.

**Schema**:
```sql
CREATE TABLE AspNetUserTokens (
    UserId UNIQUEIDENTIFIER NOT NULL,          -- Links to AspNetUsers.Id
    LoginProvider NVARCHAR(128) NOT NULL,      -- "Google"
    Name NVARCHAR(128) NOT NULL,               -- "REFRESH"
    Value NVARCHAR(MAX),                       -- The actual refresh token
    PRIMARY KEY (UserId, LoginProvider, Name),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
```

**Example Data** (after Google sign-in):
| UserId | LoginProvider | Name | Value |
|--------|---------------|------|-------|
| {user-guid} | Google | REFRESH | a1b2c3d4e5f6... |

**How It Works**:
- After successful Google authentication, MoriiCoffee generates a refresh token
- Token stored with `LoginProvider="Google"` to track authentication method
- When user requests token refresh, system verifies stored token matches
- Each new sign-in replaces the previous refresh token

---

### AspNetRoles (Existing - No Changes)

**Purpose**: Defines available roles in the system.

**Relevant Role**:
| Id | Name | NormalizedName |
|----|------|----------------|
| {role-guid} | CUSTOMER | CUSTOMER |

**OAuth Usage**: All new Google users automatically assigned CUSTOMER role.

---

### AspNetUserRoles (Existing - Used by OAuth)

**Purpose**: Links users to their assigned roles.

**Schema**:
```sql
CREATE TABLE AspNetUserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);
```

**How It Works**:
- After new Google user account creation, CUSTOMER role assigned via `UserManager.AddToRoleAsync`
- Existing users signing in with Google keep their existing roles

---

## OAuth Data Flow

### New User Sign-In with Google

```
1. User authenticates with Google
   ↓
2. Google returns: email="user@gmail.com", name="John Doe", sub="117234567890123456789"
   ↓
3. Check: Does email exist in AspNetUsers?
   No → Create new User
   ↓
4. Insert into AspNetUsers:
   - Id: {new-guid}
   - Email: "user@gmail.com"
   - UserName: "user" (from email prefix)
   - FullName: "John Doe"
   - EmailConfirmed: true
   - Status: Active
   ↓
5. Insert into AspNetUserLogins:
   - LoginProvider: "Google"
   - ProviderKey: "117234567890123456789"
   - UserId: {new-guid}
   ↓
6. Insert into AspNetUserRoles:
   - UserId: {new-guid}
   - RoleId: {CUSTOMER-role-guid}
   ↓
7. Insert into AspNetUserTokens:
   - UserId: {new-guid}
   - LoginProvider: "Google"
   - Name: "REFRESH"
   - Value: "a1b2c3d4..."
```

---

### Existing User Sign-In with Google (First Time)

```
1. User authenticates with Google
   ↓
2. Google returns: email="existing@example.com", sub="117234567890123456789"
   ↓
3. Check: Does email exist in AspNetUsers?
   Yes → Find existing User by email
   ↓
4. Check: Is Google already linked?
   No → Create link
   ↓
5. Insert into AspNetUserLogins:
   - LoginProvider: "Google"
   - ProviderKey: "117234567890123456789"
   - UserId: {existing-user-guid}
   ↓
6. Update AspNetUserTokens:
   - UserId: {existing-user-guid}
   - LoginProvider: "Google"
   - Name: "REFRESH"
   - Value: "a1b2c3d4..."
   ↓
7. No role change (existing user keeps their roles)
```

---

### Returning Google User Sign-In

```
1. User authenticates with Google
   ↓
2. Google returns: sub="117234567890123456789"
   ↓
3. Query AspNetUserLogins:
   - WHERE LoginProvider="Google" AND ProviderKey="117234567890123456789"
   ↓
4. Find linked UserId
   ↓
5. Load User from AspNetUsers
   ↓
6. Update AspNetUserTokens:
   - Replace existing REFRESH token with new one
   ↓
7. Return access token and refresh token
```

---

## Data Uniqueness Constraints

| Table | Unique Constraint | Enforced By | Impact on OAuth |
|-------|-------------------|-------------|-----------------|
| AspNetUsers | Email | ASP.NET Identity | Prevents duplicate accounts when linking by email |
| AspNetUsers | UserName | ASP.NET Identity | Generated from email, guaranteed unique |
| AspNetUserLogins | (LoginProvider, ProviderKey) | Primary Key | Prevents duplicate Google account links |
| AspNetUserLogins | One Google account per MoriiCoffee user | Application logic | User can't link multiple Google accounts |
| AspNetUserRoles | (UserId, RoleId) | Primary Key | User can't have duplicate role assignments |

---

## OAuth Security Considerations

### State Parameter (Not Stored)

**Purpose**: CSRF protection for OAuth flow
**Storage**: Temporary cookie created by ASP.NET Identity middleware
**Lifespan**: Deleted after successful callback
**Validation**: Automatically handled by framework

### Authorization Code (Not Stored)

**Purpose**: One-time token exchanged for user profile
**Storage**: None (used immediately and discarded)
**Lifespan**: Single use, expires in seconds
**Security**: Can only be used once, validated by Google

### Access Token (Not Stored in Database)

**Purpose**: Short-lived JWT for API authentication
**Storage**: Returned to client in cookie, not persisted in database
**Lifespan**: Configured in JwtOptions (typically 8 hours)
**Security**: Signed by MoriiCoffee, cannot be revoked (short expiration mitigates risk)

### Refresh Token (Stored in AspNetUserTokens)

**Purpose**: Long-lived token for obtaining new access tokens
**Storage**: AspNetUserTokens table, encrypted at rest
**Lifespan**: Configured in JwtOptions (typically 7 days)
**Security**: Can be revoked by deleting from database

---

## Migration Requirements

**Migration Needed**: ❌ NO

**Reasons**:
1. ASP.NET Core Identity tables already exist (AspNetUsers, AspNetUserLogins, AspNetUserTokens)
2. User entity already extends IdentityUser<Guid>
3. No schema modifications required
4. No new columns or tables needed

**Rollback**: If OAuth feature is removed, AspNetUserLogins and AspNetUserTokens entries for Google provider are benign and can remain in database.

---

## Query Examples

### Find User by Google Account
```sql
SELECT u.*
FROM AspNetUsers u
JOIN AspNetUserLogins ul ON u.Id = ul.UserId
WHERE ul.LoginProvider = 'Google'
  AND ul.ProviderKey = '117234567890123456789';
```

### Check if Email Already Has Google Linked
```sql
SELECT ul.*
FROM AspNetUserLogins ul
JOIN AspNetUsers u ON ul.UserId = u.Id
WHERE u.Email = 'user@example.com'
  AND ul.LoginProvider = 'Google';
```

### Get Refresh Token for User
```sql
SELECT Value
FROM AspNetUserTokens
WHERE UserId = '{user-guid}'
  AND LoginProvider = 'Google'
  AND Name = 'REFRESH';
```

### List All Google Users
```sql
SELECT u.Email, u.FullName, u.CreatedAt
FROM AspNetUsers u
JOIN AspNetUserLogins ul ON u.Id = ul.UserId
WHERE ul.LoginProvider = 'Google'
ORDER BY u.CreatedAt DESC;
```

---

## Validation Summary

| Requirement | Data Support | Implementation |
|-------------|--------------|----------------|
| Link Google to MoriiCoffee account | ✅ AspNetUserLogins | Framework built-in |
| Store Google User ID | ✅ ProviderKey column | Framework built-in |
| Match by email | ✅ Email column | Application logic |
| Prevent duplicates | ✅ PK constraints | Framework + application |
| Store refresh tokens | ✅ AspNetUserTokens | Framework built-in |
| Track authentication method | ✅ LoginProvider column | Framework built-in |
| Assign roles | ✅ AspNetUserRoles | Framework built-in |

---

## Conclusion

The existing ASP.NET Core Identity data model fully supports Google OAuth authentication without any modifications. All necessary tables (AspNetUserLogins, AspNetUserTokens, AspNetUserRoles) are already in place and designed for external authentication providers. The implementation is purely application logic using framework-provided mechanisms.

**Key Points**:
- Zero database migrations required
- AspNetUserLogins tracks Google account links
- AspNetUserTokens stores refresh tokens
- Email matching enables existing user linking
- Built-in constraints prevent data inconsistencies
- Rollback is trivial (no schema changes to revert)
