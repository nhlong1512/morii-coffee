using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.Auth.ForgotPassword;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.Tests.Helpers;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly EmailSettings _emailSettings = new()
    {
        ResetPasswordBaseUrl = "https://app.test/reset-password",
        FromEmail = "no-reply@test.com",
        FromName = "Test",
        StorefrontUrl = "https://app.test",
        Brevo = new BrevoSettings { ApiKey = "test-key" }
    };
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new ForgotPasswordCommandHandler(
            _userManager.Object, _emailService.Object, _emailSettings);
    }

    [Fact]
    public async Task Handle_UserExists_SendsEmailAndReturnsTrue()
    {
        const string email = "user@example.com";
        var user = new UserEntity { Id = Guid.NewGuid(), Email = email };
        _userManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _emailService.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new ForgotPasswordCommand { Email = email }, CancellationToken.None);

        result.Should().BeTrue();
        _emailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsTrueWithoutSendingEmail()
    {
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((UserEntity?)null);

        var result = await _handler.Handle(
            new ForgotPasswordCommand { Email = "ghost@example.com" }, CancellationToken.None);

        result.Should().BeTrue();
        _emailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
