using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>Request body for PUT /api/v1/cart/items/{variantId}.</summary>
public class UpdateCartItemDto
{
    [SwaggerSchema("New quantity for the cart line. Pass 0 to remove the item from the cart.")]
    public int Quantity { get; set; }
}
