namespace MoriiCoffee.Application.SeedWork.DTOs.Blog;

/// <summary>
/// Payload for reordering blog categories in batch.
/// </summary>
public class ReorderBlogCategoriesDto
{
    public List<ReorderBlogCategoriesItemDto> Items { get; set; } = new();
}

/// <summary>
/// A single category/order pair used during blog category reorder operations.
/// </summary>
public class ReorderBlogCategoriesItemDto
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
}
