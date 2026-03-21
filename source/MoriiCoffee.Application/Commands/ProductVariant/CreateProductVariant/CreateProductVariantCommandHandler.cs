using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;

/// <summary>
/// Handles adding one or more variants to a product in a single operation.
/// If any variant in the batch is marked as default, all pre-existing default flags are cleared first.
/// </summary>
public class CreateProductVariantCommandHandler : ICommandHandler<CreateProductVariantCommand, List<ProductVariantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProductVariantCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ProductVariantDto>> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        // Ensure the product exists
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException("Product", request.ProductId);

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
                IsAvailable = true
            };

            await _unitOfWork.ProductVariants.CreateAsync(variant);
            createdVariants.Add(variant);
        }

        await _unitOfWork.CommitAsync();

        return createdVariants.Select(v =>
        {
            var result = _mapper.Map<ProductVariantDto>(v);
            result.TotalPrice = product.BasePrice + v.AdditionalPrice;
            return result;
        }).ToList();
    }
}
