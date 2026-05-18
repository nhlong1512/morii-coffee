using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
using Stripe;
using Stripe.Checkout;
using System.Linq;

namespace MoriiCoffee.Infrastructure.Services.Payment;

/// <summary>
/// Concrete <see cref="IPaymentGateway"/> backed by the official <c>Stripe.net</c> SDK.
/// This is the ONLY type in the codebase that references <c>Stripe.*</c> — all other layers
/// depend on <see cref="IPaymentGateway"/> abstractions so the domain stays provider-agnostic.
/// </summary>
/// <remarks>
/// <para>
/// The gateway is registered as a scoped service; <see cref="StripeClient"/> instances are
/// cheap to construct but holding one per request is the documented pattern in the SDK.
/// </para>
/// <para>
/// Currency handling: VND is a zero-decimal currency. We send <c>UnitAmount</c> values as-is
/// (no <c>* 100</c> multiplication). The check is asserted in unit tests so a future currency
/// change is impossible without the test changing too.
/// </para>
/// </remarks>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly StripeClient _client;
    private readonly SessionService _sessionService;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    /// <summary>
    /// Constructs the gateway. Built around a per-instance <see cref="StripeClient"/> so future
    /// HTTP-handler instrumentation (retries, tracing, etc.) can be added without static state.
    /// </summary>
    public StripePaymentGateway(StripeSettings settings, ILogger<StripePaymentGateway> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            throw new InvalidOperationException(
                "Stripe SecretKey is not configured. Set the Stripe__SecretKey environment variable.");

        _client = new StripeClient(_settings.SecretKey);
        _sessionService = new SessionService(_client);
        _paymentIntentService = new PaymentIntentService(_client);
        _refundService = new RefundService(_client);
    }

    /// <inheritdoc />
    public string PublishableKey => _settings.PublishableKey;

    /// <inheritdoc />
    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SuccessUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CancelUrl);

        // Build line items. Stripe charges = sum(LineItem.UnitAmount × Quantity); we validate
        // the local sum against TotalAmount here to fail-fast on data integrity issues.
        var sum = request.Items.Sum(i => i.UnitAmount * i.Quantity);
        if (sum != request.TotalAmount)
            throw new InvalidOperationException(
                $"Line-item sum ({sum}) does not match request TotalAmount ({request.TotalAmount}).");

        var options = new SessionCreateOptions
        {
            Mode = "payment",                          // one-shot charge (not subscription)
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = request.Items.Select(line => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = request.Currency,       // "vnd"
                    UnitAmount = line.UnitAmount,      // zero-decimal: integer == đồng
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = line.Name,
                        Description = string.IsNullOrWhiteSpace(line.Description) ? null : line.Description
                    }
                },
                Quantity = line.Quantity
            }).ToList(),
            ClientReferenceId = request.ClientReferenceId,
            Metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            },
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl
        };

        _logger.LogInformation(
            "Creating Stripe Checkout Session {ClientReferenceId} amount {Amount} {Currency}",
            request.ClientReferenceId, request.TotalAmount, request.Currency);

        var session = await _sessionService.CreateAsync(options, cancellationToken: cancellationToken);

        return new CheckoutSessionResult
        {
            SessionId = session.Id,
            Url = session.Url,
            ExpiresAtUtc = session.ExpiresAt
        };
    }

    /// <inheritdoc />
    public async Task<CheckoutSessionStatusResult> GetCheckoutSessionStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var session = await _sessionService.GetAsync(
            sessionId,
            new SessionGetOptions(),
            cancellationToken: cancellationToken);

        string? paymentIntentId = session.PaymentIntentId;
        string? chargeId = null;
        string? failureReason = null;

        if (!string.IsNullOrWhiteSpace(paymentIntentId))
        {
            var paymentIntent = await _paymentIntentService.GetAsync(
                paymentIntentId,
                new PaymentIntentGetOptions
                {
                    Expand = ["latest_charge"]
                },
                cancellationToken: cancellationToken);

            chargeId = paymentIntent.LatestChargeId;
            failureReason = paymentIntent.LastPaymentError?.Message;
        }

        var state = session.Status == "expired"
            ? ECheckoutSessionState.Expired
            : string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
                ? ECheckoutSessionState.Paid
                : ECheckoutSessionState.Pending;

        return new CheckoutSessionStatusResult
        {
            SessionId = session.Id,
            State = state,
            PaymentIntentId = paymentIntentId,
            ChargeId = chargeId,
            FailureReason = failureReason,
            ExpiresAtUtc = session.ExpiresAt
        };
    }

    /// <inheritdoc />
    public WebhookEventEnvelope ConstructWebhookEvent(string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            throw new PaymentGatewaySignatureException("Webhook body is empty.");
        if (string.IsNullOrWhiteSpace(signatureHeader))
            throw new PaymentGatewaySignatureException("Stripe-Signature header is missing.");
        if (string.IsNullOrWhiteSpace(_settings.WebhookSigningSecret))
            throw new InvalidOperationException(
                "Stripe WebhookSigningSecret is not configured. Set Stripe__WebhookSigningSecret.");

        Event stripeEvent;
        try
        {
            // EventUtility verifies the HMAC-SHA256 signature against the raw bytes Stripe signed.
            // throwOnApiVersionMismatch=false: we don't pin a specific Stripe API version, so events
            // shaped by a newer dashboard version still deserialise.
            stripeEvent = EventUtility.ConstructEvent(
                rawBody,
                signatureHeader,
                _settings.WebhookSigningSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            throw new PaymentGatewaySignatureException(
                "Stripe webhook signature verification failed.", ex);
        }

        return MapEventToEnvelope(stripeEvent, rawBody);
    }

    /// <inheritdoc />
    public async Task<RefundResult> CreateRefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PaymentIntentId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.Amount);

        var options = new RefundCreateOptions
        {
            PaymentIntent = request.PaymentIntentId,
            Amount = request.Amount,
            // Stripe expects a fixed enum-like string here. We pick "requested_by_customer" so
            // the refund is treated as a normal merchant-initiated refund (not fraud/duplicate).
            Reason = "requested_by_customer",
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = request.OrderId.ToString(),
                ["adminUserId"] = request.InitiatedByAdminUserId.ToString(),
                ["adminReason"] = request.Reason ?? string.Empty
            }
        };

        _logger.LogInformation(
            "Creating Stripe Refund for PaymentIntent {Pi} amount {Amount} (order {OrderId}) by admin {AdminId}",
            request.PaymentIntentId, request.Amount, request.OrderId, request.InitiatedByAdminUserId);

        var refund = await _refundService.CreateAsync(options, cancellationToken: cancellationToken);

        return new RefundResult
        {
            RefundId = refund.Id,
            Status = refund.Status ?? "pending"
        };
    }

    /// <summary>
    /// Translates a verified Stripe <see cref="Event"/> into a provider-agnostic envelope. The
    /// switch covers the four event types the application subscribes to; any other type results
    /// in an envelope without payload-specific fields, which the handler treats as
    /// <c>UnhandledEventType</c>.
    /// </summary>
    private static WebhookEventEnvelope MapEventToEnvelope(Event stripeEvent, string rawBody)
    {
        var envelope = new WebhookEventEnvelope
        {
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            RawBody = rawBody
        };

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            case "checkout.session.expired":
                if (stripeEvent.Data.Object is Session session)
                {
                    envelope.ProviderSessionId = session.Id;
                    envelope.ProviderPaymentId = session.PaymentIntentId;
                    if (session.Metadata is not null)
                    {
                        if (session.Metadata.TryGetValue("checkoutDraftId", out var draftIdStr) &&
                            Guid.TryParse(draftIdStr, out var draftId))
                            envelope.MetadataCheckoutDraftId = draftId;
                        if (session.Metadata.TryGetValue("orderId", out var orderIdStr) &&
                            Guid.TryParse(orderIdStr, out var orderId))
                            envelope.MetadataOrderId = orderId;
                        if (session.Metadata.TryGetValue("paymentId", out var paymentIdStr) &&
                            Guid.TryParse(paymentIdStr, out var paymentId))
                            envelope.MetadataPaymentId = paymentId;
                    }
                }
                break;

            case "payment_intent.payment_failed":
                if (stripeEvent.Data.Object is PaymentIntent intent)
                {
                    envelope.ProviderPaymentId = intent.Id;
                    envelope.FailureReason = intent.LastPaymentError?.Message;
                    if (intent.Metadata is not null)
                    {
                        if (intent.Metadata.TryGetValue("checkoutDraftId", out var draftIdStr) &&
                            Guid.TryParse(draftIdStr, out var draftId))
                            envelope.MetadataCheckoutDraftId = draftId;
                        if (intent.Metadata.TryGetValue("orderId", out var orderIdStr) &&
                            Guid.TryParse(orderIdStr, out var orderId))
                            envelope.MetadataOrderId = orderId;
                        if (intent.Metadata.TryGetValue("paymentId", out var paymentIdStr) &&
                            Guid.TryParse(paymentIdStr, out var paymentId))
                            envelope.MetadataPaymentId = paymentId;
                    }
                }
                break;

            case "charge.refunded":
                if (stripeEvent.Data.Object is Charge charge)
                {
                    envelope.ProviderChargeId = charge.Id;
                    envelope.ProviderPaymentId = charge.PaymentIntentId;
                    envelope.AmountRefunded = charge.AmountRefunded;
                    envelope.ProviderRefundIds = charge.Refunds?.Data.Select(r => r.Id).ToList()
                                                 ?? new List<string>();
                }
                break;
        }

        return envelope;
    }
}
