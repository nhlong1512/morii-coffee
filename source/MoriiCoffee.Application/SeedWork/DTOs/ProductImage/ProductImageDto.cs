namespace MoriiCoffee.Application.SeedWork.DTOs.ProductImage;

/// <summary>Represents a single gallery image returned to clients.</summary>
public class ProductImageDto
{
    /// <summary>Unique identifier of the image record.</summary>
    public Guid Id { get; set; }

    /// <summary>CloudFront CDN URL of the image.</summary>
    public string Url { get; set; } = null!;

    /// <summary>Sort order within the product's image gallery (lower = first).</summary>
    public int DisplayOrder { get; set; }
}
