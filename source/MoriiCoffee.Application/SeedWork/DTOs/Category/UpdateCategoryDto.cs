using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Category;

/// <summary>
/// Multipart/form-data payload for updating an existing product category.
/// When a new Icon file is provided, the old icon is deleted from MinIO and replaced.
/// </summary>
public class UpdateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Optional new icon file. When provided, the existing icon is deleted from MinIO
    /// and replaced with this file. When omitted, the current icon is kept unchanged.
    /// </summary>
    public IFormFile? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}
