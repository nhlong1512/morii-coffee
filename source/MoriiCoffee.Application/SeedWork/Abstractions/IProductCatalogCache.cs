using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Read-through cache for hot product catalog reads.
/// Tracks active list-cache keys so write handlers can invalidate them without a Redis key scan.
/// Catalog reads fall back to the primary database when Redis is unavailable.
/// </summary>
public interface IProductCatalogCache
{
    /// <summary>Returns a cached paginated product list or null on a cache miss or Redis failure.</summary>
    Task<Pagination<ProductSummaryDto>?> GetListAsync(string cacheKey);

    /// <summary>Stores a paginated product list under <paramref name="cacheKey"/> and registers the key for bulk invalidation.</summary>
    Task SetListAsync(string cacheKey, Pagination<ProductSummaryDto> value);

    /// <summary>Returns a cached product detail DTO or null on a cache miss or Redis failure.</summary>
    Task<ProductDto?> GetDetailAsync(Guid productId);

    /// <summary>Stores a product detail DTO keyed by <paramref name="productId"/>.</summary>
    Task SetDetailAsync(Guid productId, ProductDto dto);

    /// <summary>Removes the detail cache entry for <paramref name="productId"/>.</summary>
    Task InvalidateProductAsync(Guid productId);

    /// <summary>Removes all tracked paginated list cache entries.</summary>
    Task InvalidateAllListsAsync();
}
