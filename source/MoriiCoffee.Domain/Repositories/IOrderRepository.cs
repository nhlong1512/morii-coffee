using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="Order"/> aggregates.
/// Extends the generic base with Order-specific queries that cannot be expressed
/// cleanly through the generic <see cref="IRepositoryBase{T}"/> API.
/// </summary>
public interface IOrderRepository : IRepositoryBase<Order>
{
    /// <summary>
    /// Retrieves an order by its primary key with its <see cref="Order.Items"/> eagerly loaded.
    /// Returns <c>null</c> if the order does not exist or has been soft-deleted.
    /// </summary>
    Task<Order?> GetByIdWithItemsAsync(Guid id);

    /// <summary>
    /// Retrieves an order by its human-readable display number (e.g., <c>MRC-20260430-001</c>).
    /// Returns <c>null</c> if no matching, non-deleted order is found.
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// Returns the count of non-deleted orders whose <see cref="Order.OrderNumber"/>
    /// starts with <paramref name="prefix"/>. Used by the order-number generator to
    /// determine the daily sequence number (e.g., prefix <c>MRC-20260430-</c>).
    /// </summary>
    Task<int> CountByOrderNumberPrefixAsync(string prefix);
}
