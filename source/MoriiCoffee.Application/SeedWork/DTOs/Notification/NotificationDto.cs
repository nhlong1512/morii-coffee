using MoriiCoffee.Domain.Shared.Enums.Notification;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Notification;

/// <summary>Response DTO returned by all notification endpoints.</summary>
public class NotificationDto
{
    [SwaggerSchema("Unique identifier of the notification.")]
    public Guid Id { get; set; }

    [SwaggerSchema("ID of the user this notification belongs to.")]
    public Guid UserId { get; set; }

    [SwaggerSchema("Short heading of the notification.")]
    public string Title { get; set; } = null!;

    [SwaggerSchema("Full body text of the notification.")]
    public string Message { get; set; } = null!;

    [SwaggerSchema("Visual category: Info, Warning, Error, Success.")]
    public ENotificationType Type { get; set; }

    [SwaggerSchema("Whether the user has read this notification.")]
    public bool IsRead { get; set; }

    [SwaggerSchema("UTC timestamp when the notification was read. Null if unread.")]
    public DateTime? ReadAt { get; set; }

    [SwaggerSchema("UTC timestamp when the notification was created.")]
    public DateTime CreatedAt { get; set; }
}
