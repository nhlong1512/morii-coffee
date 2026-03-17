using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Notification.GetUnreadNotificationCount;

/// <summary>Delegates to the repository's optimized unread-count query.</summary>
public class GetUnreadNotificationCountQueryHandler
    : IQueryHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUnreadNotificationCountQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
        => await _unitOfWork.Notifications.GetUnreadCountAsync(request.UserId);
}
