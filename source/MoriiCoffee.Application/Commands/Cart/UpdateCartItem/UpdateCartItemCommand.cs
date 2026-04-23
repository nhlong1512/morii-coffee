using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.UpdateCartItem;

/// <summary>Updates the quantity for an existing cart line. Quantity 0 removes the line.</summary>
public class UpdateCartItemCommand : ICommand<CartDto>
{
    /// <summary>Authenticated user's ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Variant whose quantity should be updated.</summary>
    public Guid VariantId { get; set; }

    /// <summary>New quantity. Pass 0 to remove the line.</summary>
    public int Quantity { get; set; }
}
