using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Product.ReorderProductImages;

/// <summary>Command to reorder a product's gallery images.</summary>
public class ReorderProductImagesCommand : ICommand<List<ProductImageDto>>
{
    public ReorderProductImagesCommand(Guid productId, List<Guid> imageIds)
    {
        ProductId = productId;
        ImageIds = imageIds;
    }

    /// <summary>ID of the product whose images should be reordered.</summary>
    public Guid ProductId { get; }

    /// <summary>
    /// All image IDs for the product in the desired display order.
    /// Images are assigned <c>DisplayOrder</c> values of 1, 2, 3… based on their position in this list.
    /// </summary>
    public List<Guid> ImageIds { get; }
}
