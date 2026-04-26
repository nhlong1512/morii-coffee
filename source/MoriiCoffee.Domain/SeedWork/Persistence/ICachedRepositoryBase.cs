using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.SeedWork.Persistence;

/// <summary>
/// Marker interface for repositories that add caching behavior on top of <see cref="IRepositoryBase{T}"/>.
/// </summary>
public interface ICachedRepositoryBase<T> : IRepositoryBase<T> where T : EntityBase
{
}
