using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Category;

/// <summary>
/// Multipart/form-data payload for creating a new product category.
/// Icon image is uploaded as a file — the URL is generated server-side by MinIO.
/// </summary>
public class CreateCategoryDto
{
    /// <summary>The display name of the category (e.g., "Espresso Drinks").</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>Optional short description shown to customers.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Optional icon image file. Uploaded to MinIO; the resulting URL is stored on the category.</summary>
    public IFormFile? Icon { get; set; }

    /// <summary>Sort order for displaying this category in the catalog (default: 0).</summary>
    public int DisplayOrder { get; set; }
}
