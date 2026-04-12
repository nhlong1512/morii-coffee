using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.User.ChangeAvatar;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class ChangeAvatarCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly ChangeAvatarCommandHandler _handler;

    public ChangeAvatarCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new ChangeAvatarCommandHandler(_userManager.Object, _fileService.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_UploadsAvatarAndReturnsUserDto()
    {
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Email = "user@example.com" };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("avatar.jpg");

        var blobResponse = new BlobResponseDto { Blob = new BlobDto { Uri = "https://cdn.test/avatar.jpg", Name = "avatar.jpg" } };
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _fileService.Setup(f => f.UploadAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "CUSTOMER" });
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { AvatarUrl = "https://cdn.test/avatar.jpg" });

        var cmd = new ChangeAvatarCommand { UserId = userId, Avatar = fileMock.Object };
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.AvatarUrl.Should().Be("https://cdn.test/avatar.jpg");
        _fileService.Verify(f => f.UploadAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((UserEntity?)null);

        var fileMock = new Mock<IFormFile>();
        var cmd = new ChangeAvatarCommand { UserId = userId, Avatar = fileMock.Object };

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
