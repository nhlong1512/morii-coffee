using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.User.UpdateProfile;

/// <summary>Handles profile updates: loads the user, calls the domain method, persists via UserManager, and returns the updated UserDto.</summary>
public class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, UserDto>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IMapper _mapper;

    public UpdateProfileCommandHandler(UserManager<UserEntity> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException("User", request.UserId);

        user.UpdateProfile(request.FullName, request.Dob, request.Gender, request.Bio);
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var dto = _mapper.Map<UserDto>(user);
        dto.Roles = roles.ToList();
        return dto;
    }
}
