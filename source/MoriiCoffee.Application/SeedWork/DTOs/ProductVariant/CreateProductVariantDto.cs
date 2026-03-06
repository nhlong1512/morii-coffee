using System.ComponentModel.DataAnnotations;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;

/// <summary>Payload for adding a new variant to an existing product.</summary>
public class CreateProductVariantDto
{
    /// <summary>Display name for this variant (e.g., "Small (8 oz)").</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>Standardized size classification for this variant.</summary>
    [Required]
    public EProductSize Size { get; set; }

    /// <summary>Additional price added on top of the product's base price (must be >= 0).</summary>
    [Range(0, double.MaxValue)]
    public decimal AdditionalPrice { get; set; }

    /// <summary>Optional SKU code for inventory management.</summary>
    [MaxLength(50)]
    public string? Sku { get; set; }

    /// <summary>Initial stock quantity. Use -1 for unlimited stock.</summary>
    public int StockQuantity { get; set; } = -1;

    /// <summary>Whether this should be the default selection when viewing the product.</summary>
    public bool IsDefault { get; set; }
}
