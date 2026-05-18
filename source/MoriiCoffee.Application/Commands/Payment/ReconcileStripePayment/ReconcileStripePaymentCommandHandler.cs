using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;

/// <summary>
/// Self-heals Stripe payment state after a success redirect by querying Stripe directly when the
/// authoritative webhook is delayed or missed.
/// </summary>
public class ReconcileStripePaymentCommandHandler
    : ICommandHandler<ReconcileStripePaymentCommand, OrderPaymentSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;

    public ReconcileStripePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
    }

    public async Task<OrderPaymentSummaryDto> Handle(
        ReconcileStripePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(command.OrderId);
        if (order is null)
            throw new NotFoundException("Order", command.OrderId);

        if (!command.IsAdmin && order.UserId != command.RequestingUserId)
            throw new UnauthorizedException(
                "You are not authorized to reconcile payment details for this order.");

        if (order.PaymentMethod != EPaymentMethod.STRIPE)
            throw new BadRequestException("Only Stripe-payment orders can be reconciled.");

        var payment = await ResolvePaymentAttemptAsync(order.Id, command.SessionId);
        if (payment is not null)
        {
            var providerState = await _paymentGateway.GetCheckoutSessionStatusAsync(
                payment.StripeSessionId,
                cancellationToken);

            await ApplyProviderStateAsync(order, payment, providerState);
        }

        var payments = await _unitOfWork.Payments.ListByOrderIdAsync(order.Id);
        return MapToSummary(order.Id, order.PaymentStatus, payments);
    }

    private async Task<PaymentEntity?> ResolvePaymentAttemptAsync(Guid orderId, string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var paymentBySession = await _unitOfWork.Payments.GetBySessionIdAsync(sessionId);
            if (paymentBySession is null || paymentBySession.OrderId != orderId)
                throw new NotFoundException("Stripe checkout session", sessionId);
            return paymentBySession;
        }

        var pendingPayment = await _unitOfWork.Payments.GetLatestPendingByOrderIdAsync(orderId);
        if (pendingPayment is not null)
            return pendingPayment;

        return await _unitOfWork.Payments.GetLatestSucceededByOrderIdAsync(orderId);
    }

    private async Task ApplyProviderStateAsync(
        Domain.Aggregates.OrderAggregate.Order order,
        PaymentEntity payment,
        CheckoutSessionStatusResult providerState)
    {
        var hasChanges = false;

        switch (providerState.State)
        {
            case CheckoutSessionState.Paid:
                if (payment.Status != EPaymentTransactionStatus.Succeeded)
                {
                    if (string.IsNullOrWhiteSpace(providerState.PaymentIntentId) ||
                        string.IsNullOrWhiteSpace(providerState.ChargeId))
                    {
                        throw new InvalidOperationException(
                            "Stripe reported the session as paid but did not include payment identifiers.");
                    }

                    payment.MarkSucceeded(providerState.PaymentIntentId, providerState.ChargeId);
                    await _unitOfWork.Payments.Update(payment);
                    hasChanges = true;
                }

                if (order.PaymentStatus == EPaymentStatus.Pending)
                {
                    order.MarkPaymentPaid(
                        providerState.PaymentIntentId ?? payment.StripePaymentIntentId!,
                        providerState.ChargeId ?? payment.StripeChargeId!);
                    await _unitOfWork.Orders.Update(order);
                    hasChanges = true;
                }
                break;

            case CheckoutSessionState.Expired:
                if (payment.Status == EPaymentTransactionStatus.Created)
                {
                    payment.MarkExpired();
                    await _unitOfWork.Payments.Update(payment);
                    hasChanges = true;
                }

                if (order.PaymentStatus == EPaymentStatus.Pending)
                {
                    order.MarkPaymentFailed();
                    await _unitOfWork.Orders.Update(order);
                    hasChanges = true;
                }
                break;

            case CheckoutSessionState.Pending:
            default:
                break;
        }

        if (hasChanges)
            await _unitOfWork.CommitAsync();
    }

    private static OrderPaymentSummaryDto MapToSummary(
        Guid orderId,
        EPaymentStatus paymentStatus,
        IReadOnlyList<PaymentEntity> payments)
    {
        return new OrderPaymentSummaryDto
        {
            OrderId = orderId,
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
