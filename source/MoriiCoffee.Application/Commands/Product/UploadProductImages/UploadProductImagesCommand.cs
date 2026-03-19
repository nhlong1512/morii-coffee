using Microsoft.AspNetCore.Http;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Product.UploadProductImages;

/// <summary>Command to upload one or more gallery images for a product.</summary>
public class UploadProductImagesCommand : ICommand<List<ProductImageDto>>
{
    public UploadProductImagesCommand(Guid productId, List<IFormFile> files)
    {
        ProductId = productId;
        Files = files;
    }

    /// <summary>ID of the product to attach images to.</summary>
    public Guid ProductId { get; }

    /// <summary>Image files to upload. Validated for type (jpg/jpeg/png/webp) and size (max 5 MB each).</summary>
    public List<IFormFile> Files { get; }
}
