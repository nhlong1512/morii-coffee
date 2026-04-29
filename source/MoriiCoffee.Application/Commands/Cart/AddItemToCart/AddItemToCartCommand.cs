using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.AddItemToCart;

/// <summary>Command to add a product (and optional variant) to the user's Redis cart.</summary>
public class AddItemToCartCommand : ICommand<bool>
{
    /// <summary>ID of the authenticated user whose cart will be updated (set from JWT claims).</summary>
    public Guid UserId { get; set; }

    /// <summary>ID of the product to add.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Optional variant ID (e.g., size). Null when no variant is selected.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Number of units to add. Must be at least 1.</summary>
    public int Quantity { get; set; }
}
