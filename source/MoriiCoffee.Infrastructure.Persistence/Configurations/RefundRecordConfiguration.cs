using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.UserAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="RefundRecord"/> child entity.</summary>
public class RefundRecordConfiguration : IEntityTypeConfiguration<RefundRecord>
{
    public void Configure(EntityTypeBuilder<RefundRecord> builder)
    {
        // UNIQUE: Stripe refund id is the natural key webhook handler uses to find a refund row.
        builder.HasIndex(r => r.StripeRefundId).IsUnique();

        // FK to AspNetUsers — RESTRICT so we never lose audit trail by deleting an admin user.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.InitiatedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
