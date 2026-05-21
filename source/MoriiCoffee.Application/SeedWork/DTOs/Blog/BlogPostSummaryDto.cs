using MoriiCoffee.Domain.Shared.Enums.Blog;

namespace MoriiCoffee.Application.SeedWork.DTOs.Blog;

/// <summary>
/// Lightweight blog post representation used for list and grid responses.
/// </summary>
public class BlogPostSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }
    public EBlogPostStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BlogCategoryDto> Categories { get; set; } = new();
}
