using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.Property(x => x.CodAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.FeeTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasIndex(x => x.ClientOrderCode)
            .IsUnique();

        builder.HasIndex(x => x.ProviderOrderCode);

        builder.HasIndex(x => x.Status);
    }
}
