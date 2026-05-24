using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ShippingProvinceConfiguration : IEntityTypeConfiguration<ShippingProvince>
{
    public void Configure(EntityTypeBuilder<ShippingProvince> builder)
    {
        builder.HasIndex(x => x.ProvinceName);
    }
}
