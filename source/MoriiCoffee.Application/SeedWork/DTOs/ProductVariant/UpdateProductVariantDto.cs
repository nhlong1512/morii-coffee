using System.ComponentModel.DataAnnotations;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;

/// <summary>Payload for updating an existing product variant.</summary>
public class UpdateProductVariantDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public EProductSize Size { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AdditionalPrice { get; set; }

    [MaxLength(50)]
    public string? Sku { get; set; }

    public int StockQuantity { get; set; } = -1;

    public bool IsDefault { get; set; }

    public bool IsAvailable { get; set; }
}
