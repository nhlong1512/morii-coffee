namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Configuration for the Redis connection and per-feature TTL policies.
/// Bind from the "RedisSettings" section in appsettings.json.
/// </summary>
public class RedisSettings
{
    /// <summary>Redis connection string (e.g. "localhost:6379" or "host:port,password=xxx").</summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>TTL in seconds for paginated product list cache entries.</summary>
    public int CatalogListTtlSeconds { get; set; } = 300;

    /// <summary>TTL in seconds for single product detail cache entries.</summary>
    public int CatalogDetailTtlSeconds { get; set; } = 600;

    /// <summary>Sliding TTL in seconds for authenticated user cart documents.</summary>
    public int CartTtlSeconds { get; set; } = 86400;

    /// <summary>TTL in seconds for one-time password reset tickets.</summary>
    public int PasswordResetTicketTtlSeconds { get; set; } = 3600;
}
