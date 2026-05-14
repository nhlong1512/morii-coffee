using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Product.UploadProductImages;

/// <summary>
/// Uploads one or more gallery images for a product to the public S3 bucket and persists the records.
/// Business rules enforced:
/// <list type="bullet">
///   <item>Product must exist and not be deleted.</item>
///   <item>Total images per product must not exceed 10 after the upload.</item>
///   <item>Files are stored at <c>products/{productId}/{timestamp}-{filename}</c> in S3.</item>
///   <item>The storage key is stored in <see cref="ProductImage.Url"/>; the full CDN URL is resolved at read time via <c>CdnUrlHelper</c>.</item>
///   <item>If <c>Product.ThumbnailUrl</c> is null, it is automatically set to the first uploaded image's storage key.</item>
/// </list>
/// </summary>
public class UploadProductImagesCommandHandler : ICommandHandler<UploadProductImagesCommand, List<ProductImageDto>>
{
    private const int MaxImagesPerProduct = 10;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public UploadProductImagesCommandHandler(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<List<ProductImageDto>> Handle(
        UploadProductImagesCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException("Product", request.ProductId);

        int existingCount = await _unitOfWork.ProductImages.CountByProductIdAsync(request.ProductId);
        if (existingCount + request.Files.Count > MaxImagesPerProduct)
            throw new BadRequestException(
                $"A product can have at most {MaxImagesPerProduct} images. " +
                $"Currently has {existingCount}; uploading {request.Files.Count} would exceed the limit.");

        int nextOrder = existingCount + 1;

        // Step 1: Upload all files to S3
        var uploadedImages = new List<ProductImage>();
        for (int i = 0; i < request.Files.Count; i++)
        {
            var file = request.Files[i];
            var s3Key = S3KeyHelper.BuildS3Key(request.ProductId, file.FileName);
            var uploadResult = await _fileService.UploadAsync(file, FileContainers.PRODUCTS, s3Key);

            uploadedImages.Add(ProductImageFactory.CreateImage(
                request.ProductId, uploadResult.Blob.StorageKey!, s3Key, displayOrder: nextOrder + i));
        }

        // Step 2: Persist to DB inside a retriable transaction.
        // If the product has no thumbnail yet, promote the first uploaded image.
        bool needsThumbnailUpdate = string.IsNullOrEmpty(product.ThumbnailUrl);
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var image in uploadedImages)
                await _unitOfWork.ProductImages.CreateAsync(image);

            if (needsThumbnailUpdate)
            {
                product.ThumbnailUrl = uploadedImages[0].Url;
                await _unitOfWork.Products.Update(product);
            }
        });

        return uploadedImages.Select(image => _mapper.Map<ProductImageDto>(image)).ToList();
    }

}
