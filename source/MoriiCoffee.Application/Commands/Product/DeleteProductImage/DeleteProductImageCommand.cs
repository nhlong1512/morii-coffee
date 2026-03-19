using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Product.DeleteProductImage;

/// <summary>Command to delete a product gallery image from both S3 and the database.</summary>
public class DeleteProductImageCommand : ICommand<bool>
{
    public DeleteProductImageCommand(Guid productId, Guid imageId)
    {
        ProductId = productId;
        ImageId = imageId;
    }

    /// <summary>ID of the product that owns the image (used for access validation).</summary>
    public Guid ProductId { get; }

    /// <summary>ID of the image record to delete.</summary>
    public Guid ImageId { get; }
}
