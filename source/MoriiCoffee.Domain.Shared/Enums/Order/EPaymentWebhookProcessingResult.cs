namespace MoriiCoffee.Domain.Shared.Enums.Order;

/// <summary>
/// Outcome categories for an incoming Stripe webhook event after the handler runs.
/// Stored on the audit/idempotency row so operators can answer "what happened" for any event.
/// </summary>
public enum EPaymentWebhookProcessingResult
{
    /// <summary>The event was new, the signature was valid, and the handler updated state successfully.</summary>
    Processed = 1,

    /// <summary>
    /// The event id has already been processed before; the second delivery was a no-op (Stripe retry
    /// safety net per FR-008 idempotency requirement).
    /// </summary>
    Duplicate = 2,

    /// <summary>
    /// Signature verification (HMAC-SHA256 via <c>EventUtility.ConstructEvent</c>) failed.
    /// The event payload was rejected and no state was changed. HTTP 422 returned to Stripe.
    /// </summary>
    SignatureInvalid = 3,

    /// <summary>
    /// The event referred to a Payment / Order that does not exist locally. Logged for operator
    /// investigation, but HTTP 200 is returned so Stripe stops retrying.
    /// </summary>
    OrderNotFound = 4,

    /// <summary>
    /// The event type is not one of the four types we subscribe to. The audit row is written and
    /// HTTP 200 is returned to politely stop Stripe from retrying.
    /// </summary>
    UnhandledEventType = 5,

    /// <summary>
    /// The handler threw while processing the event. The audit row is updated with the error
    /// message and HTTP 500 is returned so Stripe will retry with backoff.
    /// </summary>
    Failed = 6
}
