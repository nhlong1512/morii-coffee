using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Notification.DeleteNotification;

/// <summary>Soft-deletes a notification after verifying ownership.</summary>
public class DeleteNotificationCommandHandler : ICommandHandler<DeleteNotificationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId)
            ?? throw new NotFoundException("Notification", request.NotificationId);

        if (notification.UserId != request.UserId)
            throw new UnauthorizedException("You do not have permission to delete this notification.");

        await _unitOfWork.Notifications.SoftDelete(notification);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
