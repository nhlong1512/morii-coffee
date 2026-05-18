using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;
using MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;
using MoriiCoffee.Application.Commands.Payment.RefundPayment;
using MoriiCoffee.Application.Queries.Payment.GetPaymentByOrderId;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Payment endpoints used by customers (create a checkout session, query payment state)
/// and admins (issue refunds). The webhook endpoints are hosted on a separate
/// <see cref="PaymentWebhookController"/> because they need <c>[AllowAnonymous]</c>.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a Stripe-hosted Checkout Session from the authenticated user's current cart.
    /// The frontend should redirect the browser to <see cref="CheckoutSessionResponseDto.CheckoutUrl"/>.
    /// No order is created yet; the backend finalizes the order only after Stripe confirms payment.
    /// </summary>
    [HttpPost("stripe/checkout-session")]
    [SwaggerOperation(Summary = "[Stripe] Create a Checkout Session from cart")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(CheckoutSessionResponseDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionDto dto)
    {
        var result = await _mediator.Send(new CreateCheckoutSessionCommand
        {
            UserId = GetCurrentUserId(),
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            Notes = dto.Notes,
            SaveDeliveryProfile = dto.SaveDeliveryProfile
        });

        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>
    /// Re-checks the Stripe Checkout Session against Stripe and synchronizes local state when the
    /// success redirect returns before the webhook updates the order.
    /// </summary>
    [HttpPost("stripe/reconcile")]
    [SwaggerOperation(Summary = "[Stripe] Reconcile a Stripe payment after success redirect")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(ReconcileStripePaymentResponseDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> ReconcileStripePayment([FromBody] ReconcileStripePaymentDto dto)
    {
        var result = await _mediator.Send(new ReconcileStripePaymentCommand
        {
            SessionId = dto.SessionId,
            CheckoutDraftId = dto.CheckoutDraftId,
            RequestingUserId = GetCurrentUserId(),
            IsAdmin = IsAdmin()
        });

        return Ok(new ApiOkResponse(result));
    }

    /// <summary>
    /// Get all payment attempts (and refunds) for a given order. Caller must be the order owner
    /// or an admin.
    /// </summary>
    [HttpGet("by-order/{orderId:guid}")]
    [SwaggerOperation(Summary = "Get payment history for an order")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(OrderPaymentSummaryDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetByOrder([FromRoute] Guid orderId)
    {
        var result = await _mediator.Send(
            new GetPaymentByOrderIdQuery(orderId, GetCurrentUserId(), IsAdmin()));

        return Ok(new ApiOkResponse(result));
    }

    /// <summary>
    /// [Admin] Issue a full or partial refund against a paid order. Null/zero <c>amount</c>
    /// means full refund of remaining unrefunded balance.
    /// </summary>
    [HttpPost("{orderId:guid}/refund")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "Refund a paid order (admin)")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(RefundResponseDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> Refund(
        [FromRoute] Guid orderId,
        [FromBody] CreateRefundDto dto)
    {
        var result = await _mediator.Send(new RefundPaymentCommand
        {
            OrderId = orderId,
            AdminUserId = GetCurrentUserId(),
            Amount = dto.Amount,
            Reason = dto.Reason
        });

        return Ok(new ApiOkResponse(result));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Extracts the calling user's id from the JWT NameIdentifier claim.</summary>
    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("Invalid or missing user identity claim.");
        return userId;
    }

    /// <summary>Returns <c>true</c> when the authenticated user has the Admin role.</summary>
    private bool IsAdmin() => User.IsInRole(nameof(ERole.ADMIN));
}
