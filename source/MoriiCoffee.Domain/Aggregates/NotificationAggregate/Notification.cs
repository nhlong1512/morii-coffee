using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Notification;

namespace MoriiCoffee.Domain.Aggregates.NotificationAggregate;

/// <summary>
/// Represents an in-app notification delivered to a specific user.
/// Notifications are user-scoped and support read-tracking and soft-delete.
/// Real-time delivery is handled via SignalR; this entity persists the notification record.
/// </summary>
[Table("Notifications")]
public class Notification : AggregateRoot
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>ID of the user this notification belongs to.</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Short heading of the notification (e.g., "Order Confirmed").</summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string Title { get; set; } = null!;

    /// <summary>Full body text of the notification.</summary>
    [Required]
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string Message { get; set; } = null!;

    /// <summary>Visual severity / category of the notification (Info, Warning, Error, Success).</summary>
    public ENotificationType Type { get; set; } = ENotificationType.Info;

    /// <summary>Whether the user has opened/acknowledged this notification.</summary>
    public bool IsRead { get; set; }

    /// <summary>UTC timestamp when the notification was marked as read. Null while unread.</summary>
    public DateTime? ReadAt { get; set; }

    #region Domain Methods

    /// <summary>Marks the notification as read and records the timestamp.</summary>
    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    #endregion
}
