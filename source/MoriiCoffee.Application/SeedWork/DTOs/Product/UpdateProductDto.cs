using Microsoft.AspNetCore.Http;
using MoriiCoffee.Domain.Shared.Enums.Product;
using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Product;

/// <summary>
/// Multipart/form-data payload for updating an existing product.
/// When a new Thumbnail file is provided, the old image is deleted from MinIO and replaced.
/// </summary>
public class UpdateProductDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(200)]
    public string? Slug { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    [Required]
    public List<Guid> CategoryIds { get; set; } = new();

    /// <summary>
    /// Optional new thumbnail file. When provided, the existing thumbnail is deleted from MinIO
    /// and replaced with this file. When omitted, the current thumbnail is kept unchanged.
    /// </summary>
    public IFormFile? Thumbnail { get; set; }

    public EProductStatus Status { get; set; }

    public bool IsFeatured { get; set; }

    public int DisplayOrder { get; set; }
}
