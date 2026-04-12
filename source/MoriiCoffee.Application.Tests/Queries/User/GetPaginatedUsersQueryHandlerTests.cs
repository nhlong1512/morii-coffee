using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.User.GetPaginatedUsers;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.Tests.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Queries.User;

public class GetPaginatedUsersQueryHandlerTests
{
    private readonly Mock<Microsoft.AspNetCore.Identity.UserManager<UserEntity>> _userManager;
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPaginatedUsersQueryHandler _handler;

    public GetPaginatedUsersQueryHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new GetPaginatedUsersQueryHandler(_userManager.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithUsers_ReturnsPaginatedResult()
    {
        var user1 = new UserEntity { Id = Guid.NewGuid(), Email = "alice@example.com" };
        var user2 = new UserEntity { Id = Guid.NewGuid(), Email = "bob@example.com" };
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity> { user1, user2 }.AsQueryable());
        _mapper.Setup(m => m.Map<UserSummaryDto>(It.IsAny<UserEntity>()))
            .Returns((UserEntity u) => new UserSummaryDto { Email = u.Email });

        var query = new GetPaginatedUsersQuery(
            new PaginationFilter { TakeAll = true }, null, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Email).Should().Contain("alice@example.com");
    }

    [Fact]
    public async Task Handle_EmptyUsers_ReturnsEmptyPagination()
    {
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity>().AsQueryable());
        _mapper.Setup(m => m.Map<UserSummaryDto>(It.IsAny<UserEntity>())).Returns(new UserSummaryDto());

        var query = new GetPaginatedUsersQuery(
            new PaginationFilter { TakeAll = true }, null, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Metadata.TotalCount.Should().Be(0);
    }
}
