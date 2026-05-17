using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="PaymentWebhookEvent"/> audit/idempotency rows.
/// Not derived from <see cref="MoriiCoffee.Domain.SeedWork.Persistence.IRepositoryBase{T}"/>
/// because the entity is not navigated as part of an aggregate root — its lookups are flat and
/// the idempotency-by-UNIQUE-constraint pattern needs a bespoke insert helper.
/// </summary>
public interface IPaymentWebhookEventRepository
{
    /// <summary>
    /// Attempts to insert <paramref name="evt"/>. Returns <c>true</c> when the insert succeeded
    /// (first delivery of this <see cref="PaymentWebhookEvent.StripeEventId"/>) and
    /// <c>false</c> when the UNIQUE constraint on <c>StripeEventId</c> rejected the insert,
    /// meaning the event has already been processed (duplicate per FR-008 idempotency).
    /// </summary>
    /// <remarks>
    /// On <c>true</c> the row is queued in the change tracker; the caller is responsible for
    /// committing the unit of work. On <c>false</c> no row is added and no exception is thrown.
    /// </remarks>
    Task<bool> TryInsertAsync(PaymentWebhookEvent evt);

    /// <summary>Marks an existing audit row as updated (e.g. when MarkProcessed has been called).</summary>
    Task UpdateAsync(PaymentWebhookEvent evt);

    /// <summary>Look up an audit row by its Stripe event id. Returns <c>null</c> if not present.</summary>
    Task<PaymentWebhookEvent?> GetByEventIdAsync(string stripeEventId);
}
