using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;

/// <summary>Represents an application role. Extends IdentityRole and adds audit tracking fields.</summary>
public class Role : IdentityRole<Guid>, IEntityBase
{
    public Role(string name) : base(name)
    {
    }

    /// <summary>Optional description of the role's purpose.</summary>
    public string? Description { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the role was created. Set automatically by DateTrackingInterceptor.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last update. Set automatically by DateTrackingInterceptor.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>UTC timestamp when the role was soft-deleted. Null if not deleted.</summary>
    public DateTime? DeletedAt { get; set; }
}
