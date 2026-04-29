using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class UserDeliveryProfileConfiguration : IEntityTypeConfiguration<UserDeliveryProfile>
{
    public void Configure(EntityTypeBuilder<UserDeliveryProfile> builder)
    {
        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserDeliveryProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
