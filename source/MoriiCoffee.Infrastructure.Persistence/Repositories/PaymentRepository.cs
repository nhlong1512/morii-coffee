using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPaymentRepository"/>.</summary>
public class PaymentRepository : RepositoryBase<Payment>, IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Payment?> GetBySessionIdAsync(string stripeSessionId)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => !p.IsDeleted && p.StripeSessionId == stripeSessionId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Payment?> GetByPaymentIntentIdAsync(string stripePaymentIntentId)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => !p.IsDeleted && p.StripePaymentIntentId == stripePaymentIntentId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Payment?> GetLatestPendingByOrderIdAsync(Guid orderId)
    {
        return await _context.Payments
            .Where(p => !p.IsDeleted &&
                        p.OrderId == orderId &&
                        p.Status == Domain.Shared.Enums.Order.EPaymentTransactionStatus.Created)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Payment?> GetLatestSucceededByOrderIdAsync(Guid orderId)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => !p.IsDeleted &&
                        p.OrderId == orderId &&
                        p.Status == Domain.Shared.Enums.Order.EPaymentTransactionStatus.Succeeded)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payment>> ListByOrderIdAsync(Guid orderId)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => !p.IsDeleted && p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
