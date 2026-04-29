namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Provides a typed abstraction over distributed caching operations.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves and deserializes a cached value by key.
    /// Returns <c>default</c> when the key is not found.
    /// </summary>
    Task<T?> GetDataAsync<T>(string key);

    /// <summary>
    /// Serializes and stores a value in the cache, optionally with expiration.
    /// </summary>
    Task<bool> SetDataAsync<T>(string key, T value, TimeSpan? expirationTime = null);

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task<bool> RemoveDataAsync(string key);
}
