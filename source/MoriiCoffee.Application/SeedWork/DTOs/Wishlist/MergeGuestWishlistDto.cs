namespace MoriiCoffee.Application.SeedWork.DTOs.Wishlist;

/// <summary>Request body for POST /v1/wishlist/merge — items saved while unauthenticated.</summary>
public class MergeGuestWishlistDto
{
    public List<GuestWishlistItemDto> GuestItems { get; set; } = [];
}

/// <summary>A single guest wishlist item (only productId — no snapshot needed for merge).</summary>
public class GuestWishlistItemDto
{
    public Guid ProductId { get; set; }
}
