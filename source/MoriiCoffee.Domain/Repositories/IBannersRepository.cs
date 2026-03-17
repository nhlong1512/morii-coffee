using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="Banner"/> entities.
/// Inherits all base CRUD, pagination, and soft-delete operations from <see cref="IRepositoryBase{T}"/>.
/// </summary>
public interface IBannersRepository : IRepositoryBase<Banner>
{
    /// <summary>
    /// Checks whether a banner with the given <paramref name="displayOrder"/> already exists,
    /// optionally excluding a specific banner (used during update to allow same-value saves).
    /// </summary>
    Task<bool> DisplayOrderExistsAsync(int displayOrder, Guid? excludeId = null);
}
