namespace MoriiCoffee.Application.SeedWork.DTOs.Wishlist;

/// <summary>Full wishlist response returned by GET /v1/wishlist and POST /v1/wishlist/merge.</summary>
public class WishlistDto
{
    public List<WishlistItemDto> Items { get; set; } = [];
    public DateTime? UpdatedAt { get; set; }
}
