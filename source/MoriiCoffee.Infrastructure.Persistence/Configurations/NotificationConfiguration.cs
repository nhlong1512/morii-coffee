using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.NotificationAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Notification aggregate.
/// FK to Users is set with no cascade delete — the User aggregate uses soft-delete,
/// so physical deletion never occurs.
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");

        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");

        builder.HasOne<Domain.Aggregates.UserAggregate.User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
