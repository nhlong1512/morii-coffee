using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="Category"/> entities.
/// Inherits all base CRUD, pagination, and soft-delete operations.
/// </summary>
public interface ICategoriesRepository : IRepositoryBase<Category>
{
    /// <summary>Retrieves a category by its unique name (case-insensitive).</summary>
    Task<Category?> GetByNameAsync(string name);
}
