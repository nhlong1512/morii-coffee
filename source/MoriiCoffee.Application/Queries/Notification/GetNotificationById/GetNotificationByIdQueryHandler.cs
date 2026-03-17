using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Notification;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Notification.GetNotificationById;

/// <summary>Fetches a notification by ID and verifies it belongs to the requesting user.</summary>
public class GetNotificationByIdQueryHandler : IQueryHandler<GetNotificationByIdQuery, NotificationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetNotificationByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<NotificationDto> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Notification", request.Id);

        if (notification.UserId != request.UserId)
            throw new UnauthorizedException("You do not have permission to view this notification.");

        return _mapper.Map<NotificationDto>(notification);
    }
}
