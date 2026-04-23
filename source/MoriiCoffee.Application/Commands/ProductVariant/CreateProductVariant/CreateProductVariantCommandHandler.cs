using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;

/// <summary>
/// Handles creating one or more variants for a product in a single operation.
/// Rejects the request if any incoming size already exists on the product — use PUT to update existing variants.
/// If any variant in the batch is marked as default, all pre-existing default flags are cleared first.
/// </summary>
public class CreateProductVariantCommandHandler : ICommandHandler<CreateProductVariantCommand, List<ProductVariantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProductCatalogCache _catalogCache;

    public CreateProductVariantCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IProductCatalogCache catalogCache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _catalogCache = catalogCache;
    }

    public async Task<List<ProductVariantDto>> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        // Ensure the product exists
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException("Product", request.ProductId);

        // Guard against duplicate sizes — each product may have at most one variant per size
        var existingVariants = await _unitOfWork.ProductVariants.GetByProductIdAsync(request.ProductId);
        var existingSizes = existingVariants.Select(v => v.Size).ToHashSet();
        var duplicates = request.Variants
            .Where(v => existingSizes.Contains(v.Size))
            .Select(v => v.Size.ToString())
            .ToList();

        if (duplicates.Count > 0)
            throw new BadRequestException(
                $"Variant(s) with the following size(s) already exist for this product: {string.Join(", ", duplicates)}. Use PUT to update existing variants.");

        // Clear existing default flag if any variant in the batch is marked as default
        bool anyDefault = request.Variants.Any(v => v.IsDefault);
        if (anyDefault)
            await _unitOfWork.ProductVariants.ClearDefaultFlagAsync(request.ProductId);

        var createdVariants = new List<Domain.Aggregates.ProductAggregate.Entities.ProductVariant>();
        foreach (var dto in request.Variants)
        {
            var variant = new Domain.Aggregates.ProductAggregate.Entities.ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                Name = dto.Name,
                Size = dto.Size,
                AdditionalPrice = dto.AdditionalPrice,
                Sku = dto.Sku,
                StockQuantity = dto.StockQuantity,
                IsDefault = dto.IsDefault,
                IsAvailable = dto.IsAvailable
            };

            await _unitOfWork.ProductVariants.CreateAsync(variant);
            createdVariants.Add(variant);
        }

        await _unitOfWork.CommitAsync();

        await _catalogCache.InvalidateProductAsync(request.ProductId);
        await _catalogCache.InvalidateAllListsAsync();

        return createdVariants.Select(v =>
        {
            var result = _mapper.Map<ProductVariantDto>(v);
            result.TotalPrice = product.BasePrice + v.AdditionalPrice;
            return result;
        }).ToList();
    }
}
