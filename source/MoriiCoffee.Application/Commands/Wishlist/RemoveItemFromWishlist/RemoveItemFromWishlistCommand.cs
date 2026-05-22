using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Wishlist.RemoveItemFromWishlist;

/// <summary>Removes a product from the user's wishlist. Returns 404 if not found.</summary>
public class RemoveItemFromWishlistCommand : ICommand<bool>
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
}
