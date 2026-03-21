using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

namespace MoriiCoffee.Application.Commands.Product;

/// <summary>
/// Shared factory for building <see cref="ProductImage"/> entities and their S3 keys.
/// Centralises the key-generation and sanitisation logic used by both
/// <c>CreateProductCommandHandler</c> and <c>UploadProductImagesCommandHandler</c>.
/// </summary>
internal static class ProductImageFactory
{
    /// <summary>
    /// Builds the S3 object key as <c>{productId}/{timestamp}-{sanitized-filename}</c>.
    /// The container prefix (e.g. <c>products/</c>) is prepended by the S3 service.
    /// </summary>
    internal static string BuildS3Key(Guid productId, string originalFileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safe = System.Text.RegularExpressions.Regex.Replace(
            name.ToLowerInvariant(), @"[^a-z0-9\-_]", "-");
        return $"{productId}/{timestamp}-{safe}{ext}";
    }

    /// <summary>
    /// Creates a new <see cref="ProductImage"/> entity from an S3 upload result.
    /// </summary>
    /// <param name="productId">The owning product's identifier.</param>
    /// <param name="url">Public CDN/S3 URL returned by the file service.</param>
    /// <param name="s3Key">The S3 object key used during upload.</param>
    /// <param name="displayOrder">Position of this image in the product gallery.</param>
    internal static ProductImage CreateImage(Guid productId, string url, string s3Key, int displayOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Url = url,
            S3Key = s3Key,
            DisplayOrder = displayOrder
        };
}
