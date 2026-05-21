using MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;

/// <summary>
/// Represents a blog category used to organize editorial content for admin management
/// and storefront navigation.
/// </summary>
[Table("BlogCategories")]
public class BlogCategory : AggregateRoot
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Display name shown in admin and storefront navigation.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>Unique slug used by storefront consumers.</summary>
    [Required]
    [MaxLength(150)]
    public string Slug { get; set; } = null!;

    /// <summary>Optional short description of the category.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Explicit ordering for admin and storefront category display.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether this category should appear in public category listings.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Post assignments that reference this category.</summary>
    public ICollection<BlogPostCategory> BlogPostCategories { get; set; } = new List<BlogPostCategory>();

    /// <summary>Creates a new blog category with normalized slug and safe defaults.</summary>
    public static BlogCategory Create(string name, string slug, string? description, int displayOrder, bool isActive)
    {
        return new BlogCategory
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            DisplayOrder = displayOrder,
            IsActive = isActive
        };
    }

    /// <summary>Updates editable category fields.</summary>
    public void Update(string name, string slug, string? description, int displayOrder, bool isActive)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }
}
