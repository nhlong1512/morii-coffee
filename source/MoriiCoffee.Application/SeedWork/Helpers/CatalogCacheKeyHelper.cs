using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.SeedWork.Helpers;

/// <summary>
/// Builds deterministic Redis cache key strings from <see cref="ProductPaginationFilter"/> instances.
/// The same filter values always produce the same key, regardless of object identity.
/// Lives in Application so query handlers can build keys without referencing Infrastructure.
/// </summary>
public static class CatalogCacheKeyHelper
{
    /// <summary>
    /// Produces a short, deterministic cache key suffix from the filter's discriminating fields:
    /// Page, Size, TakeAll, CategoryId, and IsFeatured.
    /// </summary>
    public static string BuildListKey(ProductPaginationFilter filter)
    {
        var page = filter.Page;
        var size = filter.Size;
        var takeAll = filter.TakeAll ? "1" : "0";
        var category = filter.CategoryId.HasValue ? filter.CategoryId.Value.ToString("N") : "all";
        var featured = filter.IsFeatured.HasValue ? filter.IsFeatured.Value.ToString().ToLowerInvariant() : "any";

        return $"p{page}-s{size}-all{takeAll}-cat{category}-feat{featured}";
    }
}
