namespace MoriiCoffee.Domain.Shared.Enums.Blog;

/// <summary>
/// Lifecycle states for a blog post managed by the internal CMS.
/// Only <see cref="Published"/> content is visible on the public storefront.
/// </summary>
public enum EBlogPostStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
