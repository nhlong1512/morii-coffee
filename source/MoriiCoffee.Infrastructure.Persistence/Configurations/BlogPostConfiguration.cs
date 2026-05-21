using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="BlogPost"/> aggregate.
/// </summary>
public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.DisplayOrder);
        builder.HasIndex(x => x.IsFeatured);

        builder.Property(x => x.ContentHtml).HasColumnType("text");
        builder.Property(x => x.ContentJson).HasColumnType("text");

        builder.HasMany(x => x.BlogPostCategories)
            .WithOne(x => x.BlogPost)
            .HasForeignKey(x => x.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
