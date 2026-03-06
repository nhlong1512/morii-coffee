using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;

/// <summary>
/// Handles adding a new variant to a product.
/// If the new variant is marked as default, all other variants' IsDefault flag is cleared first.
/// </summary>
public class CreateProductVariantCommandHandler : ICommandHandler<CreateProductVariantCommand, ProductVariantDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProductVariantCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductVariantDto> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        // Ensure the product exists
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException("Product", request.ProductId);

        // Clear default flag on other variants if this one will be default
        if (request.IsDefault)
        {
            await _unitOfWork.ProductVariants.ClearDefaultFlagAsync(request.ProductId);
            await _unitOfWork.CommitAsync();
        }

        var variant = new Domain.Aggregates.ProductAggregate.Entities.ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Name = request.Name,
            Size = request.Size,
            AdditionalPrice = request.AdditionalPrice,
            Sku = request.Sku,
            StockQuantity = request.StockQuantity,
            IsDefault = request.IsDefault,
            IsAvailable = true
        };

        await _unitOfWork.ProductVariants.CreateAsync(variant);
        await _unitOfWork.CommitAsync();

        var dto = _mapper.Map<ProductVariantDto>(variant);
        dto.TotalPrice = product.BasePrice + variant.AdditionalPrice;

        return dto;
    }
}
