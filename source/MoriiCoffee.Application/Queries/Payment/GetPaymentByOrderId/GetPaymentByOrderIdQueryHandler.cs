using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Payment.GetPaymentByOrderId;

/// <summary>
/// Handles <see cref="GetPaymentByOrderIdQuery"/>. Loads the Order to confirm it exists and that
/// the caller may see it, then lists all <see cref="Domain.Aggregates.PaymentAggregate.Payment"/>
/// rows for that order with their refund children eager-loaded.
/// </summary>
public class GetPaymentByOrderIdQueryHandler
    : IQueryHandler<GetPaymentByOrderIdQuery, OrderPaymentSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPaymentByOrderIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<OrderPaymentSummaryDto> Handle(
        GetPaymentByOrderIdQuery query,
        CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(query.OrderId);
        if (order is null)
            throw new NotFoundException("Order", query.OrderId);

        if (!query.IsAdmin && order.UserId != query.RequestingUserId)
            throw new UnauthorizedException(
                "You are not authorized to view payment details for this order.");

        var payments = await _unitOfWork.Payments.ListByOrderIdAsync(order.Id);
        var paymentStatus = PaymentStatusResolver.Resolve(order, payments);

        return new OrderPaymentSummaryDto
        {
            OrderId = order.Id,
            PaymentStatus = paymentStatus,
            Payments = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                StripeSessionId = p.StripeSessionId,
                StripePaymentIntentId = p.StripePaymentIntentId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                FailureReason = p.FailureReason,
                CreatedAt = p.CreatedAt,
                Refunds = p.Refunds
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new RefundDto
                    {
                        Id = r.Id,
                        StripeRefundId = r.StripeRefundId,
                        Amount = r.Amount,
                        Reason = r.Reason,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList()
            }).ToList()
        };
    }
}
