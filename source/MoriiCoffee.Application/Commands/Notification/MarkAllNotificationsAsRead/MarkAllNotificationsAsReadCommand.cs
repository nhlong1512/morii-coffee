using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Notification.MarkAllNotificationsAsRead;

/// <summary>Command to mark all notifications for the current user as read in a single batch.</summary>
public class MarkAllNotificationsAsReadCommand : ICommand<bool>
{
    public MarkAllNotificationsAsReadCommand(Guid userId) => UserId = userId;
    public Guid UserId { get; }
}
