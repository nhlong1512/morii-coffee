using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>Request body for POST /api/v1/cart/items.</summary>
public class AddCartItemDto
{
    [SwaggerSchema("ID of the product variant to add to the cart.")]
    public Guid VariantId { get; set; }

    [SwaggerSchema("Number of units to add (1–99).")]
    public int Quantity { get; set; }
}
