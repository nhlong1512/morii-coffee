using MediatR;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Order.CancelOrder;

/// <summary>
/// Handles <see cref="CancelOrderCommand"/> by verifying ownership and delegating cancellation
/// to the order aggregate, which enforces its own status-transition rules.
/// </summary>
public class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public CancelOrderCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(command.OrderId);

        if (order is null)
            throw new NotFoundException("Order", command.OrderId);

        if (order.UserId != command.UserId)
            throw new UnauthorizedException("You are not authorized to cancel this order.");

        // The aggregate throws InvalidOperationException if the status is not PENDING or CONFIRMED
        order.Cancel();

        await _unitOfWork.CommitAsync();

        return Unit.Value;
    }
}
