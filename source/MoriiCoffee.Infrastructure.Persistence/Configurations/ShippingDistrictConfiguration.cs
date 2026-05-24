using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ShippingDistrictConfiguration : IEntityTypeConfiguration<ShippingDistrict>
{
    public void Configure(EntityTypeBuilder<ShippingDistrict> builder)
    {
        builder.HasIndex(x => x.ProvinceId);
        builder.HasIndex(x => x.DistrictName);
    }
}
