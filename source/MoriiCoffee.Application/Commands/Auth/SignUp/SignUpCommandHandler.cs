using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.SignUp;

/// <summary>Handles user registration: creates the account, assigns the CUSTOMER role, generates tokens, and sends a welcome email.</summary>
public class SignUpCommandHandler : ICommandHandler<SignUpCommand, AuthResponseDto>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public SignUpCommandHandler(
        UserManager<UserEntity> userManager,
        ITokenService tokenService,
        IEmailService emailService,
        IMapper mapper)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            throw new BadRequestException("Email already exists.");

        if (_userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber) is not null)
            throw new BadRequestException("Phone number already exists.");

        var userName = request.UserName ?? request.Email.Split('@')[0];

        var user = new UserEntity
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            UserName = userName
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new BadRequestException($"Registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, nameof(ERole.CUSTOMER));

        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = Guid.NewGuid().ToString("N");
        await _userManager.SetAuthenticationTokenAsync(user, TokenProviders.DEFAULT, TokenTypes.REFRESH, refreshToken);

        _ = _emailService.SendWelcomeEmailAsync(user.Email!, user.UserName!);

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = [nameof(ERole.CUSTOMER)];

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto
        };
    }
}
