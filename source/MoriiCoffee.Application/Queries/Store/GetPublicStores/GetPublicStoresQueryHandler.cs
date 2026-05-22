using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Store.GetPublicStores;

/// <summary>
/// Handles <see cref="GetPublicStoresQuery"/>.
/// When geolocation params are provided, computes Haversine distance in memory and sorts ascending.
/// Otherwise sorts by DisplayOrder ascending.
/// </summary>
public class GetPublicStoresQueryHandler : IQueryHandler<GetPublicStoresQuery, Pagination<StoreDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublicStoresQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Pagination<StoreDto>> Handle(GetPublicStoresQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Stores
            .FindAll(trackChanges: false)
            .Where(s => s.IsActive)
            .Include(s => s.OpeningHours)
            .AsQueryable();

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

        var stores = await query.ToListAsync(cancellationToken);
        var dtos = stores.Select(s => _mapper.Map<StoreDto>(s)).ToList();

        bool hasGeo = request.Latitude.HasValue && request.Longitude.HasValue;
        if (hasGeo)
        {
            foreach (var dto in dtos)
            {
                dto.DistanceKm = HaversineKm(
                    request.Latitude!.Value, request.Longitude!.Value,
                    dto.Latitude, dto.Longitude);
            }

            if (request.Radius.HasValue)
                dtos = dtos.Where(d => d.DistanceKm <= request.Radius.Value).ToList();

            dtos = dtos.OrderBy(d => d.DistanceKm).ThenBy(d => d.DisplayOrder).ToList();
        }
        else
        {
            dtos = dtos.OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name).ToList();
        }

        // Manual pagination
        int totalCount = dtos.Count;
        var filter = request.Filter;
        List<StoreDto> items;
        if (filter.TakeAll)
            items = dtos;
        else
            items = dtos.Skip((filter.Page - 1) * filter.Size).Take(filter.Size).ToList();

        return new Pagination<StoreDto>
        {
            Items = items,
            Metadata = new Metadata(totalCount, filter.Page, filter.Size, filter.TakeAll)
        };
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
