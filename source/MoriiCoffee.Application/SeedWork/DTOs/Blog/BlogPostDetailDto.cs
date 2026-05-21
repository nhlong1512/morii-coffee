namespace MoriiCoffee.Application.SeedWork.DTOs.Blog;

/// <summary>
/// Full blog post representation used for detail and admin edit responses.
/// </summary>
public class BlogPostDetailDto : BlogPostSummaryDto
{
    public string ContentHtml { get; set; } = string.Empty;
    public string? ContentJson { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? CoverImageFileName { get; set; }
}
