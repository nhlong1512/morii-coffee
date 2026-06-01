using MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;
using MoriiCoffee.Domain.SeedWork.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities;

/// <summary>
/// Join entity connecting blog posts and blog categories.
/// </summary>
[Table("BlogPostCategories")]
public class BlogPostCategory : EntityBase
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Foreign key to the owning blog post.</summary>
    public Guid BlogPostId { get; set; }

    /// <summary>Navigation back to the owning blog post.</summary>
    public BlogPost BlogPost { get; set; } = null!;

    /// <summary>Foreign key to the linked category.</summary>
    public Guid BlogCategoryId { get; set; }

    /// <summary>Navigation to the linked category.</summary>
    public BlogCategory BlogCategory { get; set; } = null!;

    /// <summary>Creates a new post-category assignment.</summary>
    public static BlogPostCategory Create(Guid blogPostId, Guid blogCategoryId)
    {
        return new BlogPostCategory
        {
            BlogPostId = blogPostId,
            BlogCategoryId = blogCategoryId
        };
    }
}
