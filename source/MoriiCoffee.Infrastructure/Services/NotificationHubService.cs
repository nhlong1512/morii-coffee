using Microsoft.AspNetCore.SignalR;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Notification;
using MoriiCoffee.Infrastructure.Hubs;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// Concrete implementation of <see cref="INotificationHubService"/> that pushes
/// real-time notifications to a specific user's SignalR group via <see cref="IHubContext{T}"/>.
/// The client listens on the <c>"ReceiveNotification"</c> event name.
/// </summary>
public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>Sends a notification DTO to all active connections belonging to the specified user.</summary>
    public async Task SendToUserAsync(Guid userId, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }
}
