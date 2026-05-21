using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Abstraction over the third-party payment provider. The concrete implementation lives in
/// <c>MoriiCoffee.Infrastructure.Services.Payment.StripePaymentGateway</c> and is the ONLY type
/// that takes a dependency on <c>Stripe.net</c>. Keeping the Stripe SDK out of the Application
/// and Domain layers preserves Clean Architecture: domain logic depends only on this interface.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Creates a hosted-Checkout session at the provider and returns the URL the customer should
    /// be redirected to. Idempotent at the provider via Stripe's own deduplication of the request
    /// id, but the caller is responsible for not double-creating sessions per order.
    /// </summary>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current state of a hosted checkout session from the payment provider. Used
    /// by reconciliation flows to self-heal when the success redirect reaches the frontend before
    /// the webhook has updated local state.
    /// </summary>
    Task<CheckoutSessionStatusResult> GetCheckoutSessionStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the cryptographic signature on a raw webhook body and returns a strongly-typed
    /// envelope. Throws <see cref="PaymentGatewaySignatureException"/> when the signature header
    /// is missing or does not match the body and shared signing secret.
    /// </summary>
    /// <remarks>
    /// The implementation MUST read the raw body bytes exactly as Stripe sent them — any
    /// re-serialisation will break the signature.
    /// </remarks>
    WebhookEventEnvelope ConstructWebhookEvent(string rawBody, string? signatureHeader);

    /// <summary>
    /// Issues a refund at the provider against a successful payment intent. The provider will
    /// later emit a <c>charge.refunded</c> webhook that the application uses to finalise local
    /// state.
    /// </summary>
    Task<RefundResult> CreateRefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the provider-side refund state for a successful payment intent. Used by the
    /// refund flow to reconcile local state before issuing a new refund and to self-heal when a
    /// refund was created directly in Stripe or a webhook was missed.
    /// </summary>
    Task<PaymentProviderStatusResult> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The Stripe publishable key — safe to expose to the frontend. Returned to the client in
    /// the checkout-session response so future iterations can render Stripe Elements if desired.
    /// </summary>
    string PublishableKey { get; }
}

/// <summary>Input for <see cref="IPaymentGateway.CreateCheckoutSessionAsync"/>.</summary>
public class CreateCheckoutSessionRequest
{
    /// <summary>
    /// Optional client-facing correlation id displayed in the provider dashboard search.
    /// For payment-first Stripe this is the checkout draft id; for legacy flows it can be an order id.
    /// </summary>
    public string? ClientReferenceId { get; set; }

    /// <summary>
    /// Arbitrary metadata echoed back by the provider on webhook events.
    /// Used to correlate a hosted session to internal drafts/orders/payments.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// One line item per <c>OrderItem</c>. The provider draws the line items on the checkout page,
    /// so this is the customer's view of what they're paying for.
    /// </summary>
    public IReadOnlyList<CheckoutLineItem> Items { get; set; } = [];

    /// <summary>
    /// Authoritative order total in the currency's smallest accountable unit. For VND (zero-decimal)
    /// this equals the đồng amount. The sum of <see cref="CheckoutLineItem.UnitAmount"/> × Quantity
    /// MUST equal this value; the gateway implementation validates and forwards both.
    /// </summary>
    public long TotalAmount { get; set; }

    /// <summary>Lowercase ISO 4217 (e.g. <c>vnd</c>).</summary>
    public string Currency { get; set; } = "vnd";

    /// <summary>Absolute URL the customer is redirected to after a successful charge.</summary>
    public string SuccessUrl { get; set; } = null!;

    /// <summary>Absolute URL the customer is redirected to when they cancel.</summary>
    public string CancelUrl { get; set; } = null!;
}

/// <summary>One row on the hosted Checkout page.</summary>
public class CheckoutLineItem
{
    /// <summary>Product display name (e.g. "Cà phê sữa đá").</summary>
    public string Name { get; set; } = null!;

    /// <summary>Optional secondary label, e.g. a variant such as "Size L".</summary>
    public string? Description { get; set; }

    /// <summary>Unit price in the same currency units as <see cref="CreateCheckoutSessionRequest.TotalAmount"/>.</summary>
    public long UnitAmount { get; set; }

    /// <summary>Quantity (positive integer).</summary>
    public long Quantity { get; set; }
}

/// <summary>Output of <see cref="IPaymentGateway.CreateCheckoutSessionAsync"/>.</summary>
public class CheckoutSessionResult
{
    /// <summary>Stripe Checkout Session id (e.g. <c>cs_test_...</c>).</summary>
    public string SessionId { get; set; } = null!;

    /// <summary>The URL the frontend should redirect the browser to (e.g. <c>https://checkout.stripe.com/...</c>).</summary>
    public string Url { get; set; } = null!;

    /// <summary>UTC time at which the session expires at Stripe (default 24 h from creation).</summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>Output of <see cref="IPaymentGateway.GetCheckoutSessionStatusAsync"/>.</summary>
public class CheckoutSessionStatusResult
{
    /// <summary>Provider checkout session identifier.</summary>
    public string SessionId { get; set; } = null!;

    /// <summary>Current provider-side state of the checkout session.</summary>
    public ECheckoutSessionState State { get; set; }

    /// <summary>Provider payment intent identifier, when payment has been created.</summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>Provider charge identifier, when available.</summary>
    public string? ChargeId { get; set; }

    /// <summary>Best-effort provider failure reason when payment failed.</summary>
    public string? FailureReason { get; set; }

    /// <summary>UTC session expiry timestamp, when surfaced by the provider.</summary>
    public DateTime? ExpiresAtUtc { get; set; }
}

/// <summary>Input for <see cref="IPaymentGateway.CreateRefundAsync"/>.</summary>
public class RefundRequest
{
    /// <summary>Stripe PaymentIntent id of the original successful charge (e.g. <c>pi_3OZA...</c>).</summary>
    public string PaymentIntentId { get; set; } = null!;

    /// <summary>Refund amount in the currency's smallest unit (for VND, equals đồng).</summary>
    public long Amount { get; set; }

    /// <summary>Internal Order id, attached to Stripe's metadata for cross-system search.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Id of the admin user issuing the refund — attached to Stripe metadata for audit.</summary>
    public Guid InitiatedByAdminUserId { get; set; }

    /// <summary>Optional free-text reason. Forwarded as metadata.</summary>
    public string? Reason { get; set; }
}

/// <summary>Output of <see cref="IPaymentGateway.CreateRefundAsync"/>.</summary>
public class RefundResult
{
    /// <summary>Stripe Refund id (e.g. <c>re_3OZB...</c>).</summary>
    public string RefundId { get; set; } = null!;

    /// <summary>Refund status string as returned by Stripe (e.g. <c>pending</c>, <c>succeeded</c>).</summary>
    public string Status { get; set; } = null!;
}

/// <summary>Provider-side refund summary for a successful payment intent.</summary>
public class PaymentProviderStatusResult
{
    /// <summary>The provider payment intent identifier that was queried.</summary>
    public string PaymentIntentId { get; set; } = null!;

    /// <summary>The provider charge identifier currently associated with the payment intent.</summary>
    public string? ChargeId { get; set; }

    /// <summary>Total amount already refunded at the provider, in VND.</summary>
    public long AmountRefunded { get; set; }

    /// <summary>Provider refund rows currently attached to the charge.</summary>
    public IReadOnlyList<ProviderRefundStatusResult> Refunds { get; set; } = [];
}

/// <summary>One provider refund row attached to a payment intent's charge.</summary>
public class ProviderRefundStatusResult
{
    /// <summary>Provider refund identifier, e.g. <c>re_...</c>.</summary>
    public string RefundId { get; set; } = null!;

    /// <summary>Refund amount in VND.</summary>
    public long Amount { get; set; }

    /// <summary>Raw provider refund status, e.g. <c>pending</c>, <c>succeeded</c>, <c>failed</c>.</summary>
    public string Status { get; set; } = null!;
}

/// <summary>
/// Thrown when the payment provider rejects a refund because the charge has already been
/// refunded. The application can catch this and reconcile local refund state instead of
/// surfacing a generic 500.
/// </summary>
public class PaymentGatewayAlreadyRefundedException : Exception
{
    public PaymentGatewayAlreadyRefundedException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Provider-agnostic representation of a verified webhook event. The infrastructure layer
/// translates provider-specific events (Stripe, MOMO, VNPAY) into this shape so the
/// Application layer never sees raw provider-specific types.
/// </summary>
public class WebhookEventEnvelope
{
    /// <summary>Provider-supplied event id (e.g. Stripe: <c>evt_1Mw...</c>). Unique per event.</summary>
    public string EventId { get; set; } = null!;

    /// <summary>
    /// Provider-supplied event type (e.g. Stripe: <c>checkout.session.completed</c>,
    /// MOMO: <c>payment.completed</c>).
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Verbatim bytes that the provider signed. Stored as a string for hashing/forensics; never
    /// re-deserialised by application code.
    /// </summary>
    public string RawBody { get; set; } = null!;

    /// <summary>
    /// Our internal order id, extracted from provider metadata when we created the session.
    /// Used to find the local Order without extra provider roundtrips.
    /// </summary>
    public Guid? MetadataOrderId { get; set; }

    /// <summary>Our internal payment id, extracted from provider metadata when we created the session.</summary>
    public Guid? MetadataPaymentId { get; set; }

    /// <summary>
    /// Internal checkout draft id for payment-first flows. Present when the checkout session was
    /// created before any order existed locally.
    /// </summary>
    public Guid? MetadataCheckoutDraftId { get; set; }

    // ─── Provider Transaction Identifiers ────────────────────────────────────────────────────
    // These are mapped from provider-specific fields during envelope construction.
    // Stripe examples shown; MOMO/VNPAY will populate the same properties with their own ids.

    /// <summary>
    /// Provider's checkout session identifier. Used to find the Payment row by ProviderSessionId.
    /// <para>Stripe example: <c>cs_test_...</c> from Session.Id</para>
    /// <para>MOMO example: order id from payment order response</para>
    /// </summary>
    public string? ProviderSessionId { get; set; }

    /// <summary>
    /// Provider's payment identifier. Stored on Payment.StripePaymentIntentId and used for
    /// refund operations and idempotency matching.
    /// <para>Stripe example: <c>pi_3OZA...</c> from PaymentIntent.Id or Session.PaymentIntentId</para>
    /// <para>MOMO example: transaction reference id</para>
    /// </summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>
    /// Provider's charge/transaction identifier. Populated when a charge event is received.
    /// <para>Stripe example: <c>ch_...</c> from Charge.Id</para>
    /// <para>MOMO example: not used (payment id is sufficient)</para>
    /// </summary>
    public string? ProviderChargeId { get; set; }

    /// <summary>
    /// Refund identifiers from a refund event. Multiple refunds can appear in a single event.
    /// <para>Stripe example: Refund ids from Charge.Refunds[*].Id on <c>charge.refunded</c> event</para>
    /// <para>MOMO example: refund transaction ids from refund.complete event</para>
    /// </summary>
    public IReadOnlyList<string> ProviderRefundIds { get; set; } = [];

    /// <summary>
    /// Cumulative refunded amount for refund events (informational).
    /// <para>Stripe example: Charge.AmountRefunded on <c>charge.refunded</c></para>
    /// </summary>
    public long? AmountRefunded { get; set; }

    /// <summary>Best-effort failure reason for failed-payment events; null otherwise.</summary>
    public string? FailureReason { get; set; }
}

/// <summary>Thrown by <see cref="IPaymentGateway.ConstructWebhookEvent"/> when signature verification fails.</summary>
public class PaymentGatewaySignatureException : Exception
{
    public PaymentGatewaySignatureException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
