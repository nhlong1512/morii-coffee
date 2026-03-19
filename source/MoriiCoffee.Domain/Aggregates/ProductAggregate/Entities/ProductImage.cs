using MoriiCoffee.Domain.SeedWork.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

/// <summary>
/// Represents a gallery image associated with a product.
/// Products can have multiple images (up to 10) displayed from different angles or settings.
/// The CDN URL is stored as <see cref="Url"/>; the S3 object key is stored in <see cref="S3Key"/>
/// so the file can be deleted from storage when the DB record is removed.
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

    /// <summary>CloudFront CDN URL of the image. Never store the raw S3 URL.</summary>
    [Required]
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string Url { get; set; } = null!;

    /// <summary>
    /// S3 object key (relative path within the container) used for deletion.
    /// Format: <c>{productId}/{timestamp}-{filename}</c>.
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string S3Key { get; set; } = null!;

    /// <summary>Sort order for displaying images in the gallery (lower = first).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this image is the product thumbnail.
    /// Only one image per product may have this flag set at a time.
    /// Changing the thumbnail automatically unsets this flag on the previous thumbnail.
    /// </summary>
    public bool IsThumbnail { get; set; }
}
