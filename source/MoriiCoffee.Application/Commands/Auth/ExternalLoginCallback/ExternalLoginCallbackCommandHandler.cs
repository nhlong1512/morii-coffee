using System.Security.Claims;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;

/// <summary>
/// Handles OAuth callback processing from external provider (Google).
/// Exchanges authorization code for user profile, creates or links account,
/// generates JWT tokens, and returns them for client storage.
/// Implements User Story 1 and 2: OAuth flow with automatic account creation and role assignment.
/// </summary>
public class ExternalLoginCallbackCommandHandler
    : IRequestHandler<ExternalLoginCallbackCommand, AuthResponseDto>
{
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly UserManager<UserEntity> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public ExternalLoginCallbackCommandHandler(
        SignInManager<UserEntity> signInManager,
        UserManager<UserEntity> userManager,
        ITokenService tokenService,
        IEmailService emailService,
        IMapper mapper)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _mapper = mapper;
    }

    /// <summary>
    /// Processes OAuth callback after authentication middleware validates OAuth code/state.
    /// Creates new accounts for Google users, assigns CUSTOMER role, generates JWT tokens.
    /// </summary>
    /// <param name="request">Command containing return URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with access token, refresh token, and user profile.</returns>
    /// <exception cref="BadRequestException">Missing external login info, email, or account creation failed.</exception>
    /// <exception cref="UnauthorizedException">Account inactive or deactivated.</exception>
    public async Task<AuthResponseDto> Handle(
        ExternalLoginCallbackCommand request,
        CancellationToken cancellationToken)
    {
        // Get external login info from External cookie (set by authentication middleware)
        // The middleware has already validated OAuth code and state parameters
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            throw new BadRequestException("Failed to load external login information.");
        }

        // Extract email from Google profile
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException(
                "Google account must have a verified email address to sign in.");
        }

        // Find user by email
        UserEntity? user = await _userManager.FindByEmailAsync(email);

        // User Story 2: Create new account if user doesn't exist
        if (user == null)
        {
            // Extract user information from Google claims
            var fullName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? email.Split('@')[0];
            var userName = email.Split('@')[0];

            // Ensure unique username
            var existingUserWithSameName = await _userManager.FindByNameAsync(userName);
            if (existingUserWithSameName != null)
            {
                // Append random suffix to make username unique
                userName = $"{userName}{Guid.NewGuid().ToString("N")[..6]}";
            }

            // Create new user account
            user = new UserEntity
            {
                Email = email,
                UserName = userName,
                FullName = fullName,
                EmailConfirmed = true, // Google already verified the email
                Status = EUserStatus.Active
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to create account: {errors}");
            }

            // Link Google account to the new user
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                var errors = string.Join("; ", addLoginResult.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to link Google account: {errors}");
            }

            _ = await _userManager.AddToRoleAsync(user, nameof(ERole.CUSTOMER));

            // Send welcome email (fire and forget)
            _ = _emailService.SendWelcomeEmailAsync(user.Email!, user.UserName!);
        }
        else
        {
            // Existing user - check if account is active
            if (user.Status == EUserStatus.Inactive)
            {
                throw new UnauthorizedException(
                    "Your account has been deactivated. Contact support for assistance.");
            }

            // Link Google account if not already linked
            var existingLogin = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingLogin == null)
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    var errors = string.Join("; ", addLoginResult.Errors.Select(e => e.Description));
                    throw new BadRequestException($"Failed to link Google account: {errors}");
                }
            }
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        // Generate JWT tokens
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = Guid.NewGuid().ToString("N");

        // Store refresh token with default provider
        await _userManager.SetAuthenticationTokenAsync(user, TokenProviders.DEFAULT, TokenTypes.REFRESH, refreshToken);

        // Map user to DTO and include roles
        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto
        };
    }
}
