using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>EF Core repository for <see cref="ProductImage"/> entities.</summary>
public class ProductImagesRepository : RepositoryBase<ProductImage>, IProductImagesRepository
{
    private readonly ApplicationDbContext _context;

    public ProductImagesRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(Guid productId)
    {
        return await _context.ProductImages
            .Where(i => i.ProductId == productId && !i.IsDeleted)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task ClearThumbnailFlagAsync(Guid productId, Guid? excludeImageId = null)
    {
        var images = await _context.ProductImages
            .Where(i => i.ProductId == productId && !i.IsDeleted && i.IsThumbnail)
            .ToListAsync();

        foreach (var image in images)
        {
            if (excludeImageId.HasValue && image.Id == excludeImageId.Value)
                continue;

            image.IsThumbnail = false;
            _context.Entry(image).State = EntityState.Modified;
        }
    }

    /// <inheritdoc/>
    public async Task<int> CountByProductIdAsync(Guid productId)
    {
        return await _context.ProductImages
            .CountAsync(i => i.ProductId == productId && !i.IsDeleted);
    }
}
