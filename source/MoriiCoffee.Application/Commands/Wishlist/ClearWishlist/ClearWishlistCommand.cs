using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Wishlist.ClearWishlist;

/// <summary>Removes all items from the authenticated user's wishlist.</summary>
public class ClearWishlistCommand : ICommand<bool>
{
    public Guid UserId { get; set; }
}
