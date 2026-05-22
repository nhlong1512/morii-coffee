namespace MoriiCoffee.Application.SeedWork.DTOs.Wishlist;

/// <summary>
/// Product snapshot returned for each wishlist item.
/// Includes live product data joined at query time — no stale snapshots.
/// </summary>
public class WishlistItemDto
{
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductSlug { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public string? ThumbnailUrl { get; set; }

    /// <summary>True when product.Status == EProductStatus.Active.</summary>
    public bool InStock { get; set; }

    public DateTime AddedAt { get; set; }
}
