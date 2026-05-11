using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Queries.Order.GetValidOrderStatuses;

/// <summary>
/// Returns the list of valid next statuses an admin can transition the order into,
/// based on the order's current status and the domain transition rules.
/// </summary>
public class GetValidOrderStatusesQueryHandler : IQueryHandler<GetValidOrderStatusesQuery, List<EOrderStatus>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetValidOrderStatusesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EOrderStatus>> Handle(GetValidOrderStatusesQuery request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId);

        if (order is null)
            throw new NotFoundException("Order", request.OrderId);

        return order.GetValidNextStatuses().ToList();
    }
}
