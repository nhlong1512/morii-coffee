using System.Security.Claims;
using MoriiCoffee.Application.SeedWork.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.User.AssignRoles;
using MoriiCoffee.Application.Commands.User.ChangeAvatar;
using MoriiCoffee.Application.Commands.User.ChangePassword;
using MoriiCoffee.Application.Commands.User.SaveDeliveryProfile;
using MoriiCoffee.Application.Commands.User.UpdateProfile;
using MoriiCoffee.Application.Queries.User.GetMyDeliveryProfile;
using MoriiCoffee.Application.Queries.User.GetMyProfile;
using MoriiCoffee.Application.Queries.User.GetPaginatedUsers;
using MoriiCoffee.Application.Queries.User.GetUserById;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>Handles user management endpoints. Self-service profile routes require any valid JWT; admin routes require ADMIN or STAFF roles.</summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public UsersController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    // ─── My Profile ──────────────────────────────────────────────────────────

    /// <summary>Get the current user's profile.</summary>
    [HttpGet("me")]
    [SwaggerOperation(Summary = "Get my profile")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(UserDto))]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyProfileQuery(userId));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Update the current user's profile fields.</summary>
    [HttpPut("me/profile")]
    [SwaggerOperation(Summary = "Update my profile")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(UserDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var command = _mapper.Map<UpdateProfileCommand>(dto);
        command.UserId = GetCurrentUserId();
        var result = await _mediator.Send(command);
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Upload a new avatar for the current user.</summary>
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Change my avatar")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(UserDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> ChangeAvatar([FromForm] ChangeAvatarDto dto)
    {
        var result = await _mediator.Send(new ChangeAvatarCommand
        {
            UserId = GetCurrentUserId(),
            Avatar = dto.Avatar
        });
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Change the current user's password.</summary>
    [HttpPut("me/change-password")]
    [SwaggerOperation(Summary = "Change my password")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _mediator.Send(new ChangePasswordCommand
        {
            UserId = GetCurrentUserId(),
            CurrentPassword = dto.CurrentPassword,
            NewPassword = dto.NewPassword
        });
        return Ok(new ApiOkResponse("Password changed successfully."));
    }
    // ─── Delivery Profile ─────────────────────────────────────────────────────

    /// <summary>Get the current user's saved delivery profile. Returns 200 with null data if none has been saved yet.</summary>
    [HttpGet("me/delivery-profile")]
    [SwaggerOperation(Summary = "Get my delivery profile")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(DeliveryProfileDto))]
    public async Task<IActionResult> GetMyDeliveryProfile()
    {
        var result = await _mediator.Send(new GetMyDeliveryProfileQuery(GetCurrentUserId()));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Create or update the current user's delivery profile.</summary>
    [HttpPut("me/delivery-profile")]
    [SwaggerOperation(Summary = "Save my delivery profile")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(DeliveryProfileDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> SaveDeliveryProfile([FromBody] SaveDeliveryProfileDto dto)
    {
        var result = await _mediator.Send(new SaveDeliveryProfileCommand
        {
            UserId = GetCurrentUserId(),
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address
        });
        return Ok(new ApiOkResponse(result));
    }

    // ─── Admin / Staff ────────────────────────────────────────────────────────

    /// <summary>Get a paginated list of users.</summary>
    [HttpGet]
    [Authorize(Roles = $"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}")]
    [SwaggerOperation(Summary = "Get paginated users")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<UserSummaryDto>))]
    public async Task<IActionResult> GetPaginatedUsers(
        [FromQuery] PaginationFilter filter,
        [FromQuery] string? search,
        [FromQuery] EUserStatus? status)
    {
        var result = await _mediator.Send(new GetPaginatedUsersQuery(filter, search, status));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a user by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}")]
    [SwaggerOperation(Summary = "Get user by ID")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(UserDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Replace a user's full role set.</summary>
    [HttpPut("{id:guid}/roles")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "Assign roles (admin)", Description = "Replaces the user's full role set with the provided list.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> AssignRoles([FromRoute] Guid id, [FromBody] AssignRolesDto dto)
    {
        await _mediator.Send(new AssignRolesCommand { UserId = id, Roles = dto.Roles });
        return Ok(new ApiOkResponse("Roles updated successfully."));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        // JWT middleware maps "sub" → ClaimTypes.NameIdentifier by default
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("Invalid or missing user identity claim.");
        return userId;
    }
}
