namespace MoriiCoffee.Application.SeedWork.DTOs.Store;

/// <summary>Payload for toggling a store's active/inactive status without a full update.</summary>
public class UpdateStoreStatusDto
{
    /// <summary>The desired active status. True = visible on public page; false = hidden.</summary>
    public bool IsActive { get; set; }
}
