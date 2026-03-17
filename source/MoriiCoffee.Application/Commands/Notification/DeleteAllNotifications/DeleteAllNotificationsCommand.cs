using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Notification.DeleteAllNotifications;

/// <summary>Command to soft-delete all notifications belonging to the current user.</summary>
public class DeleteAllNotificationsCommand : ICommand<bool>
{
    public DeleteAllNotificationsCommand(Guid userId) => UserId = userId;
    public Guid UserId { get; }
}
