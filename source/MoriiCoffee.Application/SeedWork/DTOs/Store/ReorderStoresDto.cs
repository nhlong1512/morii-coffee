namespace MoriiCoffee.Application.SeedWork.DTOs.Store;

/// <summary>Payload for bulk-updating the display order of multiple stores in a single request.</summary>
public class ReorderStoresDto
{
    /// <summary>List of store ID / display order pairs to update.</summary>
    public List<ReorderStoreItem> Items { get; set; } = new();
}

/// <summary>A single store reorder item: a store ID paired with its new display order.</summary>
public class ReorderStoreItem
{
    /// <summary>The ID of the store to reorder.</summary>
    public Guid Id { get; set; }

    /// <summary>The new display order value for this store.</summary>
    public int DisplayOrder { get; set; }
}
