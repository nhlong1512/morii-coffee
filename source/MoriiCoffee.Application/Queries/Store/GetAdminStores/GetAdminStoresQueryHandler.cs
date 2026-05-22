using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Store.GetAdminStores;

/// <summary>
/// Handles <see cref="GetAdminStoresQuery"/> returning a paginated list of stores for admin.
/// Includes inactive stores; soft-deleted stores are excluded by RepositoryBase.FindAll().
/// </summary>
public class GetAdminStoresQueryHandler : IQueryHandler<GetAdminStoresQuery, Pagination<StoreDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAdminStoresQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Pagination<StoreDto>> Handle(GetAdminStoresQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Stores
            .FindAll(trackChanges: false)
            .Include(s => s.OpeningHours)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLower();
            query = query.Where(s => s.City.ToLower().Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(search) ||
                s.Address.ToLower().Contains(search));
        }

        var stores = await query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var dtoQuery = stores
            .Select(s => _mapper.Map<StoreDto>(s))
            .AsQueryable();

        return await Task.FromResult(PagingHelper.QueryPaginate(request.Filter, dtoQuery));
    }
}
