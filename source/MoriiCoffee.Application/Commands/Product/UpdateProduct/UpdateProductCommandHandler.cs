using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Product.UpdateProduct;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id)
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
        {
            throw new BadRequestException($"The slug '{slug}' is already in use by another product.");
        }

        // Update scalar fields
        product.Name = request.Name;
        product.Slug = slug;
        product.Description = request.Description;
        product.BasePrice = request.BasePrice;
        product.ThumbnailUrl = request.ThumbnailUrl;
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
