using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.WishlistAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="WishlistItem"/>.
/// Enforces the (UserId, ProductId) uniqueness constraint and adds a UserId index
/// for efficient per-user queries.
/// </summary>
public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasIndex(w => new { w.UserId, w.ProductId })
            .IsUnique()
            .HasDatabaseName("UQ_WishlistItems_UserId_ProductId");

        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("IX_WishlistItems_UserId");

        builder.HasIndex(w => w.AddedAt)
            .IsDescending()
            .HasDatabaseName("IX_WishlistItems_AddedAt");
    }
}
