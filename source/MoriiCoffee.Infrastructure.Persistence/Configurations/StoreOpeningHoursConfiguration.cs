using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>EF Core entity type configuration for the StoreOpeningHours child entity.</summary>
public class StoreOpeningHoursConfiguration : IEntityTypeConfiguration<StoreOpeningHours>
{
    public void Configure(EntityTypeBuilder<StoreOpeningHours> builder)
    {
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => new { x.StoreId, x.DayOfWeek }).IsUnique();
        builder.Property(x => x.OpenTime).HasMaxLength(5);
        builder.Property(x => x.CloseTime).HasMaxLength(5);
    }
}
