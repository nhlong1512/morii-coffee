using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Order.CancelOrder;
using MoriiCoffee.Application.Commands.Order.PlaceOrder;
using MoriiCoffee.Application.Commands.Order.UpdateOrderStatus;
using MoriiCoffee.Application.Queries.Order.GetAllOrders;
using MoriiCoffee.Application.Queries.Order.GetMyOrders;
using MoriiCoffee.Application.Queries.Order.GetOrderById;
using MoriiCoffee.Application.Queries.Order.GetValidOrderStatuses;
using MoriiCoffee.Application.SeedWork.DTOs.Order;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages customer orders. Authenticated users can place orders, view their own orders,
/// and cancel orders that are still pending confirmation. Admins can view all orders and update order statuses.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with its required mediator dependency.</summary>
    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Place a new order from the current user's cart.
    /// This endpoint is intended for payment methods that create an order immediately, such as COD.
    /// Stripe checkout now uses a payment-first flow via <c>POST /api/v1/payments/stripe/checkout-session</c>.
    /// </summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Place a new order from cart")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(OrderDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        var result = await _mediator.Send(new PlaceOrderCommand
        {
            UserId = GetCurrentUserId(),
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            Notes = dto.Notes,
            PaymentMethod = dto.PaymentMethod,
            SaveDeliveryProfile = dto.SaveDeliveryProfile
        });

        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>
    /// Get the authenticated user's order history.
    /// Results are ordered by creation date descending. Optional status filter applied.
    /// </summary>
    [HttpGet("my")]
    [SwaggerOperation(Summary = "Get my orders")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<OrderSummaryDto>))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> GetMyOrders([FromQuery] EOrderStatus? status = null)
    {
        var result = await _mediator.Send(new GetMyOrdersQuery(GetCurrentUserId(), status));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>
    /// Get a specific order by ID. Returns 403 if the order does not belong to the calling user.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get order by ID")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(OrderDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id, GetCurrentUserId(), IsAdmin()));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>
    /// Cancel an existing order. Only the order owner may cancel, and only before staff/admin confirmation.
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    [SwaggerOperation(Summary = "Cancel an order")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> CancelOrder([FromRoute] Guid id)
    {
        await _mediator.Send(new CancelOrderCommand
        {
            OrderId = id,
            UserId = GetCurrentUserId()
        });

        return Ok(new ApiOkResponse("Order cancelled successfully."));
    }

    /// <summary>
    /// [Admin] Get all orders in the system, optionally filtered by status and/or user.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "Get all orders (admin)")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<OrderSummaryDto>))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] EOrderStatus? status = null,
        [FromQuery] Guid? userId = null)
    {
        var result = await _mediator.Send(new GetAllOrdersQuery(status, userId));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>
    /// [Admin] Update the lifecycle status of an order. Invalid transitions are rejected by the domain.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "Update order status (admin)")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(List<EOrderStatus>))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderStatusDto dto)
    {
        var validStatuses = await _mediator.Send(new UpdateOrderStatusCommand
        {
            OrderId = id,
            NewStatus = dto.NewStatus
        });

        return Ok(new ApiOkResponse(validStatuses));
    }

    /// <summary>
    /// [Admin] Get the list of valid next statuses for a given order based on its current state.
    /// </summary>
    [HttpGet("{id:guid}/valid-statuses")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "Get valid next statuses for an order (admin)")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<EOrderStatus>))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetValidOrderStatuses([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetValidOrderStatusesQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Extracts the authenticated user's ID from the JWT NameIdentifier claim.</summary>
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
