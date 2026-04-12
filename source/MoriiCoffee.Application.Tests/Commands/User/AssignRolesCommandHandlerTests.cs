using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.User.AssignRoles;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class AssignRolesCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly AssignRolesCommandHandler _handler;

    public AssignRolesCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new AssignRolesCommandHandler(_userManager.Object);
    }

    [Fact]
    public async Task Handle_Success_RemovesAndAddsRoles()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId };
        var cmd = new AssignRolesCommand { UserId = userId, Roles = new List<string> { "ADMIN" } };

        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "CUSTOMER" });
        _userManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        _userManager.Verify(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        _userManager.Verify(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((UserEntity?)null);

        var cmd = new AssignRolesCommand { UserId = userId, Roles = new List<string> { "ADMIN" } };

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
