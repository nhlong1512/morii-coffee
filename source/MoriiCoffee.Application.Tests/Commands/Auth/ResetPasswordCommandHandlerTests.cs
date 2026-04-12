using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.Auth.ResetPassword;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new ResetPasswordCommandHandler(_userManager.Object);
    }

    private static ResetPasswordCommand ValidCommand(string email = "user@example.com") => new()
    {
        Email = email,
        Token = "valid-token",
        NewPassword = "NewPass1!"
    };

    [Fact]
    public async Task Handle_Success_ReturnsTrue()
    {
        var cmd = ValidCommand();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = cmd.Email };
        _userManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, cmd.Token, cmd.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var cmd = ValidCommand();
        _userManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync((UserEntity?)null);

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ResetPasswordFails_ThrowsBadRequestException()
    {
        var cmd = ValidCommand();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = cmd.Email };
        _userManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, cmd.Token, cmd.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
