using MoriiCoffee.Domain.Aggregates.BlogPostAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="BlogPost"/> entities.
/// </summary>
public interface IBlogPostsRepository : IRepositoryBase<BlogPost>
{
    /// <summary>Retrieves a blog post by its unique slug.</summary>
    Task<BlogPost?> GetBySlugAsync(string slug);

    /// <summary>Checks whether a slug is already in use by another blog post.</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
}
