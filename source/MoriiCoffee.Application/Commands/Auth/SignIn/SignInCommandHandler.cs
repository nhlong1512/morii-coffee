using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;

namespace MoriiCoffee.Application.Commands.Auth.SignIn;

/// <summary>Handles sign-in: decrypts the RSA-encrypted password, resolves the user by email, validates credentials, then issues tokens.</summary>
public class SignInCommandHandler : ICommandHandler<SignInCommand, AuthResponseDto>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IRsaDecryptionService _rsaDecryption;

    public SignInCommandHandler(
        UserManager<UserEntity> userManager,
        ITokenService tokenService,
        IMapper mapper,
        IRsaDecryptionService rsaDecryption)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _mapper = mapper;
        _rsaDecryption = rsaDecryption;
    }

    public async Task<AuthResponseDto> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var user = _userManager.Users.FirstOrDefault(u =>
            u.Email == request.Identity);

        if (user is null || user.IsDeleted)
        {
            throw new NotFoundException("User", request.Identity);
        }

        if (user.Status == EUserStatus.Inactive)
        {
            throw new UnauthorizedException("Your account has been deactivated.");
        }

        var plainPassword = _rsaDecryption.Decrypt(request.Password);
        var passwordValid = await _userManager.CheckPasswordAsync(user, plainPassword);
        if (!passwordValid)
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = Guid.NewGuid().ToString("N");
        await _userManager.SetAuthenticationTokenAsync(user, TokenProviders.DEFAULT, TokenTypes.REFRESH, refreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto
        };
    }
}
