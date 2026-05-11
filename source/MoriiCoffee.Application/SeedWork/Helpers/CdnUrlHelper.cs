namespace MoriiCoffee.Application.SeedWork.Helpers;

/// <summary>
/// Resolves a stored S3 storage key into a full CDN URL at query time.
/// Storing keys instead of absolute URLs means CDN domain changes only require a config update,
/// not a database migration.
/// </summary>
public static class CdnUrlHelper
{
    /// <summary>
    /// Builds a full URL from a storage key and a CDN base URL.
    /// <list type="bullet">
    ///   <item>If <paramref name="storageKey"/> is null/empty → returns null.</item>
    ///   <item>If <paramref name="storageKey"/> already starts with "http" (legacy absolute URL or presigned URL) → returns it as-is.</item>
    ///   <item>If <paramref name="cdnBaseUrl"/> is empty (local MinIO dev) → returns the key as-is.</item>
    ///   <item>Otherwise → <c>{cdnBaseUrl}/{storageKey}</c>.</item>
    /// </list>
    /// </summary>
    public static string? Resolve(string? storageKey, string? cdnBaseUrl)
    {
        if (string.IsNullOrEmpty(storageKey)) return null;
        if (storageKey.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return storageKey;
        if (string.IsNullOrEmpty(cdnBaseUrl)) return storageKey;
        return $"{cdnBaseUrl.TrimEnd('/')}/{storageKey}";
    }
}
