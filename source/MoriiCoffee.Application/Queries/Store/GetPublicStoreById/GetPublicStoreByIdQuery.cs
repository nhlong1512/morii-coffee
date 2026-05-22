using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Store.GetPublicStoreById;

/// <summary>Query to retrieve a single active store by ID for public display.</summary>
public class GetPublicStoreByIdQuery : IQuery<StoreDto>
{
    public GetPublicStoreByIdQuery(Guid id) => Id = id;

    /// <summary>The ID of the store to retrieve.</summary>
    public Guid Id { get; }
}
