using MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.BlogPostAggregate;

/// <summary>
/// Represents a single editorial blog post managed by the internal CMS.
/// Stores both canonical editor JSON and a rendered HTML snapshot for storefront display.
/// </summary>
[Table("BlogPosts")]
public class BlogPost : AggregateRoot
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Human-readable post title.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>Unique public slug for storefront routes.</summary>
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = null!;

    /// <summary>Optional summary shown in cards and list views.</summary>
    [MaxLength(1000)]
    public string? Excerpt { get; set; }

    /// <summary>Canonical editor content persisted as structured JSON.</summary>
    [Column(TypeName = "text")]
    public string? ContentJson { get; set; }

    /// <summary>Rendered HTML snapshot used by the storefront.</summary>
    [Column(TypeName = "text")]
    public string ContentHtml { get; set; } = string.Empty;

    /// <summary>Stored S3/CDN key or full URL for the cover image.</summary>
    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    /// <summary>Internal storage object name for replacement or cleanup flows.</summary>
    [MaxLength(500)]
    public string? CoverImageFileName { get; set; }

    /// <summary>Optional SEO title override.</summary>
    [MaxLength(200)]
    public string? SeoTitle { get; set; }

    /// <summary>Optional SEO description override.</summary>
    [MaxLength(500)]
    public string? SeoDescription { get; set; }

    /// <summary>Current publication state of the post.</summary>
    public EBlogPostStatus Status { get; set; } = EBlogPostStatus.Draft;

    /// <summary>Whether the post is included in featured storefront sections.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Explicit ordering for curated storefront sections and admin lists.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>UTC timestamp of the first time the post became published.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Category assignments for this post.</summary>
    public ICollection<BlogPostCategory> BlogPostCategories { get; set; } = new List<BlogPostCategory>();

    /// <summary>Creates a new blog post using normalized inputs.</summary>
    public static BlogPost Create(
        string title,
        string slug,
        string? excerpt,
        string? contentJson,
        string contentHtml,
        string? coverImageUrl,
        string? coverImageFileName,
        string? seoTitle,
        string? seoDescription,
        bool isFeatured,
        int displayOrder,
        EBlogPostStatus status)
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Excerpt = string.IsNullOrWhiteSpace(excerpt) ? null : excerpt.Trim(),
            ContentJson = string.IsNullOrWhiteSpace(contentJson) ? null : contentJson,
            ContentHtml = contentHtml,
            CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim(),
            CoverImageFileName = string.IsNullOrWhiteSpace(coverImageFileName) ? null : coverImageFileName.Trim(),
            SeoTitle = string.IsNullOrWhiteSpace(seoTitle) ? null : seoTitle.Trim(),
            SeoDescription = string.IsNullOrWhiteSpace(seoDescription) ? null : seoDescription.Trim(),
            IsFeatured = isFeatured,
            DisplayOrder = displayOrder,
            Status = status
        };

        if (status == EBlogPostStatus.Published)
            post.PublishedAt = DateTime.UtcNow;

        return post;
    }

    /// <summary>Applies full editable-field updates to the post.</summary>
    public void Update(
        string title,
        string slug,
        string? excerpt,
        string? contentJson,
        string contentHtml,
        string? coverImageUrl,
        string? coverImageFileName,
        string? seoTitle,
        string? seoDescription,
        bool isFeatured,
        int displayOrder,
        EBlogPostStatus status)
    {
        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Excerpt = string.IsNullOrWhiteSpace(excerpt) ? null : excerpt.Trim();
        ContentJson = string.IsNullOrWhiteSpace(contentJson) ? null : contentJson;
        ContentHtml = contentHtml;
        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
        CoverImageFileName = string.IsNullOrWhiteSpace(coverImageFileName) ? null : coverImageFileName.Trim();
        SeoTitle = string.IsNullOrWhiteSpace(seoTitle) ? null : seoTitle.Trim();
        SeoDescription = string.IsNullOrWhiteSpace(seoDescription) ? null : seoDescription.Trim();
        IsFeatured = isFeatured;
        DisplayOrder = displayOrder;

        SetStatus(status);
    }

    /// <summary>Changes the publication state while preserving first-publish history.</summary>
    public void SetStatus(EBlogPostStatus status)
    {
        Status = status;
        if (status == EBlogPostStatus.Published && PublishedAt is null)
            PublishedAt = DateTime.UtcNow;
    }

    /// <summary>Replaces category assignments with the provided category ids.</summary>
    public void ReplaceCategories(IEnumerable<Guid> categoryIds)
    {
        BlogPostCategories.Clear();
        foreach (var categoryId in categoryIds.Distinct())
        {
            BlogPostCategories.Add(BlogPostCategory.Create(Id, categoryId));
        }
    }
}
