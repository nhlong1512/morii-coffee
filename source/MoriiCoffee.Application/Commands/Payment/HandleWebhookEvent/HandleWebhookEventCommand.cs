using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Commands.Payment.HandleWebhookEvent;

/// <summary>
/// Command that processes one verified Stripe webhook event. The webhook controller has already
/// performed signature verification (via <see cref="IPaymentGateway.ConstructWebhookEvent"/>)
/// before constructing this command, so the handler may trust <see cref="Envelope"/> as authentic.
/// </summary>
public class HandleWebhookEventCommand : ICommand<HandleWebhookEventResult>
{
    /// <summary>
    /// Verified, provider-agnostic representation of the event. Null only for the
    /// invalid-signature path where the payload must still be audited.
    /// </summary>
    public WebhookEventEnvelope? Envelope { get; set; }

    /// <summary>
    /// Raw request body as received by the controller. Required when <see cref="Envelope"/> is
    /// null so the handler can still persist a payload fingerprint for forensic audit.
    /// </summary>
    public string? RawBody { get; set; }

    /// <summary>
    /// Whether the gateway successfully verified the signature. Defaults to true for the normal
    /// path; invalid-signature requests set this to false so the handler records an audit row and
    /// returns <see cref="EPaymentWebhookProcessingResult.SignatureInvalid"/>.
    /// </summary>
    public bool SignatureVerified { get; set; } = true;
}

/// <summary>Outcome of processing a single webhook event.</summary>
public class HandleWebhookEventResult
{
    /// <summary>What the handler did with the event (used to choose HTTP response + log level).</summary>
    public EPaymentWebhookProcessingResult Result { get; set; }

    /// <summary>The Stripe event id (echoed for logging).</summary>
    public string EventId { get; set; } = null!;

    /// <summary>The Stripe event type (echoed for logging).</summary>
    public string EventType { get; set; } = null!;
}
