using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Constants;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.RefreshToken;

/// <summary>Handles token refresh: extracts userId from the expired JWT (skipping lifetime validation), verifies the stored refresh token, and issues new tokens.</summary>
public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public RefreshTokenCommandHandler(
        UserManager<UserEntity> userManager,
        ITokenService tokenService,
        IMapper mapper)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Extract userId from expired JWT (skips lifetime validation)
        var principal = await _tokenService.GetPrincipalFromTokenAsync(request.AccessToken)
            ?? throw new UnauthorizedException("Invalid access token.");

        var userIdStr = principal.FindFirst("jti")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedException("Invalid access token.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedException("User not found.");

        // Verify stored refresh token
        var storedToken = await _userManager.GetAuthenticationTokenAsync(
            user, TokenProviders.DEFAULT, TokenTypes.REFRESH);

        if (storedToken != request.RefreshToken)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        // Issue new tokens
        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var newRefreshToken = Guid.NewGuid().ToString("N");
        await _userManager.SetAuthenticationTokenAsync(user, TokenProviders.DEFAULT, TokenTypes.REFRESH, newRefreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            User = userDto
        };
    }
}
