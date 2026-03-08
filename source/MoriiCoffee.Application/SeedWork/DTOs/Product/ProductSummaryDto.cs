using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.SeedWork.DTOs.Product;

/// <summary>
/// Lightweight product representation used in list/paginated responses.
/// Does not include variants or images to keep payload small.
/// </summary>
public class ProductSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public List<string> CategoryNames { get; set; } = new();
    public string? ThumbnailUrl { get; set; }
    public EProductStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
