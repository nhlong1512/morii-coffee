using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Queries.Product.GetPaginatedProducts;

/// <summary>
/// Returns a paginated, filtered product list.
/// Attempts a Redis read-through cache first; falls back to the primary database transparently on
/// cache miss or Redis failure.
/// </summary>
public class GetPaginatedProductsQueryHandler : IQueryHandler<GetPaginatedProductsQuery, Pagination<ProductSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProductCatalogCache _catalogCache;
    private readonly ILogger<GetPaginatedProductsQueryHandler> _logger;

    public GetPaginatedProductsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IProductCatalogCache catalogCache,
        ILogger<GetPaginatedProductsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _catalogCache = catalogCache;
        _logger = logger;
    }

    public async Task<Pagination<ProductSummaryDto>> Handle(
        GetPaginatedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CatalogCacheKeyHelper.BuildListKey(request.Filter);

        // Try cache first
        var cached = await _catalogCache.GetListAsync(cacheKey);
        if (cached is not null)
            return cached;

        // Cache miss — query the database
        IQueryable<ProductEntity> query = _unitOfWork.Products
            .FindAll(false)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category);

        if (request.Filter.CategoryId.HasValue)
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == request.Filter.CategoryId.Value));

        if (request.Filter.IsFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == request.Filter.IsFeatured.Value);

        var dtoQuery = query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .AsEnumerable()
            .Select(p => _mapper.Map<ProductSummaryDto>(p))
            .AsQueryable();

        var result = PagingHelper.QueryPaginate(request.Filter, dtoQuery);

        // Populate cache (fire-and-forget on error — cache failures never fail the request)
        await _catalogCache.SetListAsync(cacheKey, result);

        return result;
    }
}
