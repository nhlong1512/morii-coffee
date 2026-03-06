using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Category;

/// <summary>Payload for creating a new product category.</summary>
public class CreateCategoryDto
{
    /// <summary>The display name of the category (e.g., "Espresso Drinks").</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>Optional short description shown to customers.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Optional URL to an icon or thumbnail image for this category.</summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>Sort order for displaying this category in the catalog (default: 0).</summary>
    public int DisplayOrder { get; set; }
}
