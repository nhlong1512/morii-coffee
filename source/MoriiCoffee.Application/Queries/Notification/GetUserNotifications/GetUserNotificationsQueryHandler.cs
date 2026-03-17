using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Notification;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Notification.GetUserNotifications;

/// <summary>Builds a filtered, paginated notification list for the caller.</summary>
public class GetUserNotificationsQueryHandler
    : IQueryHandler<GetUserNotificationsQuery, Pagination<NotificationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserNotificationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Pagination<NotificationDto>> Handle(
        GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Notifications
            .FindByCondition(n => n.UserId == request.UserId);

        var f = request.NotificationFilter;

        if (f.Type.HasValue)
            query = query.Where(n => n.Type == f.Type.Value);

        if (f.UnreadOnly == true)
            query = query.Where(n => !n.IsRead);

        if (f.From.HasValue)
            query = query.Where(n => n.CreatedAt >= f.From.Value);

        if (f.To.HasValue)
            query = query.Where(n => n.CreatedAt <= f.To.Value);

        query = query.OrderByDescending(n => n.CreatedAt);

        var dtoQuery = query
            .AsEnumerable()
            .Select(n => _mapper.Map<NotificationDto>(n))
            .AsQueryable();

        var result = PagingHelper.QueryPaginate(request.Filter, dtoQuery);
        return Task.FromResult(result);
    }
}
