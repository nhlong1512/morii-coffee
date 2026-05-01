using MediatR;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Order.CancelOrder;

/// <summary>
/// Command to cancel an existing order. Only the owner of the order may cancel it,
/// and only when the order is still pending staff/admin confirmation.
/// </summary>
public class CancelOrderCommand : ICommand<Unit>
{
    /// <summary>Identifier of the order to cancel.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Identifier of the authenticated user requesting cancellation (set from JWT claims).</summary>
    public Guid UserId { get; set; }
}
