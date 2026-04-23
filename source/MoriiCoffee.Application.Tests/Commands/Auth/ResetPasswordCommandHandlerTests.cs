using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.Auth.ResetPassword;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<IPasswordResetTicketStore> _ticketStore = new();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new ResetPasswordCommandHandler(_userManager.Object, _ticketStore.Object);
    }

    private static ResetPasswordCommand ValidCommand(string ticket = "valid-ticket") => new()
    {
        Ticket = ticket,
        NewPassword = "NewPass1!"
    };

    private static PasswordResetTicketDto ValidTicketDto(Guid userId, string email = "user@example.com") => new()
    {
        TicketId = "valid-ticket",
        UserId = userId,
        Email = email,
        IdentityToken = "identity-reset-token",
        CreatedAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
    };

    [Fact]
    public async Task Handle_ValidTicket_ResetsPasswordSuccessfully()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "user@example.com" };
        var ticket = ValidTicketDto(userId);

        _ticketStore.Setup(s => s.GetAndConsumeTicketAsync("valid-ticket")).ReturnsAsync(ticket);
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, ticket.IdentityToken, "NewPass1!"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExpiredOrConsumedTicket_ThrowsBadRequestException()
    {
        _ticketStore.Setup(s => s.GetAndConsumeTicketAsync(It.IsAny<string>()))
            .ReturnsAsync((PasswordResetTicketDto?)null);

        await _handler.Invoking(h => h.Handle(ValidCommand(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var ticket = ValidTicketDto(userId);

        _ticketStore.Setup(s => s.GetAndConsumeTicketAsync("valid-ticket")).ReturnsAsync(ticket);
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((UserEntity?)null);

        await _handler.Invoking(h => h.Handle(ValidCommand(), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ResetPasswordFails_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "user@example.com" };
        var ticket = ValidTicketDto(userId);

        _ticketStore.Setup(s => s.GetAndConsumeTicketAsync("valid-ticket")).ReturnsAsync(ticket);
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, ticket.IdentityToken, "NewPass1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        await _handler.Invoking(h => h.Handle(ValidCommand(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_Success_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "user@example.com" };
        var ticket = ValidTicketDto(userId);

        _ticketStore.Setup(s => s.GetAndConsumeTicketAsync("valid-ticket")).ReturnsAsync(ticket);
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, ticket.IdentityToken, "NewPass1!"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().BeTrue();
    }
}
