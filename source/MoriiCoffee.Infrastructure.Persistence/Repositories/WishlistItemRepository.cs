using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.WishlistAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWishlistItemRepository"/>.
/// All mutations are staged in the DbContext — callers must invoke
/// <c>IUnitOfWork.CommitAsync()</c> to persist.
/// </summary>
public class WishlistItemRepository : IWishlistItemRepository
{
    private readonly ApplicationDbContext _context;

    public WishlistItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WishlistItem>> GetByUserIdAsync(Guid userId)
    {
        return await _context.WishlistItems
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();
    }

    public async Task AddAsync(WishlistItem item)
    {
        await _context.WishlistItems.AddAsync(item);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid productId)
    {
        return await _context.WishlistItems
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid productId)
    {
        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        if (item is null)
            return false;

        _context.WishlistItems.Remove(item);
        return true;
    }

    public async Task ClearAsync(Guid userId)
    {
        var items = await _context.WishlistItems
            .Where(w => w.UserId == userId)
            .ToListAsync();

        _context.WishlistItems.RemoveRange(items);
    }
}
