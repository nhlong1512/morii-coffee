using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(v => v.AdditionalPrice)
            .HasPrecision(18, 2);
    }
}
