using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
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
    public async Task<Payment?> GetBySessionIdAsync(string stripeSessionId, Domain.Shared.Enums.Order.EPaymentProvider provider = Domain.Shared.Enums.Order.EPaymentProvider.Stripe)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => !p.IsDeleted && p.Provider == provider && p.StripeSessionId == stripeSessionId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Payment?> GetByPaymentIntentIdAsync(string stripePaymentIntentId, Domain.Shared.Enums.Order.EPaymentProvider provider = Domain.Shared.Enums.Order.EPaymentProvider.Stripe)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => !p.IsDeleted && p.Provider == provider && p.StripePaymentIntentId == stripePaymentIntentId)
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

    /// <inheritdoc />
    public async Task CreateRefundAsync(RefundRecord refund)
    {
        await _context.Refunds.AddAsync(refund);
    }
}
