using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.SeedWork.DomainEvent;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.Shared.Enums.User;

namespace MoriiCoffee.Domain.Aggregates.UserAggregate;

/// <summary>
/// Represents a MoriiCoffee user. Extends IdentityUser to inherit Identity-managed fields
/// (password hash, security stamp, lockout, etc.) and adds domain-specific fields.
/// Implements IAggregateRoot directly to avoid Id property conflict with EntityBase.
/// </summary>
[Table("Users")]
public class User : IdentityUser<Guid>, IAggregateRoot, IEntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>Full display name of the user (e.g., "Nguyen Van A").</summary>
    [MaxLength(200)]
    public string? FullName { get; set; }

    /// <summary>Date of birth. Optional — used for personalization and age verification.</summary>
    public DateTime? Dob { get; set; }

    /// <summary>User's self-identified gender. Stored as int in the database.</summary>
    public EGender? Gender { get; set; }

    /// <summary>Short user bio or description displayed on the profile page.</summary>
    [MaxLength(1000)]
    public string? Bio { get; set; }

    /// <summary>Full public URL of the user's avatar image served from MinIO.</summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>Internal MinIO object key for the avatar file (used to delete the old image on update).</summary>
    [MaxLength(500)]
    public string? AvatarFileName { get; set; }

    /// <summary>Current account status. Inactive users are blocked from signing in.</summary>
    public EUserStatus Status { get; set; } = EUserStatus.Active;

    /// <summary>Soft-delete flag. True when the user has been deleted but the record is retained.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the account was created. Set automatically by DateTrackingInterceptor.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last update. Set automatically by DateTrackingInterceptor.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>UTC timestamp when the account was soft-deleted. Null if not deleted.</summary>
    public DateTime? DeletedAt { get; set; }

    #region IAggregateRoot

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();
    public void ClearDomainEvents() => _domainEvents.Clear();
    public void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    #endregion

    #region Domain Methods

    /// <summary>Updates the user's display profile fields.</summary>
    public void UpdateProfile(string? fullName, DateTime? dob, EGender? gender, string? bio)
    {
        FullName = fullName;
        Dob = dob;
        Gender = gender;
        Bio = bio;
    }

    /// <summary>Sets the user's avatar URL and internal file name after a successful upload.</summary>
    public void SetAvatar(string? url, string? fileName)
    {
        AvatarUrl = url;
        AvatarFileName = fileName;
    }

    /// <summary>Sets the account status to Active, allowing the user to sign in.</summary>
    public void Activate()
    {
        Status = EUserStatus.Active;
    }

    /// <summary>Sets the account status to Inactive, blocking the user from signing in.</summary>
    public void Deactivate()
    {
        Status = EUserStatus.Inactive;
    }

    #endregion
}
