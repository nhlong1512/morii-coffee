using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="Banner"/> entities.
/// Inherits all base CRUD, pagination, and soft-delete operations from <see cref="IRepositoryBase{T}"/>.
/// </summary>
public interface IBannersRepository : IRepositoryBase<Banner>
{
    /// <summary>Returns all non-deleted banners ordered by <see cref="Banner.DisplayOrder"/> ascending.</summary>
    Task<IEnumerable<Banner>> GetAllOrderedAsync();
}
