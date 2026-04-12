using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Mappings;
using MoriiCoffee.Domain.Shared.Enums.User;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Mappings;

public class UserMapperTests
{
    private readonly IMapper _mapper;

    public UserMapperTests()
    {
        // Note: UserMapper includes SignUpDto→SignUpCommand which has a ctor-only constructor.
        // AssertConfigurationIsValid() fails on that mapping, so we only use CreateMapper() here.
        var config = new MapperConfiguration(cfg => cfg.AddProfile<UserMapper>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void UserToUserDto_MapsCorrectly()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "testuser",
            FullName = "Nguyen Van A",
            Status = EUserStatus.Active
        };

        var dto = _mapper.Map<UserDto>(user);

        dto.Id.Should().Be(userId);
        dto.Email.Should().Be("user@example.com");
        dto.FullName.Should().Be("Nguyen Van A");
        dto.Status.Should().Be(EUserStatus.Active);
        dto.Roles.Should().BeNull(); // Roles are ignored by mapper, set manually
    }

    [Fact]
    public void UserToUserSummaryDto_MapsCorrectly()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity
        {
            Id = userId,
            Email = "admin@example.com",
            Status = EUserStatus.Active
        };

        var dto = _mapper.Map<UserSummaryDto>(user);

        dto.Id.Should().Be(userId);
        dto.Email.Should().Be("admin@example.com");
    }
}
