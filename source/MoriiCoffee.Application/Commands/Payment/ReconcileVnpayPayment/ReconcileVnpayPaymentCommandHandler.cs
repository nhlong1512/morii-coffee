using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileVnpayPayment;

public sealed class ReconcileVnpayPaymentCommandHandler
    : ICommandHandler<ReconcileVnpayPaymentCommand, ReconcileVnpayPaymentResponseDto>
{
    private readonly IStripeCheckoutDraftService _draftService;
    private readonly IPaymentGatewayResolver _resolver;

    public ReconcileVnpayPaymentCommandHandler(IStripeCheckoutDraftService draftService, IPaymentGatewayResolver resolver)
    {
        _draftService = draftService;
        _resolver = resolver;
    }

    public async Task<ReconcileVnpayPaymentResponseDto> Handle(
        ReconcileVnpayPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var draft = command.CheckoutDraftId != Guid.Empty
            ? await _draftService.GetByDraftIdAsync(command.CheckoutDraftId)
            : await _draftService.GetBySessionIdAsync(command.TxnRef!);
        if (draft is null || draft.Provider != EPaymentProvider.Vnpay)
            throw new NotFoundException("VNPAY checkout draft", command.CheckoutDraftId);
        if (!command.IsAdmin && draft.UserId != command.RequestingUserId)
            throw new UnauthorizedException("You are not authorized to reconcile this checkout.");

        var status = await _resolver.Resolve(EPaymentProvider.Vnpay)
            .GetCheckoutSessionStatusAsync(draft.SessionId, draft.CreatedAtUtc, cancellationToken);
        Guid? orderId = null;
        string? orderNumber = null;
        if (status.State == ECheckoutSessionState.Paid)
        {
            var finalized = await _draftService.FinalizeSucceededAsync(
                draft,
                status.PaymentIntentId ?? draft.SessionId,
                status.ChargeId ?? status.PaymentIntentId ?? draft.SessionId,
                cancellationToken);
            orderId = finalized.OrderId;
            orderNumber = finalized.OrderNumber;
        }
        else if (status.State == ECheckoutSessionState.Expired)
        {
            await _draftService.MarkFailedAsync(draft, status.FailureReason);
        }

        return new ReconcileVnpayPaymentResponseDto
        {
            CheckoutDraftId = draft.DraftId,
            TxnRef = draft.SessionId,
            OrderId = orderId,
            OrderNumber = orderNumber,
            PaymentStatus = status.State == ECheckoutSessionState.Paid ? EPaymentStatus.Paid :
                status.State == ECheckoutSessionState.Expired ? EPaymentStatus.Failed : EPaymentStatus.Pending,
            FailureReason = status.FailureReason,
            ExpiresAtUtc = draft.ExpiresAtUtc
        };
    }
}
