using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="BlogCategory"/> aggregate.
/// </summary>
public class BlogCategoryConfiguration : IEntityTypeConfiguration<BlogCategory>
{
    public void Configure(EntityTypeBuilder<BlogCategory> builder)
    {
        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasMany(x => x.BlogPostCategories)
            .WithOne(x => x.BlogCategory)
            .HasForeignKey(x => x.BlogCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
