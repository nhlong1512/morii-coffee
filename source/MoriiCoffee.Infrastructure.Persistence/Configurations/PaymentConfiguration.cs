using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the Payment aggregate.</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasIndex(p => p.StripePaymentIntentId)
            .IsUnique()
            .HasDatabaseName("IX_Payments_StripePaymentIntentId");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Payments_UserId");

        builder.HasOne<Domain.Aggregates.UserAggregate.User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
