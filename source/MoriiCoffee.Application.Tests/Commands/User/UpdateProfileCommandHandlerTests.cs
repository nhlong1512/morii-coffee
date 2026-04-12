using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.User.UpdateProfile;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new UpdateProfileCommandHandler(_userManager.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_UpdatesUserAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "user@example.com" };
        var cmd = new UpdateProfileCommand
        {
            UserId = userId,
            FullName = "Nguyen Van A",
            Bio = "Coffee lover"
        };

        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "CUSTOMER" });
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { FullName = "Nguyen Van A" });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.FullName.Should().Be("Nguyen Van A");
        _userManager.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((UserEntity?)null);

        var cmd = new UpdateProfileCommand { UserId = userId, FullName = "Test" };

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
