using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.Auth.SignIn;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using MoriiCoffee.Domain.Shared.Enums.User;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class SignInCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IRsaDecryptionService> _rsaDecryption = new();
    private readonly SignInCommandHandler _handler;

    public SignInCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _rsaDecryption.Setup(r => r.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        _handler = new SignInCommandHandler(
            _userManager.Object, _tokenService.Object, _mapper.Object, _rsaDecryption.Object);
    }

    private static SignInCommand ValidCommand() => new()
    {
        Identity = "user@example.com",
        Password = "TestPass1!"
    };

    private static UserEntity ActiveUser(string email = "user@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        UserName = "testuser",
        Status = EUserStatus.Active
    };

    [Fact]
    public async Task Handle_Success_ReturnsAuthResponseDto()
    {
        var cmd = ValidCommand();
        var user = ActiveUser(cmd.Identity);
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity> { user }.AsQueryable());
        _userManager.Setup(m => m.CheckPasswordAsync(user, cmd.Password)).ReturnsAsync(true);
        _tokenService.Setup(t => t.GenerateAccessTokenAsync(user)).ReturnsAsync("access-token");
        _userManager.Setup(m => m.SetAuthenticationTokenAsync(
            It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "CUSTOMER" });
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { Email = cmd.Identity });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var cmd = ValidCommand();
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity>().AsQueryable());

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorizedException()
    {
        var cmd = ValidCommand();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = cmd.Identity, Status = EUserStatus.Inactive };
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity> { user }.AsQueryable());

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedException()
    {
        var cmd = ValidCommand();
        var user = ActiveUser(cmd.Identity);
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity> { user }.AsQueryable());
        _userManager.Setup(m => m.CheckPasswordAsync(user, cmd.Password)).ReturnsAsync(false);

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }
}
