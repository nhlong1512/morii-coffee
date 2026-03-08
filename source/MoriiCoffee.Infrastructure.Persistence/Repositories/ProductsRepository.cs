using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

public class ProductsRepository : RepositoryBase<Product>, IProductsRepository
{
    private readonly ApplicationDbContext _context;

    public ProductsRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Product?> GetBySlugAsync(string slug)
    {
        return await _context.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .Where(p => !p.IsDeleted)
                .FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant());
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
    {
        var query = _context.Products
            .Where(p => !p.IsDeleted && p.Slug == slug.ToLowerInvariant());

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
