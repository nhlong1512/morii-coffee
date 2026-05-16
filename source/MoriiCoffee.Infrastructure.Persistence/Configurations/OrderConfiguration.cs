using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(o => o.Tax)
            .HasPrecision(18, 2);

        builder.Property(o => o.Shipping)
            .HasPrecision(18, 2);

        builder.Property(o => o.Discount)
            .HasPrecision(18, 2);

        builder.Property(o => o.Total)
            .HasPrecision(18, 2);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.HasIndex(o => o.UserId);

        builder.HasIndex(o => o.OrderStatus);

        builder.HasIndex(o => o.CreatedAt)
            .IsDescending();

        builder.OwnsOne(o => o.DeliveryInfo);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
