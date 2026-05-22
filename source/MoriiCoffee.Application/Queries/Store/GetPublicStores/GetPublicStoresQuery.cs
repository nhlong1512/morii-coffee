using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Store.GetPublicStores;

/// <summary>Query for the public store locator. Returns only active, non-deleted stores.</summary>
public class GetPublicStoresQuery : IQuery<Pagination<StoreDto>>
{
    public GetPublicStoresQuery(
        PaginationFilter filter,
        double? latitude,
        double? longitude,
        double? radius,
        string? city,
        string? search)
    {
        Filter = filter;
        Latitude = latitude;
        Longitude = longitude;
        Radius = radius;
        City = city;
        Search = search;
    }

    /// <summary>Pagination filter (page, size, ordering).</summary>
    public PaginationFilter Filter { get; }

    /// <summary>Optional user latitude for geolocation-based distance calculation and sorting.</summary>
    public double? Latitude { get; }

    /// <summary>Optional user longitude for geolocation-based distance calculation and sorting.</summary>
    public double? Longitude { get; }

    /// <summary>Optional radius in kilometers to filter stores by proximity.</summary>
    public double? Radius { get; }

    /// <summary>Optional filter to return only stores in the specified city.</summary>
    public string? City { get; }

    /// <summary>Optional search term matched against store name and address.</summary>
    public string? Search { get; }
}
