using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Notification.DeleteNotification;

/// <summary>Command to soft-delete a single notification. Verifies the notification belongs to the caller.</summary>
public class DeleteNotificationCommand : ICommand<bool>
{
    public DeleteNotificationCommand(Guid notificationId, Guid userId)
    {
        NotificationId = notificationId;
        UserId = userId;
    }

    public Guid NotificationId { get; }
    public Guid UserId { get; }
}
