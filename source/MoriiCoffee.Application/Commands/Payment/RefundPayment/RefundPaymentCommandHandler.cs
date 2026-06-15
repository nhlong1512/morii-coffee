using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Commands.Payment.RefundPayment;

/// <summary>
/// Handles <see cref="RefundPaymentCommand"/>. Steps:
/// <list type="number">
/// <item>Load the order's latest succeeded <c>Payment</c>.</item>
/// <item>Compute remaining unrefunded balance and validate the requested amount.</item>
/// <item>Call <see cref="IPaymentGateway.CreateRefundAsync"/>.</item>
/// <item>Persist a <c>RefundRecord</c> with <c>Status = Pending</c>; the
///       <c>charge.refunded</c> webhook flips it to <c>Succeeded</c>.</item>
/// </list>
/// Authorisation (admin-only) is enforced by the controller via
/// <c>[Authorize(Roles = nameof(ERole.ADMIN))]</c>.
/// </summary>
public class RefundPaymentCommandHandler
    : ICommandHandler<RefundPaymentCommand, RefundResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentGatewayResolver gatewayResolver,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _gatewayResolver = gatewayResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RefundResponseDto> Handle(
        RefundPaymentCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Load order and find the last successful Payment.
        var order = await _unitOfWork.Orders.GetByIdAsync(command.OrderId);
        if (order is null)
            throw new NotFoundException("Order", command.OrderId);

        var payment = await _unitOfWork.Payments.GetLatestSucceededByOrderIdAsync(order.Id);
        if (payment is null || string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            throw new BadRequestException(
                "This order has no successful Stripe payment to refund.");

        var gateway = _gatewayResolver.Resolve(payment.Provider);

        var reconciledBeforeRefund = await RefundStateReconciler.ReconcileAsync(
            _unitOfWork,
            gateway,
            order,
            payment,
            command.AdminUserId,
            cancellationToken);
        if (reconciledBeforeRefund.Changed)
            await _unitOfWork.CommitAsync();

        // 2. Validate refund amount against remaining unrefunded balance.
        var alreadyReserved = payment.Refunds
            .Where(r => r.Status != ERefundStatus.Failed)
            .Sum(r => r.Amount);
        var remaining = payment.Amount - alreadyReserved;

        if (remaining <= 0)
            return BuildSynchronizedRefundResponse(order, payment);

        var requestedAmount = command.Amount.GetValueOrDefault() <= 0
            ? remaining
            : command.Amount!.Value;

        if (requestedAmount > remaining)
            throw new BadRequestException(
                "Refund amount exceeds the remaining unrefunded balance.");

        // 3. Call Stripe. If this throws, no local row is persisted.
        RefundResult refundResult;
        try
        {
            refundResult = await gateway.CreateRefundAsync(new RefundRequest
            {
                ProviderSessionId = payment.StripeSessionId,
                PaymentIntentId = payment.StripePaymentIntentId!,
                Amount = (long)requestedAmount,
                IsFullRefund = requestedAmount == payment.Amount,
                OrderId = order.Id,
                InitiatedByAdminUserId = command.AdminUserId,
                Reason = command.Reason
            }, cancellationToken);
        }
        catch (PaymentGatewayAlreadyRefundedException)
        {
            var reconciledAfterProviderConflict = await RefundStateReconciler.ReconcileAsync(
                _unitOfWork,
                gateway,
                order,
                payment,
                command.AdminUserId,
                cancellationToken);
            if (reconciledAfterProviderConflict.Changed)
                await _unitOfWork.CommitAsync();

            alreadyReserved = payment.Refunds
                .Where(r => r.Status != ERefundStatus.Failed)
                .Sum(r => r.Amount);
            remaining = payment.Amount - alreadyReserved;

            if (remaining <= 0)
                return BuildSynchronizedRefundResponse(order, payment);

            throw new BadRequestException(
                "Stripe reports that this payment has already been refunded in part. Local state was synchronized; please retry with the remaining refundable amount.");
        }

        // 4. Persist the RefundRecord inside a transaction so the local audit row + any future
        //    Order state changes commit atomically.
        RefundRecord persistedRefund = null!;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            persistedRefund = RefundRecord.Create(
                payment.Id,
                refundResult.RefundId,
                requestedAmount,
                command.AdminUserId,
                command.Reason,
                payment.Provider);

            payment.AddRefund(persistedRefund);
            await _unitOfWork.Payments.CreateRefundAsync(persistedRefund);
            await _unitOfWork.Payments.Update(payment);
        });

        _logger.LogInformation(
            "Refund {RefundId} ({Amount} VND) initiated for Order {OrderId} by admin {AdminId}",
            refundResult.RefundId, requestedAmount, order.Id, command.AdminUserId);

        // The response reflects the optimistic local state. The charge.refunded webhook will
        // flip RefundRecord.Status to Succeeded and call Order.ApplyRefund.
        // For the immediate response, show what the order's status would optimistically become.
        var optimisticPaymentStatus = (alreadyReserved + requestedAmount) >= payment.Amount
            ? EPaymentStatus.Refunded
            : EPaymentStatus.PartiallyRefunded;

        return new RefundResponseDto
        {
            RefundId = persistedRefund.Id,
            StripeRefundId = refundResult.RefundId,
            Amount = requestedAmount,
            Status = persistedRefund.Status,
            PaymentStatus = optimisticPaymentStatus
        };
    }

    private static RefundResponseDto BuildSynchronizedRefundResponse(
        Domain.Aggregates.OrderAggregate.Order order,
        Domain.Aggregates.PaymentAggregate.Payment payment)
    {
        var latestRefund = payment.Refunds
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latestRefund is null)
            throw new BadRequestException("This payment has already been fully refunded.");

        return new RefundResponseDto
        {
            RefundId = latestRefund.Id,
            StripeRefundId = latestRefund.StripeRefundId,
            Amount = latestRefund.Amount,
            Status = latestRefund.Status,
            PaymentStatus = order.PaymentStatus
        };
    }
}
