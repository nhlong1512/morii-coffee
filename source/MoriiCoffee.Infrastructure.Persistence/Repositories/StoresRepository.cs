using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.StoreAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>EF Core repository implementation for the Store aggregate.</summary>
public class StoresRepository : RepositoryBase<Store>, IStoresRepository
{
    private readonly ApplicationDbContext _context;

    public StoresRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var query = _context.Stores.Where(s => !s.IsDeleted && s.Slug == normalizedSlug);
        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var query = _context.Stores.Where(s => !s.IsDeleted && s.Name.ToLower() == normalizedName);
        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }
}
