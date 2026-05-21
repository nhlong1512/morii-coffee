using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>.
/// Handles Order-specific queries; general CRUD operations are inherited from
/// <see cref="RepositoryBase{T}"/>.
/// </summary>
public class OrdersRepository : RepositoryBase<Order>, IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrdersRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => !o.IsDeleted && o.Id == id)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Where(o => !o.IsDeleted && o.OrderNumber == orderNumber)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<int> CountByOrderNumberPrefixAsync(string prefix)
    {
        return await _context.Orders
            .CountAsync(o => !o.IsDeleted && o.OrderNumber.StartsWith(prefix));
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, int>> GetSoldQuantitiesByProductIdsAsync(IEnumerable<Guid> productIds)
    {
        var ids = productIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, int>();

        return await _context.Orders
            .Where(o => !o.IsDeleted &&
                        o.OrderStatus != EOrderStatus.PENDING &&
                        o.OrderStatus != EOrderStatus.CANCELLED)
            .Join(
                _context.Set<OrderItem>(),
                o => o.Id,
                oi => oi.OrderId,
                (o, oi) => new { OrderItem = oi })
            .Where(x => ids.Contains(x.OrderItem.ProductId))
            .GroupBy(x => x.OrderItem.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Sum(x => x.OrderItem.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count);
    }
}
