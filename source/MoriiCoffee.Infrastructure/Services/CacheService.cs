using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// Thin wrapper over <see cref="IDistributedCache"/> that adds typed serialization and logging.
/// </summary>
public class CacheService : ICacheService
{
    private readonly ILogger<CacheService> _logger;
    private readonly IDistributedCache _distributedCache;
    private readonly ISerializeService _serializeService;

    public CacheService(
        IDistributedCache distributedCache,
        ISerializeService serializeService,
        ILogger<CacheService> logger)
    {
        _distributedCache = distributedCache;
        _serializeService = serializeService;
        _logger = logger;
    }

    public async Task<T?> GetDataAsync<T>(string key)
    {
        _logger.LogInformation("BEGIN: GetDataAsync<{DataType}>(key: {KeyName})", typeof(T).Name, key);

        string? value = await _distributedCache.GetStringAsync(key);
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogInformation("END: GetDataAsync<{DataType}> cache miss", typeof(T).Name);
            return default;
        }

        T? data = _serializeService.Deserialize<T>(value);
        _logger.LogInformation("END: GetDataAsync<{DataType}> cache hit", typeof(T).Name);
        return data;
    }

    public async Task<bool> SetDataAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        _logger.LogInformation("BEGIN: SetDataAsync<{DataType}>(key: {KeyName})", typeof(T).Name, key);

        var options = new DistributedCacheEntryOptions();
        if (expirationTime.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expirationTime.Value;
        }

        await _distributedCache.SetStringAsync(key, _serializeService.Serialize(value), options);

        _logger.LogInformation("END: SetDataAsync<{DataType}>", typeof(T).Name);
        return true;
    }

    public async Task<bool> RemoveDataAsync(string key)
    {
        try
        {
            _logger.LogInformation("BEGIN: RemoveDataAsync(key: {KeyName})", key);
            await _distributedCache.RemoveAsync(key);
            _logger.LogInformation("END: RemoveDataAsync");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveDataAsync failed for key: {KeyName}", key);
            return false;
        }
    }
}
