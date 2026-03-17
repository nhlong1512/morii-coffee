using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Notification.DeleteAllNotifications;
using MoriiCoffee.Application.Commands.Notification.DeleteNotification;
using MoriiCoffee.Application.Commands.Notification.MarkAllNotificationsAsRead;
using MoriiCoffee.Application.Commands.Notification.MarkNotificationAsRead;
using MoriiCoffee.Application.Queries.Notification.GetNotificationById;
using MoriiCoffee.Application.Queries.Notification.GetUnreadNotificationCount;
using MoriiCoffee.Application.Queries.Notification.GetUserNotifications;
using MoriiCoffee.Application.SeedWork.DTOs.Notification;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages in-app notifications for the authenticated user.
/// All routes require a valid JWT; userId is extracted from the Sub claim.
/// Real-time delivery is handled via the SignalR hub at /hubs/notifications.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ─── Queries ─────────────────────────────────────────────────────────────

    /// <summary>Get a paginated, filtered list of notifications for the current user.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get my notifications", Description = "Returns paginated notifications for the authenticated user. Supports optional filtering by type, date range, and read status.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<NotificationDto>))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> GetNotifications([FromQuery] PaginationFilter filter, [FromQuery] NotificationFilterDto notificationFilter)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetUserNotificationsQuery(userId, filter, notificationFilter));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a single notification by ID. Returns 404 if not found or not owned by caller.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get notification by ID")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(NotificationDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetNotificationById([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetNotificationByIdQuery(id, userId));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get the count of unread notifications for the current user.</summary>
    [HttpGet("unread-count")]
    [SwaggerOperation(Summary = "Get unread notification count")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(int))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _mediator.Send(new GetUnreadNotificationCountQuery(userId));
        return Ok(new ApiOkResponse(count));
    }

    // ─── Commands ────────────────────────────────────────────────────────────

    /// <summary>Mark a single notification as read. Returns 404 if not found or not owned by caller.</summary>
    [HttpPatch("{id:guid}/mark-read")]
    [SwaggerOperation(Summary = "Mark notification as read")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new MarkNotificationAsReadCommand(id, userId));
        return Ok(new ApiOkResponse("Notification marked as read."));
    }

    /// <summary>Mark all notifications for the current user as read in a single batch.</summary>
    [HttpPatch("mark-all-read")]
    [SwaggerOperation(Summary = "Mark all notifications as read")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new MarkAllNotificationsAsReadCommand(userId));
        return Ok(new ApiOkResponse("All notifications marked as read."));
    }

    /// <summary>Soft-delete a single notification. Returns 404 if not found or not owned by caller.</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete notification")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> DeleteNotification([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new DeleteNotificationCommand(id, userId));
        return NoContent();
    }

    /// <summary>Soft-delete all notifications for the current user.</summary>
    [HttpDelete]
    [SwaggerOperation(Summary = "Delete all notifications")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> DeleteAllNotifications()
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new DeleteAllNotificationsCommand(userId));
        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException();
        return userId;
    }
}
