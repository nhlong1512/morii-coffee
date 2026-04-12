using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MoriiCoffee.Application.Commands.Auth.SignUp;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Tests.Helpers;
using Xunit;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class SignUpCommandHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _userManager;
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly SignUpCommandHandler _handler;

    public SignUpCommandHandlerTests()
    {
        _userManager = UserManagerHelper.Create();
        _handler = new SignUpCommandHandler(
            _userManager.Object, _tokenService.Object, _emailService.Object, _mapper.Object);
    }

    private static SignUpCommand ValidCommand() => new(new SignUpDto
    {
        Email = "test@example.com",
        PhoneNumber = "0901234567",
        Password = "TestPass1!"
    });

    [Fact]
    public async Task Handle_Success_ReturnsAuthResponseDto()
    {
        var cmd = ValidCommand();
        _userManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync((UserEntity?)null);
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity>().AsQueryable());
        _userManager.Setup(m => m.CreateAsync(It.IsAny<UserEntity>(), cmd.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<UserEntity>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _tokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync("access-token");
        _userManager.Setup(m => m.SetAuthenticationTokenAsync(
            It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _emailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<UserDto>(It.IsAny<UserEntity>()))
            .Returns(new UserDto { Email = cmd.Email });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.User.Email.Should().Be(cmd.Email);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsBadRequestException()
    {
        var cmd = ValidCommand();
        _userManager.Setup(m => m.FindByEmailAsync(cmd.Email))
            .ReturnsAsync(new UserEntity { Email = cmd.Email });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_PhoneAlreadyExists_ThrowsBadRequestException()
    {
        var cmd = ValidCommand();
        _userManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync((UserEntity?)null);
        var existing = new UserEntity { PhoneNumber = cmd.PhoneNumber };
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity> { existing }.AsQueryable());

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_CreateAsyncFails_ThrowsBadRequestException()
    {
        var cmd = ValidCommand();
        _userManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync((UserEntity?)null);
        _userManager.Setup(m => m.Users).Returns(new List<UserEntity>().AsQueryable());
        _userManager.Setup(m => m.CreateAsync(It.IsAny<UserEntity>(), cmd.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Weak password" }));

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
