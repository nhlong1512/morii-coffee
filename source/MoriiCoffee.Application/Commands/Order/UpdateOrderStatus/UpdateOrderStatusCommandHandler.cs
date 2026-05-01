using MediatR;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Order.UpdateOrderStatus;

/// <summary>
/// Handles <see cref="UpdateOrderStatusCommand"/> by loading the order and delegating the
/// status transition to the Order aggregate, which enforces valid transition rules.
/// </summary>
public class UpdateOrderStatusCommandHandler : ICommandHandler<UpdateOrderStatusCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public UpdateOrderStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(command.OrderId);

        if (order is null)
            throw new NotFoundException("Order", command.OrderId);

        // The aggregate throws InvalidOperationException for invalid transitions
        order.UpdateStatus(command.NewStatus);

        await _unitOfWork.CommitAsync();

        return Unit.Value;
    }
}
