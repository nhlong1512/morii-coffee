using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.AddCartItem;

/// <summary>Adds a product variant to the authenticated user's cart.</summary>
public class AddCartItemCommand : ICommand<CartDto>
{
    /// <summary>Authenticated user's ID derived from the JWT claim.</summary>
    public Guid UserId { get; set; }

    /// <summary>Variant to add.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Number of units (1–99).</summary>
    public int Quantity { get; set; }
}
