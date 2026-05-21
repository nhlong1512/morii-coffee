namespace MoriiCoffee.Application.SeedWork.DTOs.Blog;

/// <summary>
/// Payload for reordering blog posts in batch.
/// </summary>
public class ReorderBlogPostsDto
{
    public List<ReorderBlogPostsItemDto> Items { get; set; } = new();
}

/// <summary>
/// A single post/order pair used during blog reorder operations.
/// </summary>
public class ReorderBlogPostsItemDto
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
}
