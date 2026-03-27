# Data Model: Email Integration and Social Login

**Feature**: Email Integration and Social Login Planning
**Date**: 2026-03-23
**Status**: Design

This document specifies the data model changes required for email integration and OAuth2 social login functionality.

---

## Overview

The email integration feature requires NO new domain entities (email messages are ephemeral and logged via Serilog). The social login feature requires extending the existing `User` aggregate from Phase 2 with fields to track external authentication providers (Google, Meta).

---

## Entity: User (Extended for Social Login)

### Current User Entity (Phase 2 Baseline)

```csharp
public class User : IdentityUser<Guid>, IAggregateRoot
{
    // Identity fields (inherited from IdentityUser<Guid>)
    // - Id: Guid
    // - Email: string
    // - UserName: string
    // - PasswordHash: string
    // - SecurityStamp: string
    // - EmailConfirmed: bool
    // - PhoneNumber: string?
    // - PhoneNumberConfirmed: bool
    // - TwoFactorEnabled: bool
    // - LockoutEnd: DateTimeOffset?
    // - LockoutEnabled: bool
    // - AccessFailedCount: int

    // Domain-specific fields (Phase 2)
    public string? FullName { get; private set; }
    public DateTime? Dob { get; private set; }
    public EGender? Gender { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? AvatarFileName { get; private set; }
    public EUserStatus Status { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
}
```

### New Fields (Social Login Extension)

| Field | Type | Nullable | Default | Description | Validation |
|-------|------|----------|---------|-------------|------------|
| `ExternalProvider` | `EExternalProvider` (enum) | No | `None` | OAuth2 provider used for account creation/login (None, Google, Meta) | Stored as int in database |
| `ExternalProviderId` | `string` | Yes | `null` | Provider-specific user ID (e.g., Google `sub` claim, Meta user ID) | Max 500 characters; unique per provider |
| `ExternalEmail` | `string` | Yes | `null` | Email returned by OAuth2 provider (for audit/debugging) | Max 320 characters |
| `ExternalEmailVerified` | `bool` | No | `false` | Whether OAuth2 provider verified the email | Required for auto-linking |

### Domain Methods (New)

```csharp
/// <summary>
/// Links an external OAuth2 provider to this user account.
/// Only allowed if email is verified by the provider.
/// </summary>
public void LinkExternalProvider(
    EExternalProvider provider,
    string providerId,
    string email,
    bool emailVerified)
{
    if (!emailVerified)
        throw new DomainException("Cannot link external provider with unverified email");

    if (provider == EExternalProvider.None)
        throw new ArgumentException("Provider cannot be None", nameof(provider));

    if (string.IsNullOrWhiteSpace(providerId))
        throw new ArgumentException("Provider ID is required", nameof(providerId));

    ExternalProvider = provider;
    ExternalProviderId = providerId;
    ExternalEmail = email;
    ExternalEmailVerified = emailVerified;
    UpdatedAt = DateTime.UtcNow;

    // Raise domain event (optional, for audit trail)
    AddDomainEvent(new UserExternalProviderLinkedDomainEvent(Id, provider));
}

/// <summary>
/// Unlinks the external OAuth2 provider, reverting to local account.
/// User must have a password set to unlink (safety check).
/// </summary>
public void UnlinkExternalProvider()
{
    if (string.IsNullOrEmpty(PasswordHash))
        throw new DomainException("Cannot unlink external provider without setting a password first");

    ExternalProvider = EExternalProvider.None;
    ExternalProviderId = null;
    ExternalEmail = null;
    ExternalEmailVerified = false;
    UpdatedAt = DateTime.UtcNow;

    // Raise domain event (optional)
    AddDomainEvent(new UserExternalProviderUnlinkedDomainEvent(Id));
}
```

### Updated User Constructor (Social Login Path)

```csharp
/// <summary>
/// Creates a new user via OAuth2 social login (no password).
/// </summary>
public static User CreateFromExternalProvider(
    string email,
    string userName,
    EExternalProvider provider,
    string providerId,
    bool emailVerified,
    string? fullName = null)
{
    if (!emailVerified)
        throw new DomainException("Cannot create user from external provider with unverified email");

    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = email,
        UserName = userName,
        EmailConfirmed = emailVerified,  // Mark email as confirmed since provider verified it
        FullName = fullName,
        ExternalProvider = provider,
        ExternalProviderId = providerId,
        ExternalEmail = email,
        ExternalEmailVerified = emailVerified,
        Status = EUserStatus.Active,
        IsDeleted = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    user.AddDomainEvent(new UserCreatedDomainEvent(user.Id));
    return user;
}
```

### Validation Rules

| Rule | Validation | Error Message |
|------|------------|---------------|
| **Unique Provider ID** | No two users can have the same `(ExternalProvider, ExternalProviderId)` pair | "This social account is already linked to another user" |
| **Email Verified Required** | `ExternalEmailVerified` must be `true` to link external provider | "Cannot link external provider with unverified email" |
| **Provider ID Format** | Max 500 characters; alphanumeric + hyphens/underscores | "Invalid provider ID format" |
| **No Duplicate Provider** | Cannot link the same provider twice to one account | "External provider already linked" |
| **Password Required for Unlink** | User must have `PasswordHash` set to unlink external provider | "Cannot unlink external provider without setting a password first" |

### State Transitions

```
Local Account (No External Provider)
├─→ Linked Account (ExternalProvider set after social login)
│   └─→ Local Account (Unlink external provider, revert to password-only)
│
└─→ Social-Only Account (Created via OAuth2, no password)
    └─→ Linked Account (User sets password in profile settings)
```

**Key Invariants**:
1. A user can have at most ONE external provider linked at a time (future enhancement: support multiple)
2. A user MUST have either a password OR an external provider (cannot have neither)
3. A user cannot unlink external provider without setting a password first (prevents account lockout)

---

## Enum: EExternalProvider

```csharp
/// <summary>
/// Represents the external OAuth2 authentication provider used for social login.
/// </summary>
public enum EExternalProvider
{
    /// <summary>
    /// No external provider linked (local account with email/password).
    /// </summary>
    None = 0,

    /// <summary>
    /// Google OAuth2 provider.
    /// </summary>
    Google = 1,

    /// <summary>
    /// Meta (Facebook) OAuth2 provider.
    /// </summary>
    Meta = 2

    // Future: Apple = 3, Microsoft = 4, GitHub = 5, etc.
}
```

**Storage**: Stored as `int` in database for forward compatibility (easy to add new providers)

**Location**: `source/MoriiCoffee.Domain.Shared/Enums/User/EExternalProvider.cs`

---

## Repository Changes

### IUsersRepository (New Methods)

```csharp
public interface IUsersRepository : IRepository<User>
{
    // Existing methods from Phase 2
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<Pagination<UserSummaryDto>> GetPaginatedAsync(/* filters */);

    // NEW methods for social login
    /// <summary>
    /// Finds a user by external provider and provider-specific user ID.
    /// </summary>
    Task<User?> GetByExternalProviderAsync(EExternalProvider provider, string providerId);

    /// <summary>
    /// Checks if an external provider ID is already linked to any user.
    /// </summary>
    Task<bool> IsExternalProviderLinkedAsync(EExternalProvider provider, string providerId);
}
```

**Implementation** (UsersRepository.cs):
```csharp
public async Task<User?> GetByExternalProviderAsync(EExternalProvider provider, string providerId)
{
    return await _context.Users
        .FirstOrDefaultAsync(u =>
            u.ExternalProvider == provider &&
            u.ExternalProviderId == providerId &&
            !u.IsDeleted);
}

public async Task<bool> IsExternalProviderLinkedAsync(EExternalProvider provider, string providerId)
{
    return await _context.Users
        .AnyAsync(u =>
            u.ExternalProvider == provider &&
            u.ExternalProviderId == providerId &&
            !u.IsDeleted);
}
```

---

## Database Schema Changes (Migration)

### Migration: `AddSocialLoginFields`

**File**: `source/MoriiCoffee.Infrastructure.Persistence/Migrations/YYYYMMDD_AddSocialLoginFields.cs`

```sql
-- Add columns to AspNetUsers table
ALTER TABLE AspNetUsers ADD ExternalProvider INT NOT NULL DEFAULT 0;
ALTER TABLE AspNetUsers ADD ExternalProviderId NVARCHAR(500) NULL;
ALTER TABLE AspNetUsers ADD ExternalEmail NVARCHAR(320) NULL;
ALTER TABLE AspNetUsers ADD ExternalEmailVerified BIT NOT NULL DEFAULT 0;

-- Add unique constraint: prevent duplicate provider IDs
CREATE UNIQUE INDEX IX_Users_ExternalProvider_ExternalProviderId
ON AspNetUsers (ExternalProvider, ExternalProviderId)
WHERE ExternalProvider != 0 AND ExternalProviderId IS NOT NULL;

-- Add index for lookups by external provider
CREATE INDEX IX_Users_ExternalProvider
ON AspNetUsers (ExternalProvider)
WHERE ExternalProvider != 0;
```

**EF Core Configuration** (UserConfiguration.cs):
```csharp
public void Configure(EntityTypeBuilder<User> builder)
{
    // Existing configuration...

    // Social login fields
    builder.Property(u => u.ExternalProvider)
        .HasConversion<int>()
        .IsRequired()
        .HasDefaultValue(EExternalProvider.None);

    builder.Property(u => u.ExternalProviderId)
        .HasMaxLength(500);

    builder.Property(u => u.ExternalEmail)
        .HasMaxLength(320);

    builder.Property(u => u.ExternalEmailVerified)
        .IsRequired()
        .HasDefaultValue(false);

    // Unique index: prevent duplicate provider IDs
    builder.HasIndex(u => new { u.ExternalProvider, u.ExternalProviderId })
        .HasDatabaseName("IX_Users_ExternalProvider_ExternalProviderId")
        .IsUnique()
        .HasFilter("ExternalProvider != 0 AND ExternalProviderId IS NOT NULL");
}
```

---

## Email Message (Not Persisted)

**Note**: Email messages are ephemeral and not stored in the database. They are created, sent via SendGrid, and logged via Serilog for observability.

### Conceptual Email Message Structure

```csharp
// NOT a domain entity - used internally by EmailService
internal class EmailMessage
{
    public string To { get; set; }              // Recipient email address
    public string? ToName { get; set; }         // Recipient name (optional)
    public string Subject { get; set; }         // Email subject line
    public string HtmlBody { get; set; }        // HTML content
    public string PlainTextBody { get; set; }   // Plain-text fallback
    public string FromEmail { get; set; }       // Sender email (no-reply@moriicoffee.com)
    public string FromName { get; set; }        // Sender name (Morii Coffee)
    public DateTime SentAt { get; set; }        // Timestamp of send attempt
    public bool Success { get; set; }           // Whether send succeeded
    public string? ErrorMessage { get; set; }   // Error details if failed
}
```

**Logging** (via Serilog structured properties):
```csharp
_logger.LogInformation(
    "Email sent: {Template} to {Recipient} at {Timestamp} with status {Status}",
    "WelcomeEmail",
    recipient,
    DateTime.UtcNow,
    success ? "Success" : "Failed");

if (!success)
{
    _logger.LogError(
        "Email send failed: {Template} to {Recipient}. Error: {Error}",
        "WelcomeEmail",
        recipient,
        errorMessage);
}
```

---

## Domain Events (New)

### UserExternalProviderLinkedDomainEvent

```csharp
/// <summary>
/// Raised when a user links an external OAuth2 provider to their account.
/// </summary>
public class UserExternalProviderLinkedDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public EExternalProvider Provider { get; }
    public DateTime OccurredOn { get; }

    public UserExternalProviderLinkedDomainEvent(Guid userId, EExternalProvider provider)
    {
        UserId = userId;
        Provider = provider;
        OccurredOn = DateTime.UtcNow;
    }
}
```

**Use Case**: Send notification email to user when external provider is linked (security awareness)

### UserExternalProviderUnlinkedDomainEvent

```csharp
/// <summary>
/// Raised when a user unlinks an external OAuth2 provider from their account.
/// </summary>
public class UserExternalProviderUnlinkedDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public DateTime OccurredOn { get; }

    public UserExternalProviderUnlinkedDomainEvent(Guid userId)
    {
        UserId = userId;
        OccurredOn = DateTime.UtcNow;
    }
}
```

**Use Case**: Audit trail for account security changes

---

## DTO Updates (Application Layer)

### UserDto (Extended)

```csharp
public class UserDto
{
    // Existing fields from Phase 2
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public string? FullName { get; set; }
    public DateTime? Dob { get; set; }
    public string? Gender { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; }

    // NEW fields for social login
    public string ExternalProvider { get; set; }  // "None", "Google", "Meta"
    public bool HasPassword { get; set; }         // Whether user has set a password
}
```

**Mapping** (UserMapper.cs):
```csharp
CreateMap<User, UserDto>()
    .ForMember(d => d.ExternalProvider, opt => opt.MapFrom(s => s.ExternalProvider.ToString()))
    .ForMember(d => d.HasPassword, opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.PasswordHash)));
```

---

## Summary

### Data Model Changes

| Entity | Change Type | Impact |
|--------|-------------|--------|
| `User` | Extended | Add 4 fields: `ExternalProvider`, `ExternalProviderId`, `ExternalEmail`, `ExternalEmailVerified` |
| `User` | Methods | Add `LinkExternalProvider()`, `UnlinkExternalProvider()`, `CreateFromExternalProvider()` |
| `IUsersRepository` | Extended | Add `GetByExternalProviderAsync()`, `IsExternalProviderLinkedAsync()` |
| `EExternalProvider` | New Enum | Define provider types: `None`, `Google`, `Meta` |
| Domain Events | New | Add `UserExternalProviderLinkedDomainEvent`, `UserExternalProviderUnlinkedDomainEvent` |

### Database Migration

- **New Columns**: 4 columns added to `AspNetUsers` table
- **Indexes**: 2 indexes added (unique constraint on provider + provider ID, lookup index on provider)
- **Backward Compatibility**: Default value `0` (None) for `ExternalProvider` ensures existing users unaffected

### No New Tables Required

Email messages are ephemeral (not persisted). Social login data is stored in existing `AspNetUsers` table via User entity extensions.

---

## Next Steps

1. Create `EExternalProvider.cs` enum in Domain.Shared
2. Extend `User.cs` entity with new fields and domain methods
3. Update `IUsersRepository.cs` with new methods
4. Implement new repository methods in `UsersRepository.cs`
5. Update `UserConfiguration.cs` with EF Core mappings
6. Generate migration: `dotnet ef migrations add AddSocialLoginFields`
7. Update `UserDto.cs` and `UserMapper.cs` for API responses
8. Create domain event classes: `UserExternalProviderLinkedDomainEvent`, `UserExternalProviderUnlinkedDomainEvent`
