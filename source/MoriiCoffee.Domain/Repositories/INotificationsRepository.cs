using MoriiCoffee.Domain.Aggregates.NotificationAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="Notification"/> entities.
/// Provides user-scoped bulk operations beyond the standard <see cref="IRepositoryBase{T}"/> contract.
/// </summary>
public interface INotificationsRepository : IRepositoryBase<Notification>
{
    /// <summary>Returns the number of unread notifications for the specified user.</summary>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>Marks all unread notifications for the user as read in a single batch update.</summary>
    Task MarkAllAsReadAsync(Guid userId);

    /// <summary>Soft-deletes all notifications belonging to the specified user.</summary>
    Task DeleteAllByUserIdAsync(Guid userId);
}
