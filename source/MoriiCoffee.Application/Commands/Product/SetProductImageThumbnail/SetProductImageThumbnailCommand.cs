using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Product.SetProductImageThumbnail;

/// <summary>Command to promote a gallery image to the product thumbnail.</summary>
public class SetProductImageThumbnailCommand : ICommand<ProductImageDto>
{
    public SetProductImageThumbnailCommand(Guid productId, Guid imageId)
    {
        ProductId = productId;
        ImageId = imageId;
    }

    /// <summary>ID of the product that owns the image.</summary>
    public Guid ProductId { get; }

    /// <summary>ID of the image to promote as the thumbnail.</summary>
    public Guid ImageId { get; }
}
