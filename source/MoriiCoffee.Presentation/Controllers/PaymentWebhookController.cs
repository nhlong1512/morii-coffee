using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Payment.HandleWebhookEvent;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Receives Stripe webhook events. <strong>Anonymous</strong>: Stripe doesn't use bearer tokens —
/// authentication is performed by verifying the HMAC-SHA256 signature in the
/// <c>Stripe-Signature</c> header against the configured shared signing secret.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint MUST read the raw request bytes verbatim — any re-serialisation by ASP.NET would
/// invalidate the signature. We do that here by reading <c>Request.Body</c> as a string.
/// </para>
/// <para>
/// Response semantics (per <c>contracts/webhook.md</c>):
/// <list type="bullet">
/// <item><c>200 OK</c> — event processed, duplicate, unknown order, or unhandled event type.
///       Stripe stops retrying.</item>
/// <item><c>422 Unprocessable Entity</c> — signature header missing or signature verification failed.</item>
/// <item><c>500 Internal Server Error</c> — handler threw. Stripe retries with backoff.</item>
/// </list>
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/payments/stripe/webhook")]
[AllowAnonymous]
[Produces("application/json")]
public class PaymentWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(
        IMediator mediator,
        IPaymentGatewayResolver gatewayResolver,
        ILogger<PaymentWebhookController> logger)
    {
        _mediator = mediator;
        _gateway = gatewayResolver.Resolve(EPaymentProvider.Stripe);
        _logger = logger;
    }

    /// <summary>
    /// Stripe webhook endpoint. Reads the raw body, verifies the Stripe-Signature header,
    /// and dispatches the event to <see cref="HandleWebhookEventCommand"/>.
    /// </summary>
    [HttpPost]
    [SwaggerOperation(Summary = "[Stripe] Receive a webhook event (anonymous, signature-verified)")]
    [SwaggerResponse(200, "Event processed (or politely ignored).")]
    [SwaggerResponse(422, "Signature verification failed.")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        // 1) Read the raw body bytes. Stripe signs the bytes verbatim; we MUST NOT let ASP.NET
        //    deserialise the body to JSON first because that would re-encode it and break the
        //    signature. Model binding is therefore not applicable here (S6932 suppressed).
#pragma warning disable S6932
        string rawBody;
        using (var reader = new StreamReader(Request.Body, leaveOpen: false))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var signature = Request.Headers["Stripe-Signature"].ToString();
#pragma warning restore S6932

        // 2) Verify the signature. PaymentGatewaySignatureException is converted to HTTP 422.
        WebhookEventEnvelope envelope;
        try
        {
            envelope = _gateway.ConstructWebhookEvent(rawBody, signature);
        }
        catch (PaymentGatewaySignatureException ex)
        {
            // Truncate the signature header in the log so a forensic record exists without
            // leaking the full signature.
            var truncated = TruncateSignatureForLogging(signature);
            _logger.LogWarning(ex,
                "Stripe webhook rejected: signature invalid. Signature header (truncated): {Sig}",
                truncated);

            await _mediator.Send(
                new HandleWebhookEventCommand
                {
                    RawBody = rawBody,
                    SignatureVerified = false
                },
                cancellationToken);

            return UnprocessableEntity(new
            {
                received = false,
                reason = "signature_invalid"
            });
        }

        // 3) Dispatch to the command handler. The handler performs idempotency via the UNIQUE
        //    constraint on PaymentWebhookEvents.StripeEventId, so duplicate deliveries get a no-op.
        var result = await _mediator.Send(
            new HandleWebhookEventCommand { Envelope = envelope },
            cancellationToken);

        _logger.LogInformation(
            "Stripe webhook {EventId} ({EventType}) finished with result {Result}",
            result.EventId, result.EventType, result.Result);

        // 4) Always return 200 for the cases the audit row captured, so Stripe stops retrying.
        //    The handler will rethrow for genuine server errors, which the middleware turns into 500.
        return Ok(new
        {
            received = true,
            result = result.Result.ToString()
        });
    }

    private static string TruncateSignatureForLogging(string signature)
    {
        if (string.IsNullOrEmpty(signature))
            return "(missing)";
        if (signature.Length <= 12)
            return signature;
        return signature[..12] + "...";
    }
}
