namespace MoriiCoffee.Domain.SeedWork.Entities;

/// <summary>Marks an entity that tracks creation and modification timestamps.</summary>
public interface IDateTracking
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
