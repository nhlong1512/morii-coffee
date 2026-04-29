using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.ClearCart;

/// <summary>Command to remove all items from the user's cart.</summary>
public class ClearCartCommand : ICommand<bool>
{
    /// <summary>ID of the authenticated user whose cart will be cleared (set from JWT claims).</summary>
    public Guid UserId { get; set; }
}
