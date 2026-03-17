using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Notification.MarkNotificationAsRead;

/// <summary>Marks a single notification as read after verifying ownership.</summary>
public class MarkNotificationAsReadCommandHandler : ICommandHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId)
            ?? throw new NotFoundException("Notification", request.NotificationId);

        if (notification.UserId != request.UserId)
            throw new UnauthorizedException("You do not have permission to modify this notification.");

        if (!notification.IsRead)
        {
            notification.MarkAsRead();
            await _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.CommitAsync();
        }

        return true;
    }
}
