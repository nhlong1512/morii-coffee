using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="Product"/> entities.
/// Inherits all base CRUD, pagination, and soft-delete operations.
/// </summary>
public interface IProductsRepository : IRepositoryBase<Product>
{
    /// <summary>Retrieves a product by its URL slug.</summary>
    Task<Product?> GetBySlugAsync(string slug);

    /// <summary>Checks whether a slug is already in use (for uniqueness validation).</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
}
