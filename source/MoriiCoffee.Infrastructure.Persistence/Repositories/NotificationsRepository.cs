using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.NotificationAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>EF Core repository for the Notification aggregate with user-scoped bulk operations.</summary>
public class NotificationsRepository : RepositoryBase<Notification>, INotificationsRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationsRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
    }

    /// <inheritdoc/>
    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var now = DateTime.UtcNow;

        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now)
                .SetProperty(n => n.UpdatedAt, now));
    }

    /// <inheritdoc/>
    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        var now = DateTime.UtcNow;

        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsDeleted, true)
                .SetProperty(n => n.DeletedAt, now));
    }
}
