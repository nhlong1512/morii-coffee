using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.SeedWork.DTOs.Product;

/// <summary>Full product representation returned to clients, including variants and images.</summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public string? ThumbnailUrl { get; set; }
    public EProductStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
}
