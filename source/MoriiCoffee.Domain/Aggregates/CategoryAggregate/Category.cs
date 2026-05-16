using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.CategoryAggregate;

/// <summary>
/// Represents a product category (e.g., Coffee, Tea, Food, Cold Brew).
/// Acts as the aggregate root for the Category bounded context.
/// </summary>
[Table("Categories")]
public class Category : AggregateRoot
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Display name of the category (e.g., "Espresso Drinks").</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>Short description shown to customers.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>URL of the icon or thumbnail image for this category.</summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>Internal MinIO object name (GUID) for the icon. Used to delete or refresh the URL.</summary>
    [MaxLength(500)]
    public string? IconFileName { get; set; }

    /// <summary>Sort order for displaying categories in the catalog.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether this category is currently visible in the catalog.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Collection of products in this category.</summary>
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}
