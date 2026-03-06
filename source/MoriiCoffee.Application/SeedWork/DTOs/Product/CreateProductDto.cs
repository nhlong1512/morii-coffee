using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Product;

/// <summary>Payload for creating a new product in the catalog.</summary>
public class CreateProductDto
{
    /// <summary>Display name of the product (e.g., "Iced Caramel Macchiato").</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// URL-friendly slug. If not provided, it will be auto-generated from the name.
    /// Must be unique across all products.
    /// </summary>
    [MaxLength(200)]
    public string? Slug { get; set; }

    /// <summary>Full product description shown on the detail page.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Base price before size/option adjustments (must be >= 0).</summary>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    /// <summary>ID of the category this product belongs to.</summary>
    [Required]
    public Guid CategoryId { get; set; }

    /// <summary>URL of the main product thumbnail.</summary>
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>Whether this product should appear in the featured section.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Sort order within its category.</summary>
    public int DisplayOrder { get; set; }
}
