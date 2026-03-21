using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for <see cref="Banner"/> entities.
/// Provides banner-specific queries on top of the generic <see cref="RepositoryBase{T}"/>.
/// </summary>
public class BannersRepository : RepositoryBase<Banner>, IBannersRepository
{
    private readonly ApplicationDbContext _context;

    public BannersRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Banner>> GetAllOrderedAsync()
    {
        return await _context.Banners
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();
    }
}
