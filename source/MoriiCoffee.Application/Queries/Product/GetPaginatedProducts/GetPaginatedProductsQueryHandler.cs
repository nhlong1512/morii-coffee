using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Product.GetPaginatedProducts;

public class GetPaginatedProductsQueryHandler : IQueryHandler<GetPaginatedProductsQuery, Pagination<ProductSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPaginatedProductsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Pagination<ProductSummaryDto>> Handle(GetPaginatedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Products
            .FindAll(false, p => p.ProductCategories.Select(pc => pc.Category));

        // Apply optional filters
        if (request.Filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == request.Filter.CategoryId.Value));
        }

        if (request.Filter.IsFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == request.Filter.IsFeatured.Value);
        }

        var dtoQuery = query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .AsEnumerable()
            .Select(p => _mapper.Map<ProductSummaryDto>(p))
            .AsQueryable();

        var result = PagingHelper.QueryPaginate(request.Filter, dtoQuery);
        return Task.FromResult(result);
    }
}
