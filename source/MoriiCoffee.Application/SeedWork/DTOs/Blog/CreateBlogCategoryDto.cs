using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Blog;

/// <summary>
/// JSON payload for creating a new blog category.
/// </summary>
public class CreateBlogCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(150)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
