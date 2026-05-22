using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Wishlist.AddItemToWishlist;

/// <summary>Adds a product to the user's wishlist. Idempotent — no-op if already present.</summary>
public class AddItemToWishlistCommand : ICommand<bool>
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
}
