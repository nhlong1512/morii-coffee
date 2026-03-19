using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Product.DeleteProductImage;

/// <summary>
/// Deletes a product gallery image from the database and the S3 bucket.
/// Business rules enforced:
/// <list type="bullet">
///   <item>Image must exist and belong to the specified product.</item>
///   <item>The S3 file is deleted after the DB commit succeeds.</item>
///   <item>
///     If the deleted image was the thumbnail, the image with the next lowest
///     <c>DisplayOrder</c> is automatically promoted as the new thumbnail.
///     When no images remain, <c>Product.ThumbnailUrl</c> is set to <c>null</c>.
///   </item>
/// </list>
/// </summary>
public class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;

    public DeleteProductImageCommandHandler(IUnitOfWork unitOfWork, IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
    }

    public async Task<bool> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _unitOfWork.ProductImages.GetByIdAsync(request.ImageId)
            ?? throw new NotFoundException("ProductImage", request.ImageId);

        if (image.ProductId != request.ProductId)
            throw new NotFoundException("ProductImage", request.ImageId);

        bool wasThumbnail = image.IsThumbnail;
        string s3KeyToDelete = image.S3Key;

        // Hard-delete the image record
        await _unitOfWork.ProductImages.Delete(image);
        await _unitOfWork.CommitAsync();

        // Promote the next image as thumbnail if the deleted one was the thumbnail
        if (wasThumbnail)
        {
            var remaining = (await _unitOfWork.ProductImages.GetByProductIdAsync(request.ProductId))
                .OrderBy(i => i.DisplayOrder)
                .ToList();

            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
            if (product != null)
            {
                if (remaining.Count > 0)
                {
                    remaining[0].IsThumbnail = true;
                    await _unitOfWork.ProductImages.Update(remaining[0]);
                    product.ThumbnailUrl = remaining[0].Url;
                }
                else
                {
                    product.ThumbnailUrl = null;
                }

                await _unitOfWork.Products.Update(product);
                await _unitOfWork.CommitAsync();
            }
        }

        // Delete from S3 after DB operations succeed (swallows errors internally)
        await _fileService.DeleteAsync(FileContainers.PRODUCTS, s3KeyToDelete);

        return true;
    }
}
