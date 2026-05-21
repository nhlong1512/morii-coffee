using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileRefundPayment;

/// <summary>
/// Synchronizes refund state from Stripe for the latest successful payment on an order.
/// Intended for admin/support flows that want an explicit reconcile step after refund creation.
/// </summary>
public class ReconcileRefundPaymentCommandHandler
    : ICommandHandler<ReconcileRefundPaymentCommand, RefundReconcileResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _gateway;

    public ReconcileRefundPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentGateway gateway)
    {
        _unitOfWork = unitOfWork;
        _gateway = gateway;
    }

    public async Task<RefundReconcileResponseDto> Handle(
        ReconcileRefundPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(command.OrderId);
        if (order is null)
            throw new NotFoundException("Order", command.OrderId);

        var payment = await _unitOfWork.Payments.GetLatestSucceededByOrderIdAsync(order.Id);
        if (payment is null || string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            throw new BadRequestException(
                "This order has no successful Stripe payment to reconcile.");

        var reconciliation = await RefundStateReconciler.ReconcileAsync(
            _unitOfWork,
            _gateway,
            order,
            payment,
            command.AdminUserId,
            cancellationToken);

        if (reconciliation.Changed)
            await _unitOfWork.CommitAsync();

        return new RefundReconcileResponseDto
        {
            OrderId = order.Id,
            PaymentStatus = reconciliation.PaymentStatus,
            LatestRefundStatus = reconciliation.LatestRefund?.Status,
            Reconciled = reconciliation.Changed,
            ReconciledRefundCount = reconciliation.ReconciledRefundCount
        };
    }
}
