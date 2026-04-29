namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>Request body for adding a product to the cart.</summary>
public class AddCartItemDto
{
    /// <summary>Product to add.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Optional variant (e.g., size). Null when the product has no variants.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Number of units to add. Must be at least 1.</summary>
    public int Quantity { get; set; }
}
