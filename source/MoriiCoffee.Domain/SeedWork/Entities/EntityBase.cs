namespace MoriiCoffee.Domain.SeedWork.Entities;

/// <summary>
/// The base class for all domain entities.
/// Provides soft-delete support and automatic date tracking.
/// </summary>
public abstract class EntityBase : IEntityBase
{
    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
