using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

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

    public async Task<Pagination<ProductSummaryDto>> Handle(GetPaginatedProductsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductEntity> query = _unitOfWork.Products
            .FindAll(false)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category);

        // Apply optional filters
        if (request.Filter.CategoryIds is { Count: > 0 })
        {
            query = query.Where(p => p.ProductCategories.Any(pc => request.Filter.CategoryIds.Contains(pc.CategoryId)));
        }

        if (request.Filter.IsFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == request.Filter.IsFeatured.Value);
        }

        var productList = await query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var productIds = productList.Select(p => p.Id).ToList();
        var soldCounts = await _unitOfWork.Orders.GetSoldQuantitiesByProductIdsAsync(productIds);

        var dtoQuery = productList
            .Select(p =>
            {
                var dto = _mapper.Map<ProductSummaryDto>(p);
                dto.QuantitySold = soldCounts.GetValueOrDefault(p.Id, 0);
                return dto;
            })
            .AsQueryable();

        var result = PagingHelper.QueryPaginate(request.Filter, dtoQuery);
        return result;
    }
}
