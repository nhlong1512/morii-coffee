using AutoMapper;
using Microsoft.AspNetCore.Http;
using MoriiCoffee.Application.SeedWork.Abstractions;
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
///   <item>CDN URL is stored in <see cref="ProductImage.Url"/>; the S3 key is stored in <see cref="ProductImage.S3Key"/>.</item>
///   <item>If this is the first image for the product, it is automatically set as the thumbnail.</item>
///   <item>If S3 upload succeeds but the DB commit fails, all uploaded S3 objects are rolled back.</item>
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

        bool isFirstImage = existingCount == 0;
        int nextOrder = existingCount + 1;

        var uploadedImages = new List<(ProductImage image, string s3Key)>();
        for (int i = 0; i < request.Files.Count; i++)
        {
            var file = request.Files[i];
            var s3Key = BuildS3Key(request.ProductId, file.FileName);
            var uploadResult = await _fileService.UploadAsync(file, FileContainers.PRODUCTS, s3Key);

            var image = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                Url = uploadResult.Blob.Uri!,
                S3Key = s3Key,
                DisplayOrder = nextOrder + i,
                IsThumbnail = isFirstImage && i == 0
            };

            uploadedImages.Add((image, s3Key));
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (image, _) in uploadedImages)
                await _unitOfWork.ProductImages.CreateAsync(image);

            if (isFirstImage && uploadedImages.Count > 0)
            {
                product.ThumbnailUrl = uploadedImages[0].image.Url;
                await _unitOfWork.Products.Update(product);
            }
        });

        return uploadedImages.Select(x => _mapper.Map<ProductImageDto>(x.image)).ToList();
    }

    /// <summary>
    /// Builds the S3 object key in the format <c>{productId}/{timestamp}-{sanitized-filename}</c>.
    /// The container prefix (<c>products/</c>) is prepended by the S3 service's internal key builder.
    /// </summary>
    private static string BuildS3Key(Guid productId, string originalFileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sanitized = SanitizeFilename(originalFileName);
        return $"{productId}/{timestamp}-{sanitized}";
    }

    private static string SanitizeFilename(string filename)
    {
        var name = Path.GetFileNameWithoutExtension(filename);
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        var safe = System.Text.RegularExpressions.Regex.Replace(
            name.ToLowerInvariant(), @"[^a-z0-9\-_]", "-");
        return $"{safe}{ext}";
    }
}
