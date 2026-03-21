using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.Product;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Commands.Product.CreateProduct;

/// <summary>
/// Handles the creation of a new product.
/// If a thumbnail file is provided, it is uploaded to S3 and persisted as a
/// <see cref="ProductImage"/> with <c>DisplayOrder = 1</c>.
/// <c>Product.ThumbnailUrl</c> is also set for fast display.
/// Generates a unique slug from the product name if one is not provided.
/// </summary>
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Validate that the referenced categories exist
        var categories = new List<CategoryEntity>();
        foreach (var categoryId in request.CategoryIds)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId)
                ?? throw new NotFoundException("Category", categoryId);
            categories.Add(category);
        }

        // Generate or validate slug
        string slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug.ToLowerInvariant();

        // Ensure slug uniqueness
        bool slugExists = await _unitOfWork.Products.SlugExistsAsync(slug);
        if (slugExists)
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        // Pre-generate the product ID so the S3 key can reference it
        var productId = Guid.NewGuid();

        // Upload thumbnail if provided — also creates a ProductImage with DisplayOrder = 1
        string? thumbnailUrl = null;
        ProductImage? thumbnailImage = null;
        if (request.Thumbnail != null)
        {
            var s3Key = ProductImageFactory.BuildS3Key(productId, request.Thumbnail.FileName);
            var uploadResult = await _fileService.UploadAsync(request.Thumbnail, FileContainers.PRODUCTS, s3Key);

            thumbnailUrl = uploadResult.Blob.Uri;
            thumbnailImage = ProductImageFactory.CreateImage(productId, thumbnailUrl!, s3Key, displayOrder: 1);
        }

        var product = new ProductEntity
        {
            Id = productId,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            BasePrice = request.BasePrice,
            ThumbnailUrl = thumbnailUrl,
            Status = EProductStatus.Active,
            IsFeatured = request.IsFeatured,
            DisplayOrder = request.DisplayOrder
        };

        // Add product categories
        foreach (var category in categories)
        {
            product.ProductCategories.Add(new ProductCategory
            {
                CategoryId = category.Id,
                ProductId = product.Id
            });
        }

        await _unitOfWork.Products.CreateAsync(product);

        if (thumbnailImage != null)
            await _unitOfWork.ProductImages.CreateAsync(thumbnailImage);

        await _unitOfWork.CommitAsync();

        return _mapper.Map<ProductDto>(product);
    }

    /// <summary>Generates a URL-friendly slug from a product name.</summary>
    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(
            name.ToLowerInvariant().Trim(),
            @"[^a-z0-9\s-]", "")
        .Replace(" ", "-")
        .Trim('-');
}
