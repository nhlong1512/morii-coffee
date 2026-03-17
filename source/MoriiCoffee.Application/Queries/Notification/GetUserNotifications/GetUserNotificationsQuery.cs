using MoriiCoffee.Application.SeedWork.DTOs.Notification;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Notification.GetUserNotifications;

/// <summary>Returns a paginated, filtered list of notifications for the authenticated user.</summary>
public record GetUserNotificationsQuery(
    Guid UserId,
    PaginationFilter Filter,
    NotificationFilterDto NotificationFilter) : IQuery<Pagination<NotificationDto>>;
