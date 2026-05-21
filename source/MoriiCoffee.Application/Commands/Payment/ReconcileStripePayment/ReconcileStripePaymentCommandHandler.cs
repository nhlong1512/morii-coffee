using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
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
    : ICommandHandler<ReconcileStripePaymentCommand, ReconcileStripePaymentResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IStripeCheckoutDraftService _draftService;

    public ReconcileStripePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        IStripeCheckoutDraftService draftService)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _draftService = draftService;
    }

    public async Task<ReconcileStripePaymentResponseDto> Handle(
        ReconcileStripePaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.SessionId) && command.CheckoutDraftId is null)
            throw new BadRequestException("Either SessionId or CheckoutDraftId is required.");

        if (!string.IsNullOrWhiteSpace(command.SessionId))
        {
            var finalizedPayment = await _unitOfWork.Payments.GetBySessionIdAsync(command.SessionId);
            if (finalizedPayment is not null)
                return await BuildFinalizedResponseAsync(finalizedPayment, command);
        }

        var draft = await ResolveDraftAsync(command);
        if (draft is null)
            throw new NotFoundException(
                "Stripe checkout draft",
                command.CheckoutDraftId?.ToString() ?? command.SessionId!);

        EnsureAuthorized(draft.UserId, command);

        var providerState = await _paymentGateway.GetCheckoutSessionStatusAsync(
            draft.SessionId,
            cancellationToken);

        switch (providerState.State)
        {
            case ECheckoutSessionState.Paid:
                if (string.IsNullOrWhiteSpace(providerState.PaymentIntentId) ||
                    string.IsNullOrWhiteSpace(providerState.ChargeId))
                {
                    throw new InvalidOperationException(
                        "Stripe reported the session as paid but did not return payment identifiers.");
                }

                var finalized = await _draftService.FinalizeSucceededAsync(
                    draft,
                    providerState.PaymentIntentId,
                    providerState.ChargeId,
                    cancellationToken);

                return new ReconcileStripePaymentResponseDto
                {
                    CheckoutDraftId = draft.DraftId,
                    SessionId = draft.SessionId,
                    OrderId = finalized.OrderId,
                    OrderNumber = finalized.OrderNumber,
                    PaymentStatus = finalized.PaymentStatus,
                    FailureReason = null,
                    ExpiresAtUtc = draft.ExpiresAtUtc
                };

            case ECheckoutSessionState.Expired:
                await _draftService.MarkExpiredAsync(draft);
                draft.PaymentStatus = EPaymentStatus.Failed;
                draft.FailureReason = "Stripe checkout session expired before payment was completed.";
                break;

            case ECheckoutSessionState.Pending:
            default:
                break;
        }

        return new ReconcileStripePaymentResponseDto
        {
            CheckoutDraftId = draft.DraftId,
            SessionId = draft.SessionId,
            OrderId = null,
            OrderNumber = null,
            PaymentStatus = draft.PaymentStatus,
            FailureReason = draft.FailureReason ?? providerState.FailureReason,
            ExpiresAtUtc = providerState.ExpiresAtUtc ?? draft.ExpiresAtUtc
        };
    }

    private async Task<StripeCheckoutDraftCacheDto?> ResolveDraftAsync(ReconcileStripePaymentCommand command)
    {
        if (command.CheckoutDraftId is Guid draftId && draftId != Guid.Empty)
            return await _draftService.GetByDraftIdAsync(draftId);

        return await _draftService.GetBySessionIdAsync(command.SessionId!);
    }

    private async Task<ReconcileStripePaymentResponseDto> BuildFinalizedResponseAsync(
        PaymentEntity payment,
        ReconcileStripePaymentCommand command)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId)
            ?? throw new InvalidOperationException(
                $"Payment {payment.Id} exists for session {payment.StripeSessionId} but its order is missing.");

        EnsureAuthorized(order.UserId, command);

        return new ReconcileStripePaymentResponseDto
        {
            CheckoutDraftId = null,
            SessionId = payment.StripeSessionId,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            PaymentStatus = PaymentStatusResolver.Resolve(order, [payment]),
            FailureReason = payment.FailureReason,
            ExpiresAtUtc = null
        };
    }

    private static void EnsureAuthorized(Guid ownerUserId, ReconcileStripePaymentCommand command)
    {
        if (!command.IsAdmin && ownerUserId != command.RequestingUserId)
            throw new UnauthorizedException(
                "You are not authorized to reconcile this Stripe checkout session.");
    }
}
