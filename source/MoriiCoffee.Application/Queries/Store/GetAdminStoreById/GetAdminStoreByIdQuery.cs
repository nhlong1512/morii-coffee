using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Store.GetAdminStoreById;

/// <summary>Query to retrieve a single store by ID for admin view (includes inactive).</summary>
public class GetAdminStoreByIdQuery : IQuery<StoreDto>
{
    public GetAdminStoreByIdQuery(Guid id) => Id = id;

    /// <summary>The ID of the store to retrieve.</summary>
    public Guid Id { get; }
}
