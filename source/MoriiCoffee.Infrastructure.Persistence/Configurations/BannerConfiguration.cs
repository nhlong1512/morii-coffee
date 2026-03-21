using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Banner"/> aggregate.
/// Column shape and constraints are defined via DataAnnotations on the entity.
/// This class exists to document that Banner has no navigational relationships.
/// </summary>
public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
    }
}
