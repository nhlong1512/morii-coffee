using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Blog;

namespace MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPost;

/// <summary>
/// Command to fully update an existing blog post.
/// </summary>
public class UpdateBlogPostCommand : ICommand<BlogPostDetailDto>
{
    public UpdateBlogPostCommand(Guid id, UpdateBlogPostDto dto)
    {
        Id = id;
        Title = dto.Title;
        Slug = dto.Slug;
        Excerpt = dto.Excerpt;
        ContentHtml = dto.ContentHtml;
        ContentJson = dto.ContentJson;
        CoverImageUrl = dto.CoverImageUrl;
        CoverImageFileName = dto.CoverImageFileName;
        CategoryIds = dto.CategoryIds;
        SeoTitle = dto.SeoTitle;
        SeoDescription = dto.SeoDescription;
        IsFeatured = dto.IsFeatured;
        DisplayOrder = dto.DisplayOrder;
        Status = dto.Status;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string? Slug { get; }
    public string? Excerpt { get; }
    public string ContentHtml { get; }
    public string? ContentJson { get; }
    public string? CoverImageUrl { get; }
    public string? CoverImageFileName { get; }
    public List<Guid> CategoryIds { get; }
    public string? SeoTitle { get; }
    public string? SeoDescription { get; }
    public bool IsFeatured { get; }
    public int DisplayOrder { get; }
    public EBlogPostStatus Status { get; }
}
