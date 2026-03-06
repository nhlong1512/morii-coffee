using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.AdditionalPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(v => v.Sku)
            .HasMaxLength(50);

        builder.Property(v => v.StockQuantity)
            .HasDefaultValue(-1);

        builder.Property(v => v.IsDefault)
            .HasDefaultValue(false);

        builder.Property(v => v.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(v => v.CreatedAt)
            .IsRequired();

        // Index for fast lookups by product
        builder.HasIndex(v => v.ProductId)
            .HasDatabaseName("IX_ProductVariants_ProductId");

        // Soft-delete global filter
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
