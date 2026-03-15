using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Product;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MoriiCoffee.Domain.Aggregates.ProductAggregate;

/// <summary>
/// Represents a product in the coffee shop catalog (e.g., "Caramel Latte").
/// Acts as the aggregate root for the Product bounded context.
/// Each product can have multiple <see cref="ProductVariant"/> (sizes/options)
/// and multiple <see cref="ProductImage"/> entries.
/// </summary>
[Table("Products")]
public class Product : AggregateRoot
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Display name of the product (e.g., "Iced Caramel Macchiato").</summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// URL-friendly identifier for the product (e.g., "iced-caramel-macchiato").
    /// Used for SEO-friendly endpoints and frontend routing.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string Slug { get; set; } = null!;

    /// <summary>Full description of the product shown on the product detail page.</summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Description { get; set; }

    /// <summary>
    /// Base price of the product. Variants may add an additional price on top of this.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    /// <summary>Collection of categories this product belongs to.</summary>
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

    /// <summary>URL of the main thumbnail image for the product.</summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? ThumbnailUrl { get; set; }

    /// <summary>Internal MinIO object name (GUID) for the thumbnail. Used to delete or refresh the URL.</summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? ThumbnailFileName { get; set; }

    /// <summary>Availability and visibility status of the product.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
