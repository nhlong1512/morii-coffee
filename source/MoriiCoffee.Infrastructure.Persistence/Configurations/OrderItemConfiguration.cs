using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(oi => oi.LineTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(oi => oi.OrderId);

        builder.HasIndex(oi => oi.ProductId);
    }
}
