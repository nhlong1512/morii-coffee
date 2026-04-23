using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.RemoveCartItem;

/// <summary>Removes a product variant line from the authenticated user's cart.</summary>
public class RemoveCartItemCommand : ICommand<CartDto>
{
    /// <summary>Authenticated user's ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Variant to remove from the cart.</summary>
    public Guid VariantId { get; set; }
}
