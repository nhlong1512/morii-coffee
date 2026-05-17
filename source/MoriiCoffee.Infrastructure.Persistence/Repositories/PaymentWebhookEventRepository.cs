using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using Npgsql;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core / PostgreSQL implementation of <see cref="IPaymentWebhookEventRepository"/>. Translates
/// the Npgsql unique-constraint-violation (SQLSTATE 23505) on <c>StripeEventId</c> into the
/// idempotent "duplicate event" path required by FR-008.
/// </summary>
public class PaymentWebhookEventRepository : IPaymentWebhookEventRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentWebhookEventRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<bool> TryInsertAsync(PaymentWebhookEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        // Optimistic insert: try once. If a duplicate row already exists at the DB, EF will surface
        // a DbUpdateException with an inner PostgresException whose SqlState == 23505 (unique violation).
        await _context.PaymentWebhookEvents.AddAsync(evt);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolationOn(ex, nameof(PaymentWebhookEvent.StripeEventId)))
        {
            // The row was already inserted by a prior delivery — detach the locally-added entity
            // so subsequent SaveChanges calls (in the same UoW transaction) don't retry it.
            _context.Entry(evt).State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(PaymentWebhookEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        _context.Entry(evt).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<PaymentWebhookEvent?> GetByEventIdAsync(string stripeEventId)
    {
        return await _context.PaymentWebhookEvents
            .Where(e => e.StripeEventId == stripeEventId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns true if the given <see cref="DbUpdateException"/> chain contains a PostgresException
    /// with SqlState 23505 (unique violation) referencing a column whose name contains
    /// <paramref name="columnName"/>. The column-name match is best-effort because Npgsql's
    /// constraint name is the index name, not the column name; we look at both messages.
    /// </summary>
    private static bool IsUniqueViolationOn(DbUpdateException ex, string columnName)
    {
        if (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Defensive: check that the column-or-constraint-name string contains our column.
            var hint = (pg.ConstraintName ?? pg.MessageText ?? string.Empty);
            return hint.Contains(columnName, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
