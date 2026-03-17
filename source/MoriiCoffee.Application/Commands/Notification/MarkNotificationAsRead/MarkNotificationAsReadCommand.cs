using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Notification.MarkNotificationAsRead;

/// <summary>Command to mark a single notification as read. Verifies the notification belongs to the caller.</summary>
public class MarkNotificationAsReadCommand : ICommand<bool>
{
    public MarkNotificationAsReadCommand(Guid notificationId, Guid userId)
    {
        NotificationId = notificationId;
        UserId = userId;
    }

    public Guid NotificationId { get; }
    public Guid UserId { get; }
}
