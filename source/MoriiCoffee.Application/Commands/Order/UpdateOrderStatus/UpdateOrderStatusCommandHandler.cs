using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Commands.Order.UpdateOrderStatus;

/// <summary>
/// Handles <see cref="UpdateOrderStatusCommand"/> by loading the order and delegating the
/// status transition to the Order aggregate, which enforces valid transition rules.
/// Returns the list of valid next statuses after the transition completes.
/// </summary>
public class UpdateOrderStatusCommandHandler : ICommandHandler<UpdateOrderStatusCommand, List<EOrderStatus>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public UpdateOrderStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<List<EOrderStatus>> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(command.OrderId);

        if (order is null)
            throw new NotFoundException("Order", command.OrderId);

        order.UpdateStatus(command.NewStatus);

        await _unitOfWork.CommitAsync();

        return order.GetValidNextStatuses().ToList();
    }
}
