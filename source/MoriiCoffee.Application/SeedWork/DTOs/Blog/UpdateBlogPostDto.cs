using MoriiCoffee.Domain.Shared.Enums.Blog;
using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Blog;

/// <summary>
/// JSON payload for updating an existing blog post from the admin area.
/// </summary>
public class UpdateBlogPostDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(200)]
    public string? Slug { get; set; }

    [MaxLength(1000)]
    public string? Excerpt { get; set; }

    [Required]
    public string ContentHtml { get; set; } = string.Empty;

    public string? ContentJson { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(500)]
    public string? CoverImageFileName { get; set; }

    public List<Guid> CategoryIds { get; set; } = new();

    [MaxLength(200)]
    public string? SeoTitle { get; set; }

    [MaxLength(500)]
    public string? SeoDescription { get; set; }

    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public EBlogPostStatus Status { get; set; } = EBlogPostStatus.Draft;
}
