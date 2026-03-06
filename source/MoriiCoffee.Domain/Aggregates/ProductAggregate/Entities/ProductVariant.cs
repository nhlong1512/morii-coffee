using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

/// <summary>
/// Represents a size or customization variant of a product (e.g., Small / Medium / Large).
/// A product variant defines an additional price on top of the product's base price
/// and can track its own stock quantity.
/// </summary>
public class ProductVariant : EntityBase
{
    public Guid Id { get; set; }

    /// <summary>Foreign key to the parent product.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the parent product.</summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Display name for this variant (e.g., "Small (8 oz)", "Large (16 oz)").
    /// This name is shown to the customer when selecting size.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>Standardized size classification for this variant.</summary>
    public EProductSize Size { get; set; }

    /// <summary>
    /// Price added on top of the product's base price.
    /// Total price = Product.BasePrice + AdditionalPrice.
    /// </summary>
    public decimal AdditionalPrice { get; set; }

    /// <summary>
    /// Optional stock-keeping unit code for inventory management.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>Current stock quantity. A value of -1 indicates unlimited stock.</summary>
    public int StockQuantity { get; set; } = -1;

    /// <summary>Whether this variant is the default selection when a customer views the product.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether this specific variant is currently available for ordering.</summary>
    public bool IsAvailable { get; set; } = true;
}
