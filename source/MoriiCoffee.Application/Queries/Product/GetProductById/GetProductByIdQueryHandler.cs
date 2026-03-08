using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Product.GetProductById;

public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(
            request.ProductId,
            p => p.ProductCategories.Select(pc => pc.Category),
            p => p.Variants,
            p => p.Images)
            ?? throw new NotFoundException("Product", request.ProductId);

        var dto = _mapper.Map<ProductDto>(product);

        // Compute total price for each variant
        foreach (var variant in dto.Variants)
        {
            variant.TotalPrice = product.BasePrice + variant.AdditionalPrice;
        }

        return dto;
    }
}
