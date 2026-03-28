# Data Model: Email-Only Authentication

**Feature**: 004-email-only-auth
**Created**: 2026-03-28

## Overview

This feature does NOT modify the data model. The User entity remains unchanged. This document analyzes the existing User entity to confirm that email and phone number fields support the new authentication behavior.

---

## User Entity Analysis

**Location**: `source/MoriiCoffee.Domain/Aggregates/UserAggregate/User.cs`

**Base Class**: `IdentityUser<Guid>` (ASP.NET Core Identity)

### Identity-Managed Fields (Inherited)

These fields come from `IdentityUser<Guid>` and are managed by ASP.NET Core Identity:

| Field | Type | Constraints | Purpose | Auth Role After Change |
|-------|------|-------------|---------|----------------------|
| `Id` | `Guid` | Primary Key | Unique user identifier | Used in JWT "sub" claim |
| `UserName` | `string` | Unique, Max 256 | Display name / login fallback | Not used for sign-in |
| `Email` | `string` | Unique, Max 256 | Email address | **PRIMARY AUTH IDENTITY** |
| `PhoneNumber` | `string` | Max 256, Nullable | Phone number | **PROFILE FIELD ONLY** (no auth role) |
| `PasswordHash` | `string` | Hashed | Password credential | Used for password verification |
| `SecurityStamp` | `string` | GUID | Security token version | Token invalidation |
| `EmailConfirmed` | `bool` | - | Email verification status | Not currently enforced |
| `PhoneNumberConfirmed` | `bool` | - | Phone verification status | Not used |
| `TwoFactorEnabled` | `bool` | - | 2FA flag | Not currently implemented |
| `LockoutEnd` | `DateTimeOffset?` | - | Account lockout expiry | Identity lockout feature |
| `AccessFailedCount` | `int` | - | Failed login attempts | Identity lockout feature |

### Custom Domain Fields

| Field | Type | Constraints | Purpose |
|-------|------|-------------|---------|
| `FullName` | `string` | Max 200 | User's full name |
| `Dob` | `DateTime?` | Nullable | Date of birth |
| `Gender` | `EGender` | Enum (Male=0, Female=1, Other=2) | Gender |
| `Bio` | `string` | Max 1000 | User biography |
| `AvatarUrl` | `string` | Max 500 | MinIO object URL |
| `AvatarFileName` | `string` | Max 500 | MinIO object key |
| `Status` | `EUserStatus` | Enum (Active=0, Inactive=1) | Account status |
| `IsDeleted` | `bool` | - | Soft delete flag |
| `CreatedAt` | `DateTime` | UTC | Creation timestamp |
| `UpdatedAt` | `DateTime?` | UTC, Nullable | Last update timestamp |
| `DeletedAt` | `DateTime?` | UTC, Nullable | Deletion timestamp |

---

## Authentication Identity Fields: Before vs After

### Before This Change

**Sign-In Identity Resolution**:
```csharp
var user = _userManager.Users.FirstOrDefault(u =>
    u.Email == request.Identity || u.PhoneNumber == request.Identity);
```

- **Email**: Can be used for sign-in ✅
- **PhoneNumber**: Can be used for sign-in ✅
- Both fields indexed and queried during authentication

### After This Change

**Sign-In Identity Resolution**:
```csharp
var user = _userManager.Users.FirstOrDefault(u =>
    u.Email == request.Identity);
```

- **Email**: Can be used for sign-in ✅ (ONLY auth identity)
- **PhoneNumber**: CANNOT be used for sign-in ❌ (profile field only)
- Only Email field queried during authentication

---

## Data Validation Rules

### Email Field

**Current Validation**:
- Required: Yes (enforced by Identity and SignUpCommandValidator)
- Format: Valid email address (RFC 5322)
- Uniqueness: Yes (enforced by ASP.NET Core Identity UserStore)
- Max Length: 256 characters
- Nullable: No

**Change Impact**: None (already enforced)

### PhoneNumber Field

**Current Validation**:
- Required: Yes during sign-up (enforced by SignUpCommandValidator)
- Format: Regex `^\+?[0-9]{7,15}$` (7-15 digits, optional +)
- Uniqueness: Yes (enforced by SignUpCommandHandler check)
- Max Length: 256 characters
- Nullable: Yes (Identity default, but required at sign-up)

**Change Impact**: None (remains a required profile field, just not for auth)

---

## Uniqueness Constraints

| Field | Unique? | Enforced By | Index | Impact |
|-------|---------|-------------|-------|--------|
| `Email` | Yes | ASP.NET Core Identity | Yes | No change (already primary identity) |
| `PhoneNumber` | Yes | Application logic in SignUpCommandHandler | No | No change (remains unique for profile purposes) |
| `UserName` | Yes | ASP.NET Core Identity | Yes | No change (not used for sign-in) |

**Note**: Phone number uniqueness is enforced in application code, not database constraint. This is acceptable since phone is now profile-only.

---

## Entity Relationships

**User** has no direct relationships defined in the domain model. It acts as an aggregate root that other aggregates may reference via `UserId` foreign keys (e.g., Orders, Reviews).

**Impact of Change**: None. User identity change does not affect referential integrity.

---

## State Transitions

User entity supports these state changes:

1. **Creation**: `IsDeleted = false`, `Status = Active`, `CreatedAt = now`
2. **Activation**: `Status = Active` (via `Activate()` method)
3. **Deactivation**: `Status = Inactive` (via `Deactivate()` method)
4. **Soft Delete**: `IsDeleted = true`, `DeletedAt = now`

**Authentication Rules**:
- Inactive users: Cannot sign in (checked in SignInCommandHandler)
- Deleted users: Cannot sign in (checked in SignInCommandHandler)

**Impact of Change**: None (status checks remain unchanged)

---

## Database Schema

**Table**: `AspNetUsers` (managed by ASP.NET Core Identity)

**Columns**:
- All `IdentityUser<Guid>` fields (Id, Email, PhoneNumber, PasswordHash, etc.)
- Custom domain fields (FullName, Dob, Gender, Bio, AvatarUrl, etc.)

**Indexes** (Identity default):
- `IX_AspNetUsers_Email` (unique)
- `IX_AspNetUsers_UserName` (unique)
- `IX_AspNetUsers_NormalizedEmail` (unique, used for case-insensitive lookup)
- `IX_AspNetUsers_NormalizedUserName` (unique)

**Impact of Change**:
- No migration required
- No schema modifications
- Phone number column remains in table
- Email index already exists and supports new behavior

---

## Data Migration Requirements

**Migration Needed**: ❌ NO

**Reasons**:
1. No schema changes (email and phone fields unchanged)
2. No data transformations needed
3. No new constraints required
4. Existing indexes support email-only authentication

**Rollback**: Trivial (revert application code, no database rollback needed)

---

## Validation Summary

| Requirement | Field | Current State | Change Needed |
|-------------|-------|---------------|---------------|
| Email must be unique | `Email` | ✅ Enforced | None |
| Email must be valid format | `Email` | ✅ Validated | None |
| Phone cannot be used for auth | `PhoneNumber` | ❌ Currently used | Remove from lookup query |
| Phone remains in profile | `PhoneNumber` | ✅ Present | None |
| Phone must be unique | `PhoneNumber` | ✅ Enforced | None |

---

## Conclusion

The existing User entity data model fully supports email-only authentication without modification. The change is purely behavioral (application logic) and does not require schema updates, migrations, or data transformations.

**Key Points**:
- Email already unique, indexed, and validated
- Phone number remains in database as profile field
- No breaking changes to data structure
- Zero database downtime for this change
- Rollback is application-only (no data migration rollback)
