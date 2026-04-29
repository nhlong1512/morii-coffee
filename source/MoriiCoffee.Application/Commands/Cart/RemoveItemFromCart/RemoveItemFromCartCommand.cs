using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.RemoveItemFromCart;

/// <summary>Command to remove a specific product/variant combination from the cart.</summary>
public class RemoveItemFromCartCommand : ICommand<bool>
{
    /// <summary>ID of the authenticated user whose cart will be updated (set from JWT claims).</summary>
    public Guid UserId { get; set; }

    /// <summary>ID of the product to remove.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Optional variant ID that identifies the specific line item to remove.</summary>
    public Guid? VariantId { get; set; }
}
