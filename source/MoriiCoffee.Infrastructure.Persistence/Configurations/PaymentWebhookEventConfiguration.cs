using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="PaymentWebhookEvent"/> audit/idempotency entity.</summary>
public class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        // UNIQUE: the primary idempotency gate. A duplicate-insert race is rejected at the DB
        // and the application interprets the violation as EPaymentWebhookProcessingResult.Duplicate.
        builder.HasIndex(e => new { e.Provider, e.StripeEventId }).IsUnique();

        // Descending index for "what happened most recently" diagnostic queries.
        builder.HasIndex(e => e.ReceivedAt).IsDescending();

        // Optional soft link to the Payment row — SetNull because the audit must outlive the
        // payment for forensic purposes (an incident might involve a since-purged payment).
        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(e => e.RelatedPaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
