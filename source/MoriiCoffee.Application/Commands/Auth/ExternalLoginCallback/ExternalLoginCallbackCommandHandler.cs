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
    private readonly RoleManager<MoriiCoffee.Domain.Aggregates.UserAggregate.Entities.Role> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public ExternalLoginCallbackCommandHandler(
        SignInManager<UserEntity> signInManager,
        UserManager<UserEntity> userManager,
        RoleManager<MoriiCoffee.Domain.Aggregates.UserAggregate.Entities.Role> roleManager,
        ITokenService tokenService,
        IEmailService emailService,
        IMapper mapper)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _mapper = mapper;
    }

    /// <summary>
    /// Processes OAuth callback.
    /// User Story 1 and 2: Creates new accounts for Google users, assigns CUSTOMER role, sends welcome email.
    /// Token storage will be added in Phase 5 (User Story 3).
    /// </summary>
    /// <param name="request">Command containing authorization code, state, and return URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with access token, refresh token, and user profile.</returns>
    /// <exception cref="BadRequestException">User denied permission, missing email from Google, or account creation failed.</exception>
    /// <exception cref="UnauthorizedException">Invalid state parameter, OAuth session expired, or account inactive.</exception>
    public async Task<AuthResponseDto> Handle(
        ExternalLoginCallbackCommand request,
        CancellationToken cancellationToken)
    {
        // Check if user denied permission
        if (!string.IsNullOrEmpty(request.Error))
        {
            throw new BadRequestException(
                request.ErrorDescription ?? "You must grant permission to sign in with Google.");
        }

        // Get external login info from OAuth provider (Google)
        // This validates the state parameter automatically for CSRF protection
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            throw new UnauthorizedException(
                "OAuth session expired or invalid state parameter. Please restart the sign-in process.");
        }

        // Extract email from Google profile
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException(
                "Google account must have a verified email address to sign in.");
        }

        // Find user by Google external login
        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        // If user doesn't have Google linked, try to find by email
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(email);
        }

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

            // Assign CUSTOMER role - ensure role exists first
            var roleExists = await _roleManager.RoleExistsAsync(nameof(ERole.CUSTOMER));
            if (!roleExists)
            {
                throw new BadRequestException(
                    "System error: CUSTOMER role not found. Please contact system administrator.");
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, nameof(ERole.CUSTOMER));
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to assign role: {errors}");
            }

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

        // Generate JWT tokens
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = Guid.NewGuid().ToString("N");

        // User Story 3: Store refresh token in AspNetUserTokens
        // SetAuthenticationTokenAsync automatically replaces old token if exists
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
