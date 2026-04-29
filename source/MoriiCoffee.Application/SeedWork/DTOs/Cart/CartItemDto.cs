namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>
/// Represents a single item stored in the user's Redis cart.
/// All product fields are captured at the time of adding to the cart so
/// the cart remains consistent even if the product is later edited.
/// </summary>
public class CartItemDto
{
    /// <summary>Product identifier.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Optional variant identifier (e.g., a specific size). Null when no variant selected.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Human-readable variant label (e.g., "Size M"). Null when no variant.</summary>
    public string? VariantLabel { get; set; }

    /// <summary>Product display name at the time of adding to cart.</summary>
    public string ProductName { get; set; } = null!;

    /// <summary>Unit price at the time of adding to cart (VND).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Number of units in the cart for this product/variant combination.</summary>
    public int Quantity { get; set; }

    /// <summary>Optional product thumbnail URL for cart display.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>UTC timestamp when this item was first added to the cart.</summary>
    public DateTime AddedAt { get; set; }
}
