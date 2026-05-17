using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Payment.GetPaymentByOrderId;

/// <summary>
/// Query for <c>GET /api/v1/payments/by-order/{orderId}</c>. Owner OR admin may view; other
/// users get 401.
/// </summary>
public class GetPaymentByOrderIdQuery : IQuery<OrderPaymentSummaryDto>
{
    public GetPaymentByOrderIdQuery(Guid orderId, Guid requestingUserId, bool isAdmin)
    {
        OrderId = orderId;
        RequestingUserId = requestingUserId;
        IsAdmin = isAdmin;
    }

    /// <summary>Order whose payment history is being requested.</summary>
    public Guid OrderId { get; }

    /// <summary>Calling user id from the JWT.</summary>
    public Guid RequestingUserId { get; }

    /// <summary>True when the caller has the ADMIN role.</summary>
    public bool IsAdmin { get; }
}
