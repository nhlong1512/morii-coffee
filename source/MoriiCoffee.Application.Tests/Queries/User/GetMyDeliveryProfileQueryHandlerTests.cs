using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.User.GetMyDeliveryProfile;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.User;

public class GetMyDeliveryProfileQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserDeliveryProfileRepository> _profilesRepo = new();
    private readonly GetMyDeliveryProfileQueryHandler _handler;

    public GetMyDeliveryProfileQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.UserDeliveryProfiles).Returns(_profilesRepo.Object);
        _handler = new GetMyDeliveryProfileQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_NoProfile_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserDeliveryProfile?)null);

        var result = await _handler.Handle(new GetMyDeliveryProfileQuery(userId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProfileExists_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var profile = UserDeliveryProfile.Create(
            userId,
            "Nguyễn Văn A",
            "0901234567",
            "123 Đường ABC",
            79,
            "Ho Chi Minh",
            760,
            "District 1",
            "26734",
            "Ben Nghe");
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetMyDeliveryProfileQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("Nguyễn Văn A");
        result.PhoneNumber.Should().Be("0901234567");
        result.Address.Should().Be("123 Đường ABC");
        result.ProvinceId.Should().Be(79);
        result.DistrictName.Should().Be("District 1");
        result.WardCode.Should().Be("26734");
    }
}
