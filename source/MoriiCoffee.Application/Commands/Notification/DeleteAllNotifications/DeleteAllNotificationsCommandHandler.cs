using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Notification.DeleteAllNotifications;

/// <summary>Bulk soft-deletes all notifications for the user via the repository bulk method.</summary>
public class DeleteAllNotificationsCommandHandler : ICommandHandler<DeleteAllNotificationsCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAllNotificationsCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(DeleteAllNotificationsCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.Notifications.DeleteAllByUserIdAsync(request.UserId);
        return true;
    }
}
