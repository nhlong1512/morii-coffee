using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Commands.Order.UpdateOrderStatus;

/// <summary>
/// Admin command to advance an order to a new lifecycle status.
/// Returns the valid next statuses after the transition completes.
/// </summary>
public class UpdateOrderStatusCommand : ICommand<List<EOrderStatus>>
{
    /// <summary>Identifier of the order whose status will be updated.</summary>
    public Guid OrderId { get; set; }

    /// <summary>The target lifecycle status to transition the order into.</summary>
    public EOrderStatus NewStatus { get; set; }
}
