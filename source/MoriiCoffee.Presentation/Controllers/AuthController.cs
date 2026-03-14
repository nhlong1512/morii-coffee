using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Auth.ForgotPassword;
using MoriiCoffee.Application.Commands.Auth.RefreshToken;
using MoriiCoffee.Application.Commands.Auth.ResetPassword;
using MoriiCoffee.Application.Commands.Auth.SignIn;
using MoriiCoffee.Application.Commands.Auth.SignUp;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>Handles public authentication endpoints: sign-up, sign-in, token refresh, and forgot password. All routes are anonymous.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }


    /// <summary>Register a new customer account.</summary>
    [HttpPost("signup")]
    [SwaggerOperation(Summary = "Sign up", Description = "Creates a new customer account and returns access + refresh tokens.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(AuthResponseDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> SignUp([FromBody] SignUpDto dto)
    {
        var result = await _mediator.Send(new SignUpCommand(dto));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Sign in with email/username and password.</summary>
    [HttpPost("signin")]
    [SwaggerOperation(Summary = "Sign in", Description = "Authenticates a user and returns JWT access + refresh tokens.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(AuthResponseDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] SignInDto dto)
    {
        var result = await _mediator.Send(new SignInCommand { Identity = dto.Identity, Password = dto.Password });
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Exchange an expired JWT for a new access + refresh token pair. Pass the expired JWT in the Authorization header.</summary>
    [HttpPost("refresh-token")]
    [SwaggerOperation(Summary = "Refresh token", Description = "Issues new tokens. Send expired JWT in Authorization: Bearer header and refresh token in the body.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(AuthResponseDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var accessToken = Request.Headers.Authorization.ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        var result = await _mediator.Send(new RefreshTokenCommand
        {
            AccessToken = accessToken,
            RefreshToken = dto.RefreshToken
        });
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Request a password reset email.</summary>
    [HttpPost("forgot-password")]
    [SwaggerOperation(Summary = "Forgot password", Description = "Sends a password reset email. Always returns 200 to avoid email enumeration.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _mediator.Send(new ForgotPasswordCommand { Email = dto.Email });
        return Ok(new ApiOkResponse("If the email exists, a reset link has been sent."));
    }

    /// <summary>Reset password using the token from the email.</summary>
    [HttpPost("reset-password")]
    [SwaggerOperation(Summary = "Reset password", Description = "Resets the account password using the token received via email.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _mediator.Send(new ResetPasswordCommand
        {
            Email = dto.Email,
            Token = dto.Token,
            NewPassword = dto.NewPassword
        });
        return Ok(new ApiOkResponse("Password reset successfully."));
    }
}
