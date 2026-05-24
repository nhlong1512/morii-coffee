using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.User.SaveDeliveryProfile;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class SaveDeliveryProfileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserDeliveryProfileRepository> _profilesRepo = new();
    private readonly SaveDeliveryProfileCommandHandler _handler;

    public SaveDeliveryProfileCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.UserDeliveryProfiles).Returns(_profilesRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _handler = new SaveDeliveryProfileCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_NoExistingProfile_CreatesNewProfileAndCommits()
    {
        var userId = Guid.NewGuid();
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserDeliveryProfile?)null);

        var result = await _handler.Handle(BuildCommand(userId), CancellationToken.None);

        _profilesRepo.Verify(r => r.UpsertAsync(
            It.Is<UserDeliveryProfile>(p =>
                p.UserId == userId &&
                p.FullName == "Nguyễn Văn A" &&
                p.PhoneNumber == "0901234567" &&
                p.ProvinceId == 79 &&
                p.WardCode == "26734")),
            Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        result.FullName.Should().Be("Nguyễn Văn A");
    }

    [Fact]
    public async Task Handle_ExistingProfile_UpdatesProfileAndCommits()
    {
        var userId = Guid.NewGuid();
        var existing = UserDeliveryProfile.Create(userId, "Old Name", "0900000000", "Old Address");
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);

        var command = BuildCommand(userId, fullName: "New Name", phone: "0912345678");
        var result = await _handler.Handle(command, CancellationToken.None);

        _profilesRepo.Verify(r => r.UpsertAsync(
            It.Is<UserDeliveryProfile>(p =>
                p.FullName == "New Name" &&
                p.PhoneNumber == "0912345678" &&
                p.DistrictId == 760)),
            Times.Once);
        result.FullName.Should().Be("New Name");
        result.PhoneNumber.Should().Be("0912345678");
    }

    [Fact]
    public async Task Handle_ReturnsDto_WithCommandValues()
    {
        var userId = Guid.NewGuid();
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserDeliveryProfile?)null);

        var result = await _handler.Handle(BuildCommand(userId), CancellationToken.None);

        result.Address.Should().Be("123 Đường ABC, Quận 1");
        result.ProvinceName.Should().Be("Ho Chi Minh");
        result.DistrictName.Should().Be("District 1");
        result.WardCode.Should().Be("26734");
    }

    private static SaveDeliveryProfileCommand BuildCommand(
        Guid userId,
        string fullName = "Nguyễn Văn A",
        string phone = "0901234567") => new()
    {
        UserId = userId,
        FullName = fullName,
        PhoneNumber = phone,
        Address = "123 Đường ABC, Quận 1",
        ProvinceId = 79,
        ProvinceName = "Ho Chi Minh",
        DistrictId = 760,
        DistrictName = "District 1",
        WardCode = "26734",
        WardName = "Ben Nghe"
    };
}
