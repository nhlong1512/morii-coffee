using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.Auth.RefreshToken;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new RefreshTokenCommandHandler(_userManager.Object, _tokenService.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_ReturnsNewAuthResponseDto()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "user@example.com" };
        const string storedRefreshToken = "stored-refresh-token";
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) });

        _tokenService.Setup(t => t.GetPrincipalFromTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(identity);
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticationTokenAsync(
            user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(storedRefreshToken);
        _tokenService.Setup(t => t.GenerateAccessTokenAsync(user)).ReturnsAsync("new-access-token");
        _userManager.Setup(m => m.SetAuthenticationTokenAsync(
            It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "CUSTOMER" });
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { Email = user.Email });

        var cmd = new RefreshTokenCommand
        {
            AccessToken = "expired-access-token",
            RefreshToken = storedRefreshToken
        };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new-access-token");
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ThrowsUnauthorizedException()
    {
        _tokenService.Setup(t => t.GetPrincipalFromTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((ClaimsIdentity?)null);

        var cmd = new RefreshTokenCommand { AccessToken = "bad-token", RefreshToken = "any" };

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", Guid.NewGuid().ToString()) });
        _tokenService.Setup(t => t.GetPrincipalFromTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(identity);
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((UserEntity?)null);

        var cmd = new RefreshTokenCommand { AccessToken = "token", RefreshToken = "refresh" };

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_RefreshTokenMismatch_ThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "user@example.com" };
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) });

        _tokenService.Setup(t => t.GetPrincipalFromTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(identity);
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticationTokenAsync(
            user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("stored-token");

        var cmd = new RefreshTokenCommand { AccessToken = "token", RefreshToken = "different-token" };

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }
}
