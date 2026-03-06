namespace MoriiCoffee.Domain.SeedWork.Entities;

/// <summary>Marks an entity that supports soft deletion (logical delete).</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
