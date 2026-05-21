using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="BlogPostCategory"/> join entity.
/// </summary>
public class BlogPostCategoryConfiguration : IEntityTypeConfiguration<BlogPostCategory>
{
    public void Configure(EntityTypeBuilder<BlogPostCategory> builder)
    {
        builder.HasIndex(x => new { x.BlogPostId, x.BlogCategoryId }).IsUnique();
    }
}
