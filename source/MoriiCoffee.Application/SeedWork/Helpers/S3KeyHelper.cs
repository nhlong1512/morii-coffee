using System.Text.RegularExpressions;

namespace MoriiCoffee.Application.SeedWork.Helpers;

/// <summary>
/// Shared helper for generating S3 object keys used across all file-upload features
/// (products, banners, categories, etc.).
/// </summary>
public static class S3KeyHelper
{
    /// <summary>
    /// Builds an S3 object key as <c>{entityId}/{timestamp}-{sanitized-filename}</c>.
    /// The container prefix (e.g. <c>products/</c>, <c>banners/</c>) is prepended by the S3 service —
    /// do not include it here.
    /// </summary>
    /// <param name="entityId">The ID of the owning entity (product, banner, etc.).</param>
    /// <param name="originalFileName">The original file name from the upload (e.g. "my banner.jpg").</param>
    /// <returns>A unique, URL-safe S3 object key.</returns>
    public static string BuildS3Key(Guid entityId, string originalFileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safe = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\-_]", "-");
        return $"{entityId}/{timestamp}-{safe}{ext}";
    }
}
