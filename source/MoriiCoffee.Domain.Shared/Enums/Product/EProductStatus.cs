namespace MoriiCoffee.Domain.Shared.Enums.Product;

/// <summary>
/// Represents the availability status of a product in the catalog.
/// </summary>
public enum EProductStatus
{
    /// <summary>The product is active and available for ordering.</summary>
    Active = 0,

    /// <summary>The product is temporarily unavailable or hidden from the catalog.</summary>
    Inactive = 1,

    /// <summary>The product is visible but currently out of stock.</summary>
    OutOfStock = 2
}
