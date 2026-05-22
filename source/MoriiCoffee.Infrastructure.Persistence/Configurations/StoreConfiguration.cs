using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.StoreAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>EF Core entity type configuration for the Store aggregate.</summary>
public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.DisplayOrder);

        builder.HasMany(x => x.OpeningHours)
            .WithOne(x => x.Store)
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
