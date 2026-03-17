using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>EF Core repository for the Banner aggregate.</summary>
public class BannersRepository : RepositoryBase<Banner>, IBannersRepository
{
    private readonly ApplicationDbContext _context;

    public BannersRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<bool> DisplayOrderExistsAsync(int displayOrder, Guid? excludeId = null)
    {
        var query = _context.Banners
            .Where(b => !b.IsDeleted && b.DisplayOrder == displayOrder);

        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);

        return await query.AnyAsync();
    }
}
