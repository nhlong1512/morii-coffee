using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Domain.Aggregates.ProductAggregate;

/// <summary>
/// Represents a product in the coffee shop catalog (e.g., "Caramel Latte").
/// Acts as the aggregate root for the Product bounded context.
/// Each product can have multiple <see cref="ProductVariant"/> (sizes/options)
/// and multiple <see cref="ProductImage"/> entries.
/// </summary>
public class Product : AggregateRoot
{
    public Guid Id { get; set; }

    /// <summary>Display name of the product (e.g., "Iced Caramel Macchiato").</summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// URL-friendly identifier for the product (e.g., "iced-caramel-macchiato").
    /// Used for SEO-friendly endpoints and frontend routing.
    /// </summary>
    public string Slug { get; set; } = null!;

    /// <summary>Full description of the product shown on the product detail page.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Base price of the product. Variants may add an additional price on top of this.
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>Foreign key to the product's category.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Navigation property to the product category.</summary>
    public Category? Category { get; set; }

    /// <summary>URL of the main thumbnail image for the product.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Availability and visibility status of the product.</summary>
    public EProductStatus Status { get; set; } = EProductStatus.Active;

    /// <summary>Whether this product should be highlighted on the home page or featured section.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Sort order for displaying products within a category.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Collection of size/option variants for this product.</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>Collection of additional gallery images for this product.</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
