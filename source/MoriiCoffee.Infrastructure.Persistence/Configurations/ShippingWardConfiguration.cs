using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ShippingWardConfiguration : IEntityTypeConfiguration<ShippingWard>
{
    public void Configure(EntityTypeBuilder<ShippingWard> builder)
    {
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.WardName);
    }
}
