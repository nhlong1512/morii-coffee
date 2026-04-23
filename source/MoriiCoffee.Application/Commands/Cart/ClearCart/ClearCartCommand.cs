using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.ClearCart;

/// <summary>Clears all items from the authenticated user's cart.</summary>
public class ClearCartCommand : ICommand<bool>
{
    /// <summary>Authenticated user's ID.</summary>
    public Guid UserId { get; set; }
}
