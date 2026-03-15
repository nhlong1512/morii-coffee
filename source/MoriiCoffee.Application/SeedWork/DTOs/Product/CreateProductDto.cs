using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Product;

/// <summary>
/// Multipart/form-data payload for creating a new product.
/// Thumbnail image is uploaded as a file — the URL is generated server-side by MinIO.
/// </summary>
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

    /// <summary>IDs of the categories this product belongs to (comma-separated or repeated field in form-data).</summary>
    [Required]
    public List<Guid> CategoryIds { get; set; } = new();

    /// <summary>Optional thumbnail image file. Uploaded to MinIO; the resulting URL is stored on the product.</summary>
    public IFormFile? Thumbnail { get; set; }

    /// <summary>Whether this product should appear in the featured section.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Sort order within its category.</summary>
    public int DisplayOrder { get; set; }
}
