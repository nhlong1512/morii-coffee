using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;

/// <summary>Represents a product variant (size/option) as returned to clients.</summary>
public class ProductVariantDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = null!;
    public EProductSize Size { get; set; }
    public decimal AdditionalPrice { get; set; }

    /// <summary>
    /// Computed total price = Product.BasePrice + AdditionalPrice.
    /// Populated by the query handler.
    /// </summary>
    public decimal TotalPrice { get; set; }
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
