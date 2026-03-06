using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.ProductVariant.GetVariantsByProductId;

public class GetVariantsByProductIdQueryHandler
    : IQueryHandler<GetVariantsByProductIdQuery, IEnumerable<ProductVariantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetVariantsByProductIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductVariantDto>> Handle(GetVariantsByProductIdQuery request,
        CancellationToken cancellationToken)
    {
        // Ensure product exists
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException("Product", request.ProductId);

        var variants = await _unitOfWork.ProductVariants.GetByProductIdAsync(request.ProductId);

        return variants
            .OrderBy(v => v.Size)
            .Select(v =>
            {
                var dto = _mapper.Map<ProductVariantDto>(v);
                dto.TotalPrice = product.BasePrice + v.AdditionalPrice;
                return dto;
            });
    }
}
