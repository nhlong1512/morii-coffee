using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Product.SetProductImageThumbnail;

/// <summary>
/// Promotes a gallery image to be the product thumbnail.
/// Atomically clears the <c>IsThumbnail</c> flag on all other images for the product,
/// sets it on the target image, and updates <c>Product.ThumbnailUrl</c>.
/// </summary>
public class SetProductImageThumbnailCommandHandler : ICommandHandler<SetProductImageThumbnailCommand, ProductImageDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SetProductImageThumbnailCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductImageDto> Handle(
        SetProductImageThumbnailCommand request,
        CancellationToken cancellationToken)
    {
        var image = await _unitOfWork.ProductImages.GetByIdAsync(request.ImageId)
            ?? throw new NotFoundException("ProductImage", request.ImageId);

        if (image.ProductId != request.ProductId)
            throw new NotFoundException("ProductImage", request.ImageId);

        // Clear the thumbnail flag on all other images for this product
        await _unitOfWork.ProductImages.ClearThumbnailFlagAsync(request.ProductId, excludeImageId: request.ImageId);

        // Promote the target image
        image.IsThumbnail = true;
        await _unitOfWork.ProductImages.Update(image);

        // Sync the product's ThumbnailUrl
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException("Product", request.ProductId);

        product.ThumbnailUrl = image.Url;
        await _unitOfWork.Products.Update(product);

        await _unitOfWork.CommitAsync();

        return _mapper.Map<ProductImageDto>(image);
    }
}
