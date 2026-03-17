using MoriiCoffee.Application.SeedWork.DTOs.Notification;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Abstracts the SignalR hub context so the Application layer can push real-time
/// notifications without taking a direct dependency on ASP.NET Core SignalR.
/// The concrete implementation resolves <c>IHubContext&lt;NotificationHub&gt;</c> from DI.
/// </summary>
public interface INotificationHubService
{
    /// <summary>Pushes a notification to the SignalR group for the specified user.</summary>
    Task SendToUserAsync(Guid userId, NotificationDto notification);
}
