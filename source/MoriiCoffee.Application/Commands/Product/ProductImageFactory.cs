using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

namespace MoriiCoffee.Application.Commands.Product;

/// <summary>
/// Factory for building <see cref="ProductImage"/> entities.
/// Centralises the entity-creation logic used by both
/// <c>CreateProductCommandHandler</c> and <c>UploadProductImagesCommandHandler</c>.
/// S3 key generation is handled by <see cref="MoriiCoffee.Application.SeedWork.Helpers.S3KeyHelper"/>.
/// </summary>
internal static class ProductImageFactory
{
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
