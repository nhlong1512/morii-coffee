using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ShipmentWebhookEventConfiguration : IEntityTypeConfiguration<ShipmentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentWebhookEvent> builder)
    {
        builder.HasIndex(x => x.ProviderEventId);
        builder.HasIndex(x => x.ProviderOrderCode);
        builder.HasIndex(x => x.ClientOrderCode);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.ReceivedAt);
    }
}
