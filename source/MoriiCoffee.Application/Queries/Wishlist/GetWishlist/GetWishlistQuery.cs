using MoriiCoffee.Application.SeedWork.DTOs.Wishlist;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Wishlist.GetWishlist;

/// <summary>Retrieves the authenticated user's wishlist with live product snapshots.</summary>
public class GetWishlistQuery : IQuery<WishlistDto>
{
    public Guid UserId { get; set; }
}
