using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Store.GetAdminStores;

/// <summary>Query for the admin store list. Includes inactive stores; excludes soft-deleted.</summary>
public class GetAdminStoresQuery : IQuery<Pagination<StoreDto>>
{
    public GetAdminStoresQuery(PaginationFilter filter, bool? isActive, string? city, string? search)
    {
        Filter = filter;
        IsActive = isActive;
        City = city;
        Search = search;
    }

    /// <summary>Pagination filter (page, size, ordering).</summary>
    public PaginationFilter Filter { get; }

    /// <summary>Optional filter by active/inactive status.</summary>
    public bool? IsActive { get; }

    /// <summary>Optional filter to return only stores in the specified city.</summary>
    public string? City { get; }

    /// <summary>Optional search term matched against store name and address.</summary>
    public string? Search { get; }
}
