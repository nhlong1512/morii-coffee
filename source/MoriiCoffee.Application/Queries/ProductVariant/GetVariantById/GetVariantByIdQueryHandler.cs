using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.ProductVariant.GetVariantById;

public class GetVariantByIdQueryHandler : IQueryHandler<GetVariantByIdQuery, ProductVariantDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetVariantByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductVariantDto> Handle(GetVariantByIdQuery request, CancellationToken cancellationToken)
    {
        var variant = await _unitOfWork.ProductVariants.GetByIdAsync(
            request.VariantId,
            v => v.Product!)
            ?? throw new NotFoundException("ProductVariant", request.VariantId);

        var dto = _mapper.Map<ProductVariantDto>(variant);
        dto.TotalPrice = (variant.Product?.BasePrice ?? 0) + variant.AdditionalPrice;

        return dto;
    }
}
