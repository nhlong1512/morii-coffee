namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>Request body for setting the exact quantity of a cart item. Sending 0 removes the item.</summary>
public class UpdateCartItemQuantityDto
{
    /// <summary>Product whose quantity to update.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Variant to update. Must match the variant used when the item was added.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>New quantity. 0 removes the item from the cart.</summary>
    public int Quantity { get; set; }
}
