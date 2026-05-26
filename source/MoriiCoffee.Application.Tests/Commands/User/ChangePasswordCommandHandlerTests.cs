using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.User.ChangePassword;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<IRsaDecryptionService> _rsaDecryption = new();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _rsaDecryption.Setup(r => r.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        _handler = new ChangePasswordCommandHandler(_userManager.Object, _rsaDecryption.Object);
    }

    [Fact]
    public async Task Handle_Success_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId };
        var cmd = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!"
        };

        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.ChangePasswordAsync(user, cmd.CurrentPassword, cmd.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((UserEntity?)null);

        var cmd = new ChangePasswordCommand { UserId = userId, CurrentPassword = "Old", NewPassword = "New" };

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId };
        var cmd = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = "WrongPass!",
            NewPassword = "NewPass1!"
        };

        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.ChangePasswordAsync(user, cmd.CurrentPassword, cmd.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
