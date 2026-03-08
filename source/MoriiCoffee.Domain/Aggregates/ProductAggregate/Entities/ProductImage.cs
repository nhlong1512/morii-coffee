using MoriiCoffee.Domain.SeedWork.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

/// <summary>
/// Represents an additional gallery image associated with a product.
/// Products can have multiple images for displaying from different angles or in different settings.
/// </summary>
[Table("ProductImages")]
public class ProductImage : EntityBase
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Foreign key to the parent product.</summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the parent product.</summary>
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    /// <summary>Publicly accessible URL of the image.</summary>
    [Required]
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string ImageUrl { get; set; } = null!;

    /// <summary>Alt text for accessibility and SEO.</summary>
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string? AltText { get; set; }

    /// <summary>Sort order for displaying images in the gallery.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether this is the primary product image (same as ThumbnailUrl on Product).</summary>
    public bool IsMain { get; set; }
}
