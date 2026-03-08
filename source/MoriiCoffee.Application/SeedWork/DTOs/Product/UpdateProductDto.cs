using System.ComponentModel.DataAnnotations;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.SeedWork.DTOs.Product;

/// <summary>Payload for updating an existing product.</summary>
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

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public EProductStatus Status { get; set; }

    public bool IsFeatured { get; set; }

    public int DisplayOrder { get; set; }
}
