namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>
/// Represents one selected product variant inside the cart snapshot.
/// Prices and display names are snapshotted at add-to-cart time.
/// </summary>
public class CartItemDto
{
    /// <summary>Parent product identifier.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Selected variant identifier.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Snapshotted product display name.</summary>
    public string ProductName { get; set; } = null!;

    /// <summary>Snapshotted variant option name (e.g. "Large").</summary>
    public string VariantName { get; set; } = null!;

    /// <summary>Snapshotted product thumbnail URL; null when no image is available.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Snapshotted unit price at add-to-cart time.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Customer-selected quantity; must be greater than zero.</summary>
    public int Quantity { get; set; }

    /// <summary>UnitPrice × Quantity.</summary>
    public decimal LineTotal { get; set; }
}
