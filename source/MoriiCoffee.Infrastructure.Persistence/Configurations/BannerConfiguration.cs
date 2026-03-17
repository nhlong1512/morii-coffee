using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the Banner aggregate. No navigation properties — standalone table.</summary>
public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.HasIndex(b => b.DisplayOrder)
            .HasDatabaseName("IX_Banners_DisplayOrder");
    }
}
