using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Category;

/// <summary>Payload for updating an existing product category.</summary>
public class UpdateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? IconUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}
