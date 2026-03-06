using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="ProductVariant"/> entities.
/// Inherits all base CRUD, pagination, and soft-delete operations.
/// </summary>
public interface IProductVariantsRepository : IRepositoryBase<ProductVariant>
{
    /// <summary>Returns all non-deleted variants belonging to a specific product.</summary>
    Task<IEnumerable<ProductVariant>> GetByProductIdAsync(Guid productId);

    /// <summary>Ensures that only one variant per product is marked as the default.</summary>
    Task ClearDefaultFlagAsync(Guid productId, Guid? excludeVariantId = null);
}
