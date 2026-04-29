using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.UpdateCartItemQuantity;

/// <summary>
/// Command to set the exact quantity of a cart item.
/// A quantity of 0 removes the item from the cart entirely.
/// </summary>
public class UpdateCartItemQuantityCommand : ICommand<bool>
{
    /// <summary>ID of the authenticated user whose cart will be updated (set from JWT claims).</summary>
    public Guid UserId { get; set; }

    /// <summary>ID of the product whose quantity should be updated.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Optional variant ID that identifies the specific line item to update.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>New quantity to set. A value of 0 removes the item.</summary>
    public int Quantity { get; set; }
}
