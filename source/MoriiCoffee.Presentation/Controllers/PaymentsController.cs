using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MoriiCoffee.Application.Commands.Payment.CancelPayment;
using MoriiCoffee.Application.Commands.Payment.CreatePaymentIntent;
using MoriiCoffee.Application.Commands.Payment.RefundPayment;
using MoriiCoffee.Application.Queries.Payment.GetPaymentStatus;
using MoriiCoffee.Application.Queries.Payment.GetUserPaymentHistory;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using MoriiCoffee.Domain.Shared.Settings;
using Stripe;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages Stripe payment operations for the authenticated user.
/// The webhook endpoint is anonymous so Stripe can POST to it without a JWT.
/// All other routes require a valid JWT; userId is extracted from the Sub claim.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;
    private readonly StripeSettings _stripe;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger, IOptions<StripeSettings> stripeOptions)
    {
        _mediator = mediator;
        _logger = logger;
        _stripe = stripeOptions.Value;
    }

    // ─── User Endpoints ───────────────────────────────────────────────────────

    /// <summary>Create a Stripe PaymentIntent and return the client secret for frontend confirmation.</summary>
    [HttpPost("create-intent")]
    [Authorize]
    [SwaggerOperation(Summary = "Create payment intent", Description = "Creates a Stripe PaymentIntent and persists a pending Payment record. Returns the clientSecret for Stripe.js.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(PaymentIntentResultDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new CreatePaymentIntentCommand(userId, dto));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Get the current status and details of a payment owned by the caller.</summary>
    [HttpGet("{id:guid}/status")]
    [Authorize]
    [SwaggerOperation(Summary = "Get payment status")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(PaymentDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetPaymentStatus([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetPaymentStatusQuery(id, userId));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get the paginated payment history for the current user, ordered newest first.</summary>
    [HttpGet("history")]
    [Authorize]
    [SwaggerOperation(Summary = "Get payment history")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<PaymentDto>))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> GetPaymentHistory([FromQuery] PaginationFilter filter)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetUserPaymentHistoryQuery(userId, filter));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Cancel a pending payment owned by the caller.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [SwaggerOperation(Summary = "Cancel payment")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> CancelPayment([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new CancelPaymentCommand(id, userId));
        return Ok(new ApiOkResponse("Payment cancelled successfully."));
    }

    // ─── Admin Endpoints ──────────────────────────────────────────────────────

    /// <summary>Admin: refund a succeeded payment via Stripe.</summary>
    [HttpPost("/api/v1/admin/payments/{id:guid}/refund")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(Summary = "Admin: refund payment")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(RefundResultDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> RefundPayment([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(id));
        return Ok(new ApiOkResponse(result));
    }

    // ─── Stripe Webhook ───────────────────────────────────────────────────────

    /// <summary>
    /// Stripe webhook receiver. Verifies the Stripe-Signature header and processes
    /// payment_intent.succeeded / payment_intent.payment_failed events.
    /// Must be [AllowAnonymous] because Stripe does not send a JWT.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Stripe webhook", Description = "Receives Stripe events. Validates the Stripe-Signature header using the configured webhook secret.")]
    [SwaggerResponse(200, "Event acknowledged")]
    [SwaggerResponse(400, "Invalid signature or unreadable body")]
    public async Task<IActionResult> StripeWebhook()
    {
        string json;
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            json = await reader.ReadToEndAsync();
        }

        var signature = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _stripe.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature validation failed.");
            return BadRequest(new { error = "Invalid Stripe signature." });
        }

        _logger.LogInformation("Received Stripe event: {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                if (stripeEvent.Data.Object is PaymentIntent succeededIntent)
                {
                    _logger.LogInformation("PaymentIntent succeeded: {IntentId}", succeededIntent.Id);
                    // Status update is handled through Stripe's API when ConfirmPayment is called.
                    // Webhook acts as a fallback to ensure status is synced.
                }
                break;

            case "payment_intent.payment_failed":
                if (stripeEvent.Data.Object is PaymentIntent failedIntent)
                {
                    _logger.LogWarning("PaymentIntent failed: {IntentId}", failedIntent.Id);
                }
                break;

            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        return Ok();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException();
        return userId;
    }
}
