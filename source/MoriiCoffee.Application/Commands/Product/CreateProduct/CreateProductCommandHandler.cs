using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Product;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Commands.Product.CreateProduct;

/// <summary>
/// Handles the creation of a new product.
/// Generates a unique slug from the product name if one is not provided.
/// </summary>
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Validate that the referenced category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId)
            ?? throw new NotFoundException("Category", request.CategoryId);

        // Generate or validate slug
        string slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug.ToLowerInvariant();

        // Ensure slug uniqueness
        bool slugExists = await _unitOfWork.Products.SlugExistsAsync(slug);
        if (slugExists)
        {
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            ThumbnailUrl = request.ThumbnailUrl,
            Status = EProductStatus.Active,
            IsFeatured = request.IsFeatured,
            DisplayOrder = request.DisplayOrder
        };

        await _unitOfWork.Products.CreateAsync(product);
        await _unitOfWork.CommitAsync();

        // Reload with category for DTO mapping
        product.Category = category;

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
