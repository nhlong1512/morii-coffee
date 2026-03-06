using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.ProductVariant.UpdateProductVariant;

public class UpdateProductVariantCommandHandler : ICommandHandler<UpdateProductVariantCommand, ProductVariantDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateProductVariantCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductVariantDto> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _unitOfWork.ProductVariants.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("ProductVariant", request.Id);

        var product = await _unitOfWork.Products.GetByIdAsync(variant.ProductId)
            ?? throw new NotFoundException("Product", variant.ProductId);

        // Clear default flag if this variant becomes the new default
        if (request.IsDefault && !variant.IsDefault)
        {
            await _unitOfWork.ProductVariants.ClearDefaultFlagAsync(variant.ProductId, request.Id);
            await _unitOfWork.CommitAsync();
        }

        variant.Name = request.Name;
        variant.Size = request.Size;
        variant.AdditionalPrice = request.AdditionalPrice;
        variant.Sku = request.Sku;
        variant.StockQuantity = request.StockQuantity;
        variant.IsDefault = request.IsDefault;
        variant.IsAvailable = request.IsAvailable;

        await _unitOfWork.ProductVariants.Update(variant);
        await _unitOfWork.CommitAsync();

        var dto = _mapper.Map<ProductVariantDto>(variant);
        dto.TotalPrice = product.BasePrice + variant.AdditionalPrice;

        return dto;
    }
}
