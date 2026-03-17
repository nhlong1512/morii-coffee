using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Notification.GetUnreadNotificationCount;

/// <summary>Returns the count of unread notifications for the authenticated user.</summary>
public record GetUnreadNotificationCountQuery(Guid UserId) : IQuery<int>;
