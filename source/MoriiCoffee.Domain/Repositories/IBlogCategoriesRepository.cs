using MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="BlogCategory"/> entities.
/// </summary>
public interface IBlogCategoriesRepository : IRepositoryBase<BlogCategory>
{
    /// <summary>Retrieves a category by its unique slug.</summary>
    Task<BlogCategory?> GetBySlugAsync(string slug);

    /// <summary>Checks whether a slug is already in use by another category.</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);

    /// <summary>Counts non-deleted blog posts currently linked to the category.</summary>
    Task<int> CountPostsUsingCategoryAsync(Guid categoryId);
}
