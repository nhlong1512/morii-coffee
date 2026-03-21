using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="ProductImage"/> entities.
/// Inherits all base CRUD and soft-delete operations from <see cref="IRepositoryBase{T}"/>.
/// </summary>
public interface IProductImagesRepository : IRepositoryBase<ProductImage>
{
    /// <summary>Returns all non-deleted images for a product, ordered by <see cref="ProductImage.DisplayOrder"/>.</summary>
    Task<IEnumerable<ProductImage>> GetByProductIdAsync(Guid productId);

    /// <summary>Returns the count of non-deleted images for a product.</summary>
    Task<int> CountByProductIdAsync(Guid productId);
}
