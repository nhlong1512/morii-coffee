using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Infrastructure.Services.Redis;

/// <summary>
/// Wraps a paginated product list with cache metadata for Redis storage.
/// </summary>
/// <param name="Data">The paginated list of product summary DTOs.</param>
/// <param name="CachedAt">UTC timestamp when the entry was written to the cache.</param>
public record CatalogListCacheEntry(
    Pagination<ProductSummaryDto> Data,
    DateTime CachedAt);

/// <summary>
/// Wraps a single product detail DTO with cache metadata for Redis storage.
/// </summary>
/// <param name="Data">The full product detail DTO.</param>
/// <param name="CachedAt">UTC timestamp when the entry was written to the cache.</param>
public record CatalogDetailCacheEntry(
    ProductDto Data,
    DateTime CachedAt);

/// <summary>
/// Provides deterministic Redis key constants and key-builder methods for the product catalog cache.
/// Centralising key patterns here ensures writers and readers always agree on key names.
/// </summary>
public static class CacheKeys
{
    /// <summary>Redis SET key used to track all active product list cache keys for bulk invalidation.</summary>
    public const string ListKeyRegistry = "catalog:list:keys";

    /// <summary>
    /// Builds the Redis key for a paginated product list entry.
    /// Format: <c>catalog:list:{cacheKey}</c>.
    /// </summary>
    public static string ProductListKey(string cacheKey) => $"catalog:list:{cacheKey}";

    /// <summary>
    /// Builds the Redis key for a single product detail entry.
    /// Format: <c>catalog:detail:{id}</c>.
    /// </summary>
    public static string ProductDetailKey(Guid id) => $"catalog:detail:{id}";
}

