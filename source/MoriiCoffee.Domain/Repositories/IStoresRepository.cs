using MoriiCoffee.Domain.Aggregates.StoreAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository interface for managing <see cref="Store"/> aggregate instances.
/// Inherits standard CRUD and pagination operations from <see cref="IRepositoryBase{T}"/>.
/// </summary>
public interface IStoresRepository : IRepositoryBase<Store>
{
    /// <summary>
    /// Checks whether a store with the given slug already exists.
    /// Pass <paramref name="excludeId"/> to exclude the current store during update validation.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a store with the given name already exists.
    /// Pass <paramref name="excludeId"/> to exclude the current store during update validation.
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
