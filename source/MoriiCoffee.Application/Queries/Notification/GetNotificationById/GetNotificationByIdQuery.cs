using MoriiCoffee.Application.SeedWork.DTOs.Notification;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Notification.GetNotificationById;

/// <summary>Returns a single notification by ID. Verifies ownership against the caller's UserId.</summary>
public record GetNotificationByIdQuery(Guid Id, Guid UserId) : IQuery<NotificationDto>;
