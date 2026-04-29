namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>Request body identifying which cart item to remove.</summary>
public class RemoveCartItemDto
{
    /// <summary>Product to remove.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Variant to remove. Must match the variant used when the item was added.</summary>
    public Guid? VariantId { get; set; }
}
