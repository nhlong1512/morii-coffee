using System.Text.Json;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.Shared.SeedWork;
using MoriiCoffee.Domain.Shared.Settings;
using StackExchange.Redis;

namespace MoriiCoffee.Infrastructure.Services.Redis;

/// <summary>
/// Redis-backed implementation of <see cref="IProductCatalogCache"/>.
/// All methods swallow Redis exceptions and log them at Warning level so catalog reads
/// transparently fall back to the primary database when Redis is unavailable.
/// </summary>
public class ProductCatalogCache : IProductCatalogCache
{
    private readonly IDatabase _db;
    private readonly RedisSettings _settings;
    private readonly ILogger<ProductCatalogCache> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ProductCatalogCache(
        IDatabase db,
        RedisSettings settings,
        ILogger<ProductCatalogCache> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Pagination<ProductSummaryDto>?> GetListAsync(string cacheKey)
    {
        try
        {
            var redisKey = CacheKeys.ProductListKey(cacheKey);
            var value = await _db.StringGetAsync(redisKey);
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("[CatalogCache] List cache MISS: {Key}", redisKey);
                return null;
            }

            var entry = JsonSerializer.Deserialize<CatalogListCacheEntry>((string)value!, SerializerOptions);
            _logger.LogDebug("[CatalogCache] List cache HIT: {Key}", redisKey);
            return entry?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CatalogCache] GetListAsync failed for key '{Key}' — falling back to DB.", cacheKey);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SetListAsync(string cacheKey, Pagination<ProductSummaryDto> value)
    {
        try
        {
            var redisKey = CacheKeys.ProductListKey(cacheKey);
            var entry = new CatalogListCacheEntry(value, DateTime.UtcNow);
            var json = JsonSerializer.Serialize(entry, SerializerOptions);
            var ttl = TimeSpan.FromSeconds(_settings.CatalogListTtlSeconds);

            await _db.StringSetAsync(redisKey, json, ttl);
            await _db.SetAddAsync(CacheKeys.ListKeyRegistry, redisKey);
            _logger.LogDebug("[CatalogCache] List cache SET: {Key} (TTL {Ttl}s)", redisKey, _settings.CatalogListTtlSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CatalogCache] SetListAsync failed for key '{Key}'.", cacheKey);
        }
    }

    /// <inheritdoc/>
    public async Task<ProductDto?> GetDetailAsync(Guid productId)
    {
        try
        {
            var redisKey = CacheKeys.ProductDetailKey(productId);
            var value = await _db.StringGetAsync(redisKey);
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("[CatalogCache] Detail cache MISS: {Key}", redisKey);
                return null;
            }

            var entry = JsonSerializer.Deserialize<CatalogDetailCacheEntry>((string)value!, SerializerOptions);
            _logger.LogDebug("[CatalogCache] Detail cache HIT: {Key}", redisKey);
            return entry?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CatalogCache] GetDetailAsync failed for product '{ProductId}' — falling back to DB.", productId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SetDetailAsync(Guid productId, ProductDto dto)
    {
        try
        {
            var redisKey = CacheKeys.ProductDetailKey(productId);
            var entry = new CatalogDetailCacheEntry(dto, DateTime.UtcNow);
            var json = JsonSerializer.Serialize(entry, SerializerOptions);
            var ttl = TimeSpan.FromSeconds(_settings.CatalogDetailTtlSeconds);

            await _db.StringSetAsync(redisKey, json, ttl);
            _logger.LogDebug("[CatalogCache] Detail cache SET: {Key} (TTL {Ttl}s)", redisKey, _settings.CatalogDetailTtlSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CatalogCache] SetDetailAsync failed for product '{ProductId}'.", productId);
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateProductAsync(Guid productId)
    {
        try
        {
            var redisKey = CacheKeys.ProductDetailKey(productId);
            await _db.KeyDeleteAsync(redisKey);
            _logger.LogInformation("[CatalogCache] Detail cache invalidated for product {ProductId}.", productId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CatalogCache] InvalidateProductAsync failed for product '{ProductId}'.", productId);
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateAllListsAsync()
    {
        try
        {
            var members = await _db.SetMembersAsync(CacheKeys.ListKeyRegistry);
            if (members.Length == 0)
                return;

            var keysToDelete = members
                .Select(m => (RedisKey)m.ToString())
                .Append((RedisKey)CacheKeys.ListKeyRegistry)
                .ToArray();

            await _db.KeyDeleteAsync(keysToDelete);
            _logger.LogInformation("[CatalogCache] Invalidated {Count} list cache entries.", members.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CatalogCache] InvalidateAllListsAsync failed.");
        }
    }
}
