using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

public class ProductVariantsRepository : RepositoryBase<ProductVariant>, IProductVariantsRepository
{
    private readonly ApplicationDbContext _context;

    public ProductVariantsRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(Guid productId)
    {
        return await _context.ProductVariants
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .ToListAsync();
    }

    public async Task ClearDefaultFlagAsync(Guid productId, Guid? excludeVariantId = null)
    {
        var query = _context.ProductVariants
            .Where(v => v.ProductId == productId && v.IsDefault && !v.IsDeleted);

        if (excludeVariantId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVariantId.Value);
        }

        var variants = await query.ToListAsync();
        foreach (var variant in variants)
        {
            variant.IsDefault = false;
        }
    }
}
