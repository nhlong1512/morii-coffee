using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="Payment"/> aggregate.</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // FK to Order — RESTRICT so an order with attempted payments cannot be hard-deleted
        // accidentally. Soft-delete on Order remains independent.
        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // UNIQUE: webhook handler uses Stripe session id as the natural key.
        builder.HasIndex(p => p.StripeSessionId).IsUnique();

        // Non-unique: list payments per order.
        builder.HasIndex(p => p.OrderId);

        // Non-unique: refund lookups by PI id (nullable column, but EF understands).
        builder.HasIndex(p => p.StripePaymentIntentId);

        // Children: encapsulated through the aggregate root only (field-backed collection).
        builder.HasMany(p => p.Refunds)
            .WithOne()
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Refunds)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
