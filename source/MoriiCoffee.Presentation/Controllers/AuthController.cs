using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Auth.ExternalLogin;
using MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;
using MoriiCoffee.Application.Commands.Auth.ForgotPassword;
using MoriiCoffee.Application.Commands.Auth.RefreshToken;
using MoriiCoffee.Application.Commands.Auth.ResetPassword;
using MoriiCoffee.Application.Commands.Auth.SignIn;
using MoriiCoffee.Application.Commands.Auth.SignUp;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

    /// <summary>Sign in with email and password. BREAKING CHANGE: Phone numbers are no longer accepted as identity.</summary>
    [HttpPost("signin")]
    [SwaggerOperation(Summary = "Sign in", Description = "Authenticates a user by email address and returns JWT access + refresh tokens. Phone numbers are no longer supported for authentication.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(AuthResponseDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
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

    /// <summary>
    /// Initiate OAuth 2.0 authentication flow with Google.
    /// Redirects user to Google's consent screen for authentication.
    /// After successful authentication, Google redirects back to external-auth-callback endpoint.
    /// </summary>
    /// <param name="provider">OAuth provider name. Currently only "Google" is supported (case-insensitive).</param>
    /// <param name="returnUrl">URL to redirect to after successful authentication. Defaults to "/".</param>
    /// <returns>HTTP 302 redirect to Google's OAuth consent screen.</returns>
    [HttpPost("external-login")]
    [SwaggerOperation(
        Summary = "External login with Google OAuth",
        Description = "Initiates Google OAuth 2.0 authorization code flow. Redirects to Google for authentication, then returns to external-auth-callback with authorization code.")]
    [SwaggerResponse(302, "Redirect to Google OAuth consent screen")]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(500, "OAuth configuration missing or invalid")]
    public async Task<IActionResult> ExternalLogin(
        [FromQuery] string provider = "Google",
        [FromQuery] string returnUrl = "/")
    {
        var command = new ExternalLoginCommand
        {
            Provider = provider,
            ReturnUrl = returnUrl
        };

        ExternalLoginResponseDto externalLoginResponse = await _mediator.Send(command);

        return Challenge(externalLoginResponse.Properties, externalLoginResponse.Provider);
    }

    /// <summary>
    /// Process OAuth callback from Google after authentication middleware validates OAuth response.
    /// Retrieves external login info, creates or links account, generates JWT tokens,
    /// and redirects to returnUrl with tokens in secure cookie.
    /// </summary>
    /// <param name="returnUrl">URL to redirect to after successful authentication. Defaults to "/".</param>
    /// <returns>HTTP 302 redirect to returnUrl with AuthTokenHolder cookie containing access and refresh tokens.</returns>
    [HttpGet("external-auth-callback")]
    [SwaggerOperation(
        Summary = "OAuth callback endpoint",
        Description = "Processes Google OAuth callback. Creates/links account, generates JWT tokens, and redirects with tokens in cookie.")]
    [SwaggerResponse(302, "Redirect to returnUrl with AuthTokenHolder cookie")]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, "Account inactive or deleted")]
    [SwaggerResponse(500, "Token exchange or account creation failed")]
    public async Task<IActionResult> ExternalAuthCallback([FromQuery] string returnUrl = "/")
    {
        try
        {
            var command = new ExternalLoginCallbackCommand(returnUrl);
            AuthResponseDto signInResponse = await _mediator.Send(command);

            var options = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(5)
            };

            Response.Cookies.Append(
                "AuthTokenHolder",
                JsonConvert.SerializeObject(signInResponse, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented
                }), options);
            // Redirect to returnUrl (frontend will extract tokens from cookie)
            return Redirect(returnUrl);
        }
        catch (BadRequestException e)
        {
            return BadRequest(new ApiBadRequestResponse(e.Message));
        }
    }
}
