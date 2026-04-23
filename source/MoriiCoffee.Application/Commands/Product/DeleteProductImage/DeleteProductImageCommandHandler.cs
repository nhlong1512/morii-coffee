using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Product.DeleteProductImage;

/// <summary>
/// Deletes a product gallery image from the database and the S3 bucket.
/// Gallery images are independent from the product thumbnail — deleting a gallery image
/// does not affect <c>Product.ThumbnailUrl</c>.
/// Business rules enforced:
/// <list type="bullet">
///   <item>Image must exist and belong to the specified product.</item>
///   <item>The S3 file is deleted after the DB commit succeeds.</item>
/// </list>
/// </summary>
public class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IProductCatalogCache _catalogCache;

    public DeleteProductImageCommandHandler(
        IUnitOfWork unitOfWork,
        IFileService fileService,
        IProductCatalogCache catalogCache)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _catalogCache = catalogCache;
    }

    public async Task<bool> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _unitOfWork.ProductImages.GetByIdAsync(request.ImageId)
            ?? throw new NotFoundException("ProductImage", request.ImageId);

        if (image.ProductId != request.ProductId)
            throw new NotFoundException("ProductImage", request.ImageId);

        string s3KeyToDelete = image.S3Key;

        await _unitOfWork.ProductImages.Delete(image);
        await _unitOfWork.CommitAsync();

        await _catalogCache.InvalidateProductAsync(request.ProductId);
        await _catalogCache.InvalidateAllListsAsync();

        await _fileService.DeleteAsync(FileContainers.PRODUCTS, s3KeyToDelete);

        return true;
    }
}
