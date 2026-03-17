using MoriiCoffee.Domain.Shared.Enums.Notification;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Notification;

/// <summary>Query-string filter parameters for the GET /notifications endpoint.</summary>
public class NotificationFilterDto
{
    [SwaggerSchema("Filter by notification type (Info, Warning, Error, Success). Null returns all types.")]
    public ENotificationType? Type { get; set; }

    [SwaggerSchema("Return only notifications created on or after this UTC date.")]
    public DateTime? From { get; set; }

    [SwaggerSchema("Return only notifications created on or before this UTC date.")]
    public DateTime? To { get; set; }

    [SwaggerSchema("If true, return only unread notifications. If false or null, return all.")]
    public bool? UnreadOnly { get; set; }
}
