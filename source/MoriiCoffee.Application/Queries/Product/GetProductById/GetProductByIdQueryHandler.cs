using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Product.GetProductById;

/// <summary>
/// Returns the full detail view for a single product.
/// Attempts a Redis read-through cache first; falls back to the primary database on
/// cache miss or Redis failure.
/// </summary>
public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProductCatalogCache _catalogCache;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IProductCatalogCache catalogCache,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _catalogCache = catalogCache;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // Try cache first
        var cached = await _catalogCache.GetDetailAsync(request.ProductId);
        if (cached is not null)
            return cached;

        // Cache miss — query the database
        var product = await _unitOfWork.Products
            .FindByCondition(p => p.Id == request.ProductId)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Include(p => p.Images)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        var dto = _mapper.Map<ProductDto>(product);

        // Compute total price for each variant
        foreach (var variant in dto.Variants)
            variant.TotalPrice = product.BasePrice + variant.AdditionalPrice;

        // Populate cache
        await _catalogCache.SetDetailAsync(product.Id, dto);

        return dto;
    }
}
