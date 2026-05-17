using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Commands.Payment.HandleWebhookEvent;

/// <summary>
/// Processes a verified payment provider webhook event (Stripe, MOMO, VNPAY, etc.). Implements:
/// <list type="bullet">
/// <item>Idempotency via the UNIQUE constraint on <c>PaymentWebhookEvents.StripeEventId</c> (FR-008).</item>
/// <item>Dispatch to event-type handlers based on <see cref="WebhookEventEnvelope.EventType"/> (R-004).</item>
/// <item>An audit row written for every event, regardless of outcome (FR-019).</item>
/// </list>
/// </summary>
/// <remarks>
/// The signature has already been verified by the controller via
/// <see cref="IPaymentGateway.ConstructWebhookEvent"/> before this handler runs. The handler is
/// only invoked for verified events. Provider-specific event types are normalized to
/// <see cref="WebhookEventEnvelope"/> by the gateway implementation.
/// </remarks>
public class HandleWebhookEventCommandHandler
    : ICommandHandler<HandleWebhookEventCommand, HandleWebhookEventResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HandleWebhookEventCommandHandler> _logger;

    public HandleWebhookEventCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<HandleWebhookEventCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HandleWebhookEventResult> Handle(
        HandleWebhookEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.SignatureVerified)
            return await AuditInvalidSignatureAsync(command);

        ArgumentNullException.ThrowIfNull(command.Envelope);

        var envelope = command.Envelope;

        _logger.LogInformation(
            "Processing Stripe webhook event {EventId} of type {EventType}",
            envelope.EventId, envelope.EventType);

        // Step 1: Insert the audit row. The UNIQUE constraint on StripeEventId guarantees that
        // a duplicate delivery (Stripe retry) is detected at the database level — no app-level
        // lock needed.
        var auditRow = PaymentWebhookEvent.Create(
            envelope.EventId,
            envelope.EventType,
            ComputeFingerprint(envelope.RawBody),
            signatureVerified: true);

        var inserted = await _unitOfWork.PaymentWebhookEvents.TryInsertAsync(auditRow);

        if (!inserted)
        {
            // Duplicate delivery — Stripe retried. Return 200 OK so Stripe stops retrying.
            _logger.LogInformation(
                "Duplicate Stripe webhook event {EventId} — no-op", envelope.EventId);
            return new HandleWebhookEventResult
            {
                Result = EPaymentWebhookProcessingResult.Duplicate,
                EventId = envelope.EventId,
                EventType = envelope.EventType
            };
        }

        // Step 2: Dispatch by event type.
        EPaymentWebhookProcessingResult outcome;
        Guid? relatedPaymentId = null;
        string? errorMessage = null;

        try
        {
            (outcome, relatedPaymentId) = await DispatchAsync(envelope);
        }
        catch (Exception ex)
        {
            // Unhandled error — mark audit row Failed and re-throw so the controller returns 500
            // and Stripe retries with backoff.
            _logger.LogError(ex,
                "Unhandled error while processing Stripe webhook {EventId} ({EventType})",
                envelope.EventId, envelope.EventType);

            auditRow.MarkProcessed(
                EPaymentWebhookProcessingResult.Failed,
                relatedPaymentId,
                ex.Message);
            await _unitOfWork.PaymentWebhookEvents.UpdateAsync(auditRow);
            await _unitOfWork.CommitAsync();
            throw;
        }

        auditRow.MarkProcessed(outcome, relatedPaymentId, errorMessage);
        await _unitOfWork.PaymentWebhookEvents.UpdateAsync(auditRow);
        await _unitOfWork.CommitAsync();

        return new HandleWebhookEventResult
        {
            Result = outcome,
            EventId = envelope.EventId,
            EventType = envelope.EventType
        };
    }

    /// <summary>
    /// Dispatches one of the four subscribed event types to its specific handler. Unsupported
    /// types return <see cref="EPaymentWebhookProcessingResult.UnhandledEventType"/> with no
    /// state mutation.
    /// </summary>
    private async Task<(EPaymentWebhookProcessingResult outcome, Guid? relatedPaymentId)>
        DispatchAsync(WebhookEventEnvelope envelope)
    {
        return envelope.EventType switch
        {
            "checkout.session.completed" => await HandleSessionCompletedAsync(envelope),
            "checkout.session.expired" => await HandleSessionExpiredAsync(envelope),
            "payment_intent.payment_failed" => await HandlePaymentFailedAsync(envelope),
            "charge.refunded" => await HandleChargeRefundedAsync(envelope),
            _ => (EPaymentWebhookProcessingResult.UnhandledEventType, (Guid?)null)
        };
    }

    /// <summary>
    /// <c>checkout.session.completed</c> → mark the Payment row Succeeded and the Order Paid.
    /// </summary>
    private async Task<(EPaymentWebhookProcessingResult, Guid?)>
        HandleSessionCompletedAsync(WebhookEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.ProviderSessionId) ||
            string.IsNullOrWhiteSpace(envelope.ProviderPaymentId))
        {
            _logger.LogWarning(
                "checkout.session.completed event {EventId} missing ProviderSessionId or ProviderPaymentId",
                envelope.EventId);
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);
        }

        var payment = await _unitOfWork.Payments.GetBySessionIdAsync(envelope.ProviderSessionId);
        if (payment is null)
        {
            _logger.LogWarning(
                "checkout.session.completed event {EventId} refers to unknown session {SessionId}",
                envelope.EventId, envelope.ProviderSessionId);
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);
        }

        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
        if (order is null)
        {
            _logger.LogWarning(
                "checkout.session.completed event {EventId}: Order {OrderId} not found for session {SessionId}",
                envelope.EventId, payment.OrderId, envelope.ProviderSessionId);
            return (EPaymentWebhookProcessingResult.OrderNotFound, payment.Id);
        }

        // The Charge id is part of the PaymentIntent's latest_charge field — in practice it
        // comes through on the same event payload but we fall back to payment id when not surfaced.
        var chargeId = envelope.ProviderChargeId ?? envelope.ProviderPaymentId;

        payment.MarkSucceeded(envelope.ProviderPaymentId, chargeId);
        order.MarkPaymentPaid(envelope.ProviderPaymentId, chargeId);

        await _unitOfWork.Payments.Update(payment);
        await _unitOfWork.Orders.Update(order);

        _logger.LogInformation(
            "Order {OrderId} marked Paid (ProviderPaymentId {PaymentId}) via webhook {EventId}",
            order.Id, envelope.ProviderPaymentId, envelope.EventId);

        return (EPaymentWebhookProcessingResult.Processed, payment.Id);
    }

    /// <summary>
    /// <c>checkout.session.expired</c> → mark Payment Expired and Order's PaymentStatus Failed.
    /// </summary>
    private async Task<(EPaymentWebhookProcessingResult, Guid?)>
        HandleSessionExpiredAsync(WebhookEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.ProviderSessionId))
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);

        var payment = await _unitOfWork.Payments.GetBySessionIdAsync(envelope.ProviderSessionId);
        if (payment is null)
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);

        // If the payment already succeeded (the customer paid and then the expiry event arrived
        // out of order — rare but possible), do not regress it.
        if (payment.Status == EPaymentTransactionStatus.Succeeded)
        {
            _logger.LogInformation(
                "Ignoring checkout.session.expired {EventId}: payment {PaymentId} is already Succeeded",
                envelope.EventId, payment.Id);
            return (EPaymentWebhookProcessingResult.Processed, payment.Id);
        }

        payment.MarkExpired();

        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
        if (order is not null && order.PaymentStatus == EPaymentStatus.Pending)
        {
            order.MarkPaymentFailed();
            await _unitOfWork.Orders.Update(order);
        }

        await _unitOfWork.Payments.Update(payment);

        _logger.LogInformation(
            "Order {OrderId} payment expired via webhook {EventId}",
            payment.OrderId, envelope.EventId);

        return (EPaymentWebhookProcessingResult.Processed, payment.Id);
    }

    /// <summary>
    /// <c>payment_intent.payment_failed</c> → mark Payment Failed (with reason) and Order
    /// PaymentStatus Failed.
    /// </summary>
    private async Task<(EPaymentWebhookProcessingResult, Guid?)>
        HandlePaymentFailedAsync(WebhookEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.ProviderPaymentId))
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);

        var payment = await _unitOfWork.Payments.GetByPaymentIntentIdAsync(envelope.ProviderPaymentId);
        if (payment is null)
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);

        if (payment.Status == EPaymentTransactionStatus.Succeeded)
        {
            _logger.LogWarning(
                "payment_intent.payment_failed for ProviderPaymentId {PaymentId} but local payment {LocalPaymentId} is already Succeeded",
                envelope.ProviderPaymentId, payment.Id);
            return (EPaymentWebhookProcessingResult.Processed, payment.Id);
        }

        payment.MarkFailed(envelope.FailureReason);

        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
        if (order is not null && order.PaymentStatus == EPaymentStatus.Pending)
        {
            order.MarkPaymentFailed();
            await _unitOfWork.Orders.Update(order);
        }

        await _unitOfWork.Payments.Update(payment);

        _logger.LogInformation(
            "Order {OrderId} payment failed via webhook {EventId}: {Reason}",
            payment.OrderId, envelope.EventId, envelope.FailureReason);

        return (EPaymentWebhookProcessingResult.Processed, payment.Id);
    }

    /// <summary>
    /// <c>charge.refunded</c> → flip pending refund rows to Succeeded and update the Order's
    /// PaymentStatus based on the cumulative refund amount.
    /// </summary>
    private async Task<(EPaymentWebhookProcessingResult, Guid?)>
        HandleChargeRefundedAsync(WebhookEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.ProviderPaymentId))
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);

        var payment = await _unitOfWork.Payments.GetByPaymentIntentIdAsync(envelope.ProviderPaymentId);
        if (payment is null)
            return (EPaymentWebhookProcessingResult.OrderNotFound, null);

        // Mark every pending refund whose provider refund id matches the event payload as Succeeded.
        var matched = 0;
        foreach (var refundId in envelope.ProviderRefundIds)
        {
            var refund = payment.Refunds.FirstOrDefault(r => r.StripeRefundId == refundId);
            if (refund is null) continue;
            refund.MarkSucceeded();
            matched++;
        }

        if (matched == 0)
        {
            _logger.LogWarning(
                "charge.refunded {EventId} for ProviderPaymentId {PaymentId}: none of the {Count} refund ids match local rows",
                envelope.EventId, envelope.ProviderPaymentId, envelope.ProviderRefundIds.Count);
        }

        // Update Order.PaymentStatus based on the cumulative successful refund total.
        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
        if (order is not null)
        {
            var cumulative = payment.TotalSucceededRefundAmount;
            if (cumulative > 0)
            {
                order.ApplyRefund(cumulative);
                await _unitOfWork.Orders.Update(order);
            }
        }

        await _unitOfWork.Payments.Update(payment);

        _logger.LogInformation(
            "Order {OrderId} refund processed via webhook {EventId}",
            payment.OrderId, envelope.EventId);

        return (EPaymentWebhookProcessingResult.Processed, payment.Id);
    }

    /// <summary>
    /// Computes a hex-encoded SHA-256 fingerprint of the raw webhook body. Used to detect "same
    /// event id but different content" anomalies without persisting the entire payload.
    /// </summary>
    private static string ComputeFingerprint(string rawBody)
    {
        var bytes = Encoding.UTF8.GetBytes(rawBody);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Persists a forensic audit row for a webhook whose signature failed verification. We cannot
    /// trust any provider-supplied event id from the body, so we generate a local synthetic id.
    /// </summary>
    private async Task<HandleWebhookEventResult> AuditInvalidSignatureAsync(
        HandleWebhookEventCommand command)
    {
        var rawBody = command.RawBody ?? string.Empty;
        var fingerprint = ComputeFingerprint(rawBody);
        var syntheticEventId = $"invalid-signature:{fingerprint}:{Guid.NewGuid():N}";

        var auditRow = PaymentWebhookEvent.Create(
            syntheticEventId,
            "signature.invalid",
            fingerprint,
            signatureVerified: false);
        auditRow.MarkProcessed(EPaymentWebhookProcessingResult.SignatureInvalid);

        await _unitOfWork.PaymentWebhookEvents.TryInsertAsync(auditRow);
        await _unitOfWork.CommitAsync();

        _logger.LogWarning(
            "Rejected Stripe webhook with invalid signature. Audit row {AuditEventId} created.",
            syntheticEventId);

        return new HandleWebhookEventResult
        {
            Result = EPaymentWebhookProcessingResult.SignatureInvalid,
            EventId = syntheticEventId,
            EventType = "signature.invalid"
        };
    }
}
