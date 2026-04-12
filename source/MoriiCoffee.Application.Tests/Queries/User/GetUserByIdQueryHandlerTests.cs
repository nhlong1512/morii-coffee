using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.User.GetUserById;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Queries.User;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<Microsoft.AspNetCore.Identity.UserManager<UserEntity>> _userManager;
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new GetUserByIdQueryHandler(_userManager.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_UserFound_ReturnsUserDtoWithRoles()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "admin@example.com" };
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "ADMIN" });
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { Email = "admin@example.com" });

        var result = await _handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Roles.Should().Contain("ADMIN");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((UserEntity?)null);

        await _handler.Invoking(h => h.Handle(new GetUserByIdQuery(userId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
