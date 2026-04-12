using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class ExternalLoginCallbackCommandHandlerTests
{
    private readonly Mock<SignInManager<UserEntity>> _signInManager;
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly ExternalLoginCallbackCommandHandler _handler;

    public ExternalLoginCallbackCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _signInManager = new Mock<SignInManager<UserEntity>>(
            _userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<UserEntity>>(),
            null!,
            null!,
            null!,
            null!
        );
        _handler = new ExternalLoginCallbackCommandHandler(
            _signInManager.Object,
            _userManager.Object,
            _tokenService.Object,
            _emailService.Object,
            _mapper.Object);
    }

    private static ExternalLoginInfo BuildExternalLoginInfo(string email, string? fullName = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        if (fullName != null)
            claims.Add(new Claim(ClaimTypes.Name, fullName));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        return new ExternalLoginInfo(principal, "Google", "google-key-123", "Google");
    }

    [Fact]
    public async Task Handle_GetExternalLoginInfoReturnsNull_ThrowsBadRequestException()
    {
        _signInManager.Setup(s => s.GetExternalLoginInfoAsync(null))
            .ReturnsAsync((ExternalLoginInfo?)null);

        await _handler.Invoking(h =>
                h.Handle(new ExternalLoginCallbackCommand("/"), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_NoPrincipalEmail_ThrowsBadRequestException()
    {
        // Principal with no email claim
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var info = new ExternalLoginInfo(principal, "Google", "key", "Google");

        _signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);

        await _handler.Invoking(h =>
                h.Handle(new ExternalLoginCallbackCommand("/"), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_NewUser_CreatesAccountAndReturnsTokens()
    {
        var email = "newuser@google.com";
        var info = BuildExternalLoginInfo(email, "New User");
        var userId = Guid.NewGuid();

        _signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
        _signInManager.Setup(s => s.SignInAsync(It.IsAny<UserEntity>(), false, null))
            .Returns(Task.CompletedTask);

        _userManager.Setup(m => m.FindByEmailAsync(email))
            .ReturnsAsync((UserEntity?)null);
        _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((UserEntity?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddLoginAsync(It.IsAny<UserEntity>(), info))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<UserEntity>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetAuthenticationTokenAsync(
                It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync(new List<string> { "CUSTOMER" });
        _emailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _tokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync("access-token");
        _mapper.Setup(m => m.Map<UserDto>(It.IsAny<UserEntity>()))
            .Returns(new UserDto { Email = email });

        var result = await _handler.Handle(new ExternalLoginCallbackCommand("/"), CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        _userManager.Verify(m => m.CreateAsync(It.IsAny<UserEntity>()), Times.Once);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<UserEntity>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingActiveUser_ReturnsTokensWithoutCreating()
    {
        var email = "existing@google.com";
        var info = BuildExternalLoginInfo(email);
        var existingUser = new UserEntity { Id = Guid.NewGuid(), Email = email, Status = MoriiCoffee.Domain.Shared.Enums.User.EUserStatus.Active };

        _signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
        _signInManager.Setup(s => s.SignInAsync(It.IsAny<UserEntity>(), false, null))
            .Returns(Task.CompletedTask);

        _userManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(existingUser);
        _userManager.Setup(m => m.FindByLoginAsync(info.LoginProvider, info.ProviderKey))
            .ReturnsAsync(existingUser); // already linked
        _userManager.Setup(m => m.SetAuthenticationTokenAsync(
                It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync(new List<string> { "CUSTOMER" });
        _tokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync("access-token");
        _mapper.Setup(m => m.Map<UserDto>(It.IsAny<UserEntity>()))
            .Returns(new UserDto { Email = email });

        var result = await _handler.Handle(new ExternalLoginCallbackCommand("/"), CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        _userManager.Verify(m => m.CreateAsync(It.IsAny<UserEntity>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingInactiveUser_ThrowsUnauthorizedException()
    {
        var email = "inactive@google.com";
        var info = BuildExternalLoginInfo(email);
        var inactiveUser = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            Status = MoriiCoffee.Domain.Shared.Enums.User.EUserStatus.Inactive
        };

        _signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
        _userManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(inactiveUser);

        await _handler.Invoking(h =>
                h.Handle(new ExternalLoginCallbackCommand("/"), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_CreateAsyncFails_ThrowsBadRequestException()
    {
        var email = "fail@google.com";
        var info = BuildExternalLoginInfo(email, "Fail User");

        _signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
        _userManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync((UserEntity?)null);
        _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((UserEntity?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" }));

        await _handler.Invoking(h =>
                h.Handle(new ExternalLoginCallbackCommand("/"), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
