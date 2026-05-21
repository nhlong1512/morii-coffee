using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for blog posts.
/// </summary>
public class BlogPostsRepository : RepositoryBase<BlogPost>, IBlogPostsRepository
{
    private readonly ApplicationDbContext _context;

    public BlogPostsRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug)
    {
        return await _context.Set<BlogPost>()
            .Include(x => x.BlogPostCategories)
                .ThenInclude(x => x.BlogCategory)
            .Where(x => !x.IsDeleted)
            .FirstOrDefaultAsync(x => x.Slug == slug.ToLowerInvariant());
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
    {
        var query = _context.Set<BlogPost>()
            .Where(x => !x.IsDeleted && x.Slug == slug.ToLowerInvariant());

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }
}
