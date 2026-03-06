using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.AltText)
            .HasMaxLength(200);

        builder.Property(i => i.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(i => i.IsMain)
            .HasDefaultValue(false);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.HasIndex(i => i.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");

        // Soft-delete global filter
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
