using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Product.UpdateProduct;

/// <summary>
/// Handles updating an existing product.
/// When a new thumbnail file is provided, the old MinIO object is deleted first,
/// then the new file is uploaded before the DB commit.
/// </summary>
public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // Load product WITH its existing ProductCategories so EF Core tracks the collection
        // and can delete removed rows when Clear() is called.
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id,
            p => p.ProductCategories)
            ?? throw new NotFoundException("Product", request.Id);

        // Validate that each referenced category exists
        var categories = new List<MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category>();
        foreach (var catId in request.CategoryIds)
        {
            var cat = await _unitOfWork.Categories.GetByIdAsync(catId)
                ?? throw new NotFoundException("Category", catId);
            categories.Add(cat);
        }

        // Handle slug update
        string slug = string.IsNullOrWhiteSpace(request.Slug)
            ? product.Slug
            : request.Slug.ToLowerInvariant();

        // Ensure slug uniqueness (excluding current product)
        bool slugExists = await _unitOfWork.Products.SlugExistsAsync(slug, request.Id);
        if (slugExists)
            throw new BadRequestException($"The slug '{slug}' is already in use by another product.");

        // Replace thumbnail: delete old from MinIO, upload new
        if (request.Thumbnail != null)
        {
            if (!string.IsNullOrEmpty(product.ThumbnailFileName))
                await _fileService.DeleteAsync(FileContainers.PRODUCTS, product.ThumbnailFileName);

            var uploadResult = await _fileService.UploadAsync(request.Thumbnail, FileContainers.PRODUCTS);
            product.ThumbnailUrl = uploadResult.Blob.StorageKey;
            product.ThumbnailFileName = uploadResult.Blob.Name;
        }

        // Update scalar fields
        product.Name = request.Name;
        product.Slug = slug;
        product.Description = request.Description;
        product.BasePrice = request.BasePrice;
        product.Status = request.Status;
        product.IsFeatured = request.IsFeatured;
        product.DisplayOrder = request.DisplayOrder;

        // Replace category relationships
        product.ProductCategories.Clear();
        foreach (var cat in categories)
        {
            product.ProductCategories.Add(new MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects.ProductCategory
            {
                CategoryId = cat.Id,
                ProductId = product.Id
            });
        }

        await _unitOfWork.Products.Update(product);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<ProductDto>(product);
    }
}
