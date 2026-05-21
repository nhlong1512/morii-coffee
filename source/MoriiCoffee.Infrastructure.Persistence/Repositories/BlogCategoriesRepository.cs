using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for blog categories.
/// </summary>
public class BlogCategoriesRepository : RepositoryBase<BlogCategory>, IBlogCategoriesRepository
{
    private readonly ApplicationDbContext _context;

    public BlogCategoriesRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<BlogCategory?> GetBySlugAsync(string slug)
    {
        return await _context.Set<BlogCategory>()
            .Include(x => x.BlogPostCategories)
            .Where(x => !x.IsDeleted)
            .FirstOrDefaultAsync(x => x.Slug == slug.ToLowerInvariant());
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
    {
        var query = _context.Set<BlogCategory>()
            .Where(x => !x.IsDeleted && x.Slug == slug.ToLowerInvariant());

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public Task<int> CountPostsUsingCategoryAsync(Guid categoryId)
    {
        return _context.Set<BlogCategory>()
            .Where(x => !x.IsDeleted && x.Id == categoryId)
            .SelectMany(x => x.BlogPostCategories)
            .Where(x => !x.IsDeleted && !x.BlogPost.IsDeleted)
            .CountAsync();
    }
}
