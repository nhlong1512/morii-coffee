using MoriiCoffee.Application.SeedWork.DTOs.Wishlist;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Wishlist.MergeGuestWishlist;

/// <summary>Merges guest wishlist items into the authenticated user's server-side wishlist.</summary>
public class MergeGuestWishlistCommand : ICommand<WishlistDto>
{
    public Guid UserId { get; set; }
    public List<Guid> GuestProductIds { get; set; } = [];
}
