using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>EF Core repository for the Payment aggregate.</summary>
public class PaymentsRepository : RepositoryBase<Payment>, IPaymentsRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentsRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId)
    {
        return await _context.Payments
            .Where(p => !p.IsDeleted && p.StripePaymentIntentId == paymentIntentId)
            .FirstOrDefaultAsync();
    }
}
